package com.grpcexample.userclient;

import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;
import io.grpc.StatusRuntimeException;
import io.grpc.stub.StreamObserver;
import com.grpcexample.userservice.UserServiceGrpc;
import com.grpcexample.userservice.UserServiceProto.*;

import java.net.URI;
import java.util.*;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Java gRPC Client demonstrating load balancing and cross-language interoperability
 * with both C# and Java servers. Supports all gRPC streaming patterns.
 */
public class UserServiceClient {
    
    private final List<String> serverAddresses;
    private final List<ManagedChannel> channels;
    private final List<UserServiceGrpc.UserServiceBlockingStub> blockingStubs;
    private final List<UserServiceGrpc.UserServiceStub> asyncStubs;
    private final AtomicInteger requestCounter = new AtomicInteger(0);
    
    public UserServiceClient(String[] serverAddresses) {
        this.serverAddresses = Arrays.asList(serverAddresses);
        this.channels = new ArrayList<>();
        this.blockingStubs = new ArrayList<>();
        this.asyncStubs = new ArrayList<>();
        
        initializeChannels();
    }    private void initializeChannels() {
        for (String address : serverAddresses) {
            try {
                // Parse server address (handle different formats)
                String host;
                int port;
                
                if (address.startsWith("http://") || address.startsWith("https://")) {
                    // Full URL provided - extract host and port
                    URI uri = URI.create(address);
                    host = uri.getHost();
                    port = uri.getPort();
                } else if (address.contains(":")) {
                    // Host:port format
                    String[] parts = address.split(":");
                    host = parts[0];
                    port = Integer.parseInt(parts[1]);                
                } else {
                    // Just port number, assume localhost
                    host = "localhost";
                    port = Integer.parseInt(address);
                }

                // Create channel - use plaintext for all connections
                ManagedChannel channel = ManagedChannelBuilder
                    .forAddress(host, port)
                    .usePlaintext()
                    .build();
                
                channels.add(channel);
                blockingStubs.add(UserServiceGrpc.newBlockingStub(channel));
                asyncStubs.add(UserServiceGrpc.newStub(channel));
                
                System.out.println("📡 Connected to server: " + host + ":" + port);
                
            } catch (Exception e) {
                System.err.println("❌ Failed to connect to " + address + ": " + e.getMessage());
            }
        }
        
        if (channels.isEmpty()) {
            throw new RuntimeException("Failed to connect to any servers!");
        }
    }
      private String normalizeServerAddress(String address) {
        // Support different address formats like the C# client
        if (address.startsWith("http://") || address.startsWith("https://")) {
            return address;
        } else if (address.contains(":")) {
            return "http://" + address;
        } else {
            // Just port number, assume localhost and HTTP (for Java client compatibility)
            return "http://localhost:" + address;
        }
    }
    
    private UserServiceGrpc.UserServiceBlockingStub getNextBlockingStub() {
        int index = requestCounter.getAndIncrement() % blockingStubs.size();
        return blockingStubs.get(index);
    }
    
    private UserServiceGrpc.UserServiceStub getNextAsyncStub() {
        int index = requestCounter.get() % asyncStubs.size();
        return asyncStubs.get(index);
    }
      public void demonstrateLoadBalancing() throws InterruptedException {
        System.out.println("\n📊 Load Balancing Demonstration");
        System.out.println("--------------------------------");
        
        Map<String, Integer> serverRequestCounts = new HashMap<>();
        
        // Make 15 requests to demonstrate load balancing
        for (int i = 1; i <= 15; i++) {
            try {
                ServerInfoRequest request = ServerInfoRequest.newBuilder().build();
                ServerInfoResponse response = getNextBlockingStub().getServerInfo(request);
                
                String serverId = response.getServerId();
                String serverType = serverId.startsWith("CSHARP") ? "C#" : "Java";
                String currentAddress = serverAddresses.get((i - 1) % serverAddresses.size());
                
                System.out.printf("Request %02d: %s → Server ID: %s (%s)%n", 
                    i, currentAddress, serverId, serverType);
                
                serverRequestCounts.put(serverId, serverRequestCounts.getOrDefault(serverId, 0) + 1);
                
            } catch (StatusRuntimeException e) {
                System.err.printf("Request %02d failed: %s%n", i, e.getStatus());
            }
            
            Thread.sleep(100); // Small delay between requests
        }
        
        // Print load distribution summary
        System.out.println("\n📈 Load Distribution Summary:");
        serverRequestCounts.entrySet().stream()
            .sorted(Map.Entry.comparingByKey())
            .forEach(entry -> System.out.printf("  %s: %d requests%n", 
                entry.getKey(), entry.getValue()));
    }
    
    public void demonstrateUnaryOperations() {
        System.out.println("\n🔄 Unary Operations Demo");
        System.out.println("------------------------");
        
        try {
            // Create a new user
            System.out.println("Creating new user...");
            CreateUserRequest createRequest = CreateUserRequest.newBuilder()
                .setName("Java Test User")
                .setEmail("javatest@company.com")
                .setDepartment("Engineering")
                .build();
            
            CreateUserResponse createResponse = getNextBlockingStub().createUser(createRequest);
            System.out.printf("✅ User created: ID=%d, Server=%s%n", 
                createResponse.getUser().getId(), createResponse.getServerId());
            
            // Try to get the user (might fail due to distributed storage)
            System.out.printf("Fetching user with ID %d...%n", createResponse.getUser().getId());
            GetUserRequest getUserRequest = GetUserRequest.newBuilder()
                .setUserId(createResponse.getUser().getId())
                .build();
            
            try {
                GetUserResponse getUserResponse = getNextBlockingStub().getUser(getUserRequest);
                System.out.printf("✅ User found: %s (%s)%n", 
                    getUserResponse.getUser().getName(), getUserResponse.getUser().getEmail());
            } catch (StatusRuntimeException e) {
                System.out.println("❌ User not found - This can happen in distributed systems without shared storage!");
            }
            
        } catch (StatusRuntimeException e) {
            System.err.println("❌ Unary operation failed: " + e.getStatus());
        }
    }
    
    public void demonstrateServerStreaming() throws InterruptedException {
        System.out.println("\n📡 Server Streaming Demo");
        System.out.println("------------------------");
        
        try {
            String serverAddress = serverAddresses.get(0);
            System.out.println("Getting users from Engineering department via " + serverAddress + "...");
            
            GetUsersByDepartmentRequest request = GetUsersByDepartmentRequest.newBuilder()
                .setDepartment("Engineering")
                .build();
            
            CountDownLatch latch = new CountDownLatch(1);
            AtomicInteger userCount = new AtomicInteger(0);
              getNextAsyncStub().getUsersByDepartment(request, new StreamObserver<User>() {
                @Override
                public void onNext(User user) {
                    System.out.printf("  👤 %s (%s) - ID: %d%n", 
                        user.getName(), user.getEmail(), user.getId());
                    userCount.incrementAndGet();
                }
                
                @Override
                public void onError(Throwable t) {
                    System.err.println("❌ Server streaming failed: " + t.getMessage());
                    latch.countDown();
                }
                
                @Override
                public void onCompleted() {
                    System.out.printf("✅ Streamed %d users from Engineering department%n", userCount.get());
                    latch.countDown();
                }
            });
            
            latch.await(10, TimeUnit.SECONDS);
            
        } catch (Exception e) {
            System.err.println("❌ Server streaming demo failed: " + e.getMessage());
        }
    }
    
    public void demonstrateClientStreaming() throws InterruptedException {
        System.out.println("\n📤 Client Streaming Demo");
        System.out.println("------------------------");
        
        try {
            System.out.println("Bulk creating users...");
            
            CountDownLatch latch = new CountDownLatch(1);
              StreamObserver<CreateUserRequest> requestObserver = getNextAsyncStub().bulkCreateUsers(
                new StreamObserver<BulkCreateUsersResponse>() {
                    @Override
                    public void onNext(BulkCreateUsersResponse response) {
                        System.out.printf("✅ Bulk creation completed on server %s%n", response.getServerId());
                        System.out.printf("   Created %d users%n", response.getCreatedCount());
                        System.out.printf("   New user IDs: %s%n", response.getUserIdsList());
                    }
                    
                    @Override
                    public void onError(Throwable t) {
                        System.err.println("❌ Client streaming failed: " + t.getMessage());
                        latch.countDown();
                    }
                    
                    @Override
                    public void onCompleted() {
                        latch.countDown();
                    }
                }
            );
              // Send multiple user creation requests
            String[] userNames = {"Java Alice", "Java Bob", "Java Carol", "Java David"};
            for (String name : userNames) {
                CreateUserRequest request = CreateUserRequest.newBuilder()
                    .setName(name)
                    .setEmail(name.toLowerCase().replace(" ", ".") + "@javacompany.com")
                    .setDepartment("Engineering")
                    .build();
                
                requestObserver.onNext(request);
                System.out.println("  📤 Sent: " + name);
                Thread.sleep(200);
            }
            
            requestObserver.onCompleted();
            latch.await(10, TimeUnit.SECONDS);
            
        } catch (Exception e) {
            System.err.println("❌ Client streaming demo failed: " + e.getMessage());
        }
    }
    
    public void demonstrateBidirectionalStreaming() throws InterruptedException {
        System.out.println("\n🔄 Bidirectional Streaming Demo");
        System.out.println("-------------------------------");
        
        try {
            System.out.println("Starting bidirectional user updates stream...");
            
            CountDownLatch latch = new CountDownLatch(1);
              StreamObserver<UserUpdateRequest> requestObserver = getNextAsyncStub().userUpdatesStream(
                new StreamObserver<UserUpdateResponse>() {                    @Override
                    public void onNext(UserUpdateResponse response) {
                        if (response.getSuccess()) {
                            System.out.printf("  ✅ Updated: %s (Server: %s)%n", 
                                response.getUpdatedUser().getName(), response.getServerId());
                        } else {
                            System.out.printf("  ❌ Update failed: %s (Server: %s)%n", 
                                response.getMessage(), response.getServerId());
                        }
                    }
                    
                    @Override
                    public void onError(Throwable t) {
                        System.err.println("❌ Bidirectional streaming failed: " + t.getMessage());
                        latch.countDown();
                    }
                    
                    @Override
                    public void onCompleted() {
                        System.out.println("✅ Bidirectional streaming completed");
                        latch.countDown();
                    }
                }
            );
              // Send update requests
            int[] userIds = {1, 2, 3, 999}; // 999 should fail
            for (int userId : userIds) {
                UserUpdateRequest request = UserUpdateRequest.newBuilder()
                    .setUserId(userId)
                    .setNewName("Java Updated User " + userId)
                    .build();
                
                requestObserver.onNext(request);
                System.out.println("  📤 Sent update for user " + userId);
                Thread.sleep(500);
            }
            
            requestObserver.onCompleted();
            latch.await(10, TimeUnit.SECONDS);
            
        } catch (Exception e) {
            System.err.println("❌ Bidirectional streaming demo failed: " + e.getMessage());
        }
    }
    
    public void shutdown() {
        for (ManagedChannel channel : channels) {
            try {
                channel.shutdown().awaitTermination(5, TimeUnit.SECONDS);
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
            }
        }
    }
    
    public static void main(String[] args) {
        System.out.println("🚀 Java gRPC Load Balancing Client Demo");
        System.out.println("========================================");
        
        // Parse command line arguments or use defaults
        String[] serverAddresses;
        if (args.length == 0) {
            // Default to C# servers
            serverAddresses = new String[]{"7001", "7002", "7003"};
            System.out.println("📡 Using default servers: https://localhost:7001, https://localhost:7002, https://localhost:7003");
        } else {
            serverAddresses = args;
            System.out.println("📡 Configured servers: " + String.join(", ", serverAddresses));
        }
        
        UserServiceClient client = null;
        try {
            client = new UserServiceClient(serverAddresses);
            
            // Demonstrate different client scenarios
            client.demonstrateLoadBalancing();
            client.demonstrateUnaryOperations();
            client.demonstrateServerStreaming();
            client.demonstrateClientStreaming();
            client.demonstrateBidirectionalStreaming();
            
        } catch (Exception e) {
            System.err.println("❌ Error: " + e.getMessage());
            e.printStackTrace();
        } finally {
            if (client != null) {
                client.shutdown();
            }
        }
        
        System.out.println("\n✅ Demo completed!");
    }
}
