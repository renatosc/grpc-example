package com.grpcexample.userservice;

import com.grpcexample.userservice.UserServiceGrpc.UserServiceImplBase;
import com.grpcexample.userservice.UserServiceProto.*;
import io.grpc.stub.StreamObserver;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.Instant;
import java.util.List;
import java.util.ArrayList;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Java implementation of the UserService gRPC service
 * Demonstrates all gRPC communication patterns: unary, server streaming, client streaming, and bidirectional streaming
 */
public class UserServiceImpl extends UserServiceImplBase {
    private static final Logger logger = LoggerFactory.getLogger(UserServiceImpl.class);
    
    private final String serverId;
    private final long startTime;
    private final AtomicLong requestCount = new AtomicLong(0);
    private final ConcurrentHashMap<Integer, User> users = new ConcurrentHashMap<>();
    private final AtomicInteger nextUserId = new AtomicInteger(1000); // Start Java users at 1000 to distinguish from C#
    
    public UserServiceImpl(String serverId) {
        this.serverId = serverId;
        this.startTime = Instant.now().getEpochSecond();
        initializeTestData();
    }
    
    private void initializeTestData() {
        // Add some test users
        long timestamp = Instant.now().getEpochSecond();
        
        User user1 = User.newBuilder()
                .setId(1001)
                .setName("Alice Java")
                .setEmail("alice.java@company.com")
                .setDepartment("Engineering")
                .setCreatedTimestamp(timestamp)
                .build();
                
        User user2 = User.newBuilder()
                .setId(1002)
                .setName("Bob Java")
                .setEmail("bob.java@company.com")
                .setDepartment("Marketing")
                .setCreatedTimestamp(timestamp)
                .build();
                
        User user3 = User.newBuilder()
                .setId(1003)
                .setName("Carol Java")
                .setEmail("carol.java@company.com")
                .setDepartment("Engineering")
                .setCreatedTimestamp(timestamp)
                .build();
                
        User user4 = User.newBuilder()
                .setId(1004)
                .setName("David Java")
                .setEmail("david.java@company.com")
                .setDepartment("Sales")
                .setCreatedTimestamp(timestamp)
                .build();
                
        User user5 = User.newBuilder()
                .setId(1005)
                .setName("Eva Java")
                .setEmail("eva.java@company.com")
                .setDepartment("Engineering")
                .setCreatedTimestamp(timestamp)
                .build();
                
        users.put(user1.getId(), user1);
        users.put(user2.getId(), user2);
        users.put(user3.getId(), user3);
        users.put(user4.getId(), user4);
        users.put(user5.getId(), user5);
        
        nextUserId.set(1006);
        
        logger.info("Initialized test data with {} users", users.size());
    }
      
    @Override
    public void getUser(GetUserRequest request, StreamObserver<GetUserResponse> responseObserver) {
        requestCount.incrementAndGet();
        logger.info("[{}] GetUser called for ID: {}", serverId, request.getUserId());
        
        GetUserResponse.Builder responseBuilder = GetUserResponse.newBuilder()
                .setServerId(serverId);
                
        User user = users.get(request.getUserId());
        if (user != null) {
            responseBuilder.setUser(user);
        }
        
        responseObserver.onNext(responseBuilder.build());
        responseObserver.onCompleted();
    }
    
    @Override
    public void createUser(CreateUserRequest request, StreamObserver<CreateUserResponse> responseObserver) {
        requestCount.incrementAndGet();
        logger.info("[{}] CreateUser called for: {}", serverId, request.getName());
        
        try {
            User user = User.newBuilder()
                    .setId(nextUserId.getAndIncrement())
                    .setName(request.getName())
                    .setEmail(request.getEmail())
                    .setDepartment(request.getDepartment())
                    .setCreatedTimestamp(Instant.now().getEpochSecond())
                    .build();
                    
            users.put(user.getId(), user);
            
            CreateUserResponse response = CreateUserResponse.newBuilder()
                    .setUser(user)
                    .setServerId(serverId)
                    .setSuccess(true)
                    .setMessage("User created successfully")
                    .build();
                    
            responseObserver.onNext(response);
            responseObserver.onCompleted();
            
            logger.info("[{}] Created user with ID: {}", serverId, user.getId());
        } catch (Exception e) {
            logger.error("Error creating user", e);
            
            CreateUserResponse response = CreateUserResponse.newBuilder()
                    .setServerId(serverId)
                    .setSuccess(false)
                    .setMessage("Error creating user: " + e.getMessage())
                    .build();
                    
            responseObserver.onNext(response);
            responseObserver.onCompleted();
        }
    }
    
    @Override
    public void getUsersByDepartment(GetUsersByDepartmentRequest request, StreamObserver<User> responseObserver) {
        requestCount.incrementAndGet();
        logger.info("[{}] GetUsersByDepartment called for: {}", serverId, request.getDepartment());
        
        try {
            int limit = request.getLimit() > 0 ? request.getLimit() : Integer.MAX_VALUE;
            int count = 0;
            
            for (User user : users.values()) {
                if (count >= limit) break;
                
                if (user.getDepartment().equalsIgnoreCase(request.getDepartment())) {
                    responseObserver.onNext(user);
                    count++;
                    
                    // Add a small delay to demonstrate streaming
                    try {
                        Thread.sleep(100);
                    } catch (InterruptedException e) {
                        Thread.currentThread().interrupt();
                        break;
                    }
                }
            }
            
            responseObserver.onCompleted();
            logger.info("[{}] Completed streaming {} users for department: {}", serverId, count, request.getDepartment());
        } catch (Exception e) {
            logger.error("Error streaming users by department", e);
            responseObserver.onError(e);
        }
    }
    
    @Override
    public StreamObserver<CreateUserRequest> bulkCreateUsers(StreamObserver<BulkCreateUsersResponse> responseObserver) {
        requestCount.incrementAndGet();
        logger.info("[{}] BulkCreateUsers called", serverId);
        
        return new StreamObserver<CreateUserRequest>() {
            private final List<Integer> createdUserIds = new ArrayList<>();
            private final List<String> errors = new ArrayList<>();
            
            @Override
            public void onNext(CreateUserRequest request) {
                try {
                    User user = User.newBuilder()
                            .setId(nextUserId.getAndIncrement())
                            .setName(request.getName())
                            .setEmail(request.getEmail())
                            .setDepartment(request.getDepartment())
                            .setCreatedTimestamp(Instant.now().getEpochSecond())
                            .build();
                            
                    users.put(user.getId(), user);
                    createdUserIds.add(user.getId());
                    
                    logger.info("[{}] Created user via bulk stream: {} (ID: {})", serverId, user.getName(), user.getId());
                } catch (Exception e) {
                    String errorMsg = "Failed to create user " + request.getName() + ": " + e.getMessage();
                    errors.add(errorMsg);
                    logger.error(errorMsg, e);
                }
            }
            
            @Override
            public void onError(Throwable t) {
                logger.error("[{}] Error in bulkCreateUsers stream", serverId, t);
            }
            
            @Override
            public void onCompleted() {
                BulkCreateUsersResponse.Builder responseBuilder = BulkCreateUsersResponse.newBuilder()
                        .setCreatedCount(createdUserIds.size())
                        .setServerId(serverId);
                        
                responseBuilder.addAllUserIds(createdUserIds);
                responseBuilder.addAllErrors(errors);
                        
                responseObserver.onNext(responseBuilder.build());
                responseObserver.onCompleted();
                
                logger.info("[{}] Completed bulkCreateUsers - created {} users with {} errors", 
                           serverId, createdUserIds.size(), errors.size());
            }
        };
    }
    
    @Override
    public StreamObserver<UserUpdateRequest> userUpdatesStream(StreamObserver<UserUpdateResponse> responseObserver) {
        requestCount.incrementAndGet();
        logger.info("[{}] UserUpdatesStream bidirectional stream started", serverId);
        
        return new StreamObserver<UserUpdateRequest>() {
            @Override
            public void onNext(UserUpdateRequest request) {
                try {
                    logger.info("[{}] Received user update for ID: {}", serverId, request.getUserId());
                    
                    UserUpdateResponse.Builder responseBuilder = UserUpdateResponse.newBuilder()
                            .setServerId(serverId);
                    
                    User user = users.get(request.getUserId());
                    if (user != null) {
                        // Create updated user
                        User.Builder userBuilder = user.toBuilder();
                        
                        switch (request.getUpdateTypeCase()) {
                            case NEW_NAME:
                                userBuilder.setName(request.getNewName());
                                break;
                            case NEW_EMAIL:
                                userBuilder.setEmail(request.getNewEmail());
                                break;
                            case NEW_DEPARTMENT:
                                userBuilder.setDepartment(request.getNewDepartment());
                                break;
                            default:
                                responseBuilder.setSuccess(false)
                                              .setMessage("Unknown update type");
                                responseObserver.onNext(responseBuilder.build());
                                return;
                        }
                        
                        User updatedUser = userBuilder.build();
                        users.put(updatedUser.getId(), updatedUser);
                        
                        responseBuilder.setUpdatedUser(updatedUser)
                                      .setSuccess(true)
                                      .setMessage("User updated successfully");
                    } else {
                        responseBuilder.setSuccess(false)
                                      .setMessage("User with ID " + request.getUserId() + " not found");
                    }
                    
                    responseObserver.onNext(responseBuilder.build());
                } catch (Exception e) {
                    logger.error("[{}] Error processing user update", serverId, e);
                    
                    UserUpdateResponse errorResponse = UserUpdateResponse.newBuilder()
                            .setServerId(serverId)
                            .setSuccess(false)
                            .setMessage("Error processing update: " + e.getMessage())
                            .build();
                    responseObserver.onNext(errorResponse);
                }
            }
            
            @Override
            public void onError(Throwable t) {
                logger.error("[{}] Error in userUpdatesStream", serverId, t);
            }
            
            @Override
            public void onCompleted() {
                responseObserver.onCompleted();
                logger.info("[{}] UserUpdatesStream bidirectional stream completed", serverId);
            }
        };
    }
    
    @Override
    public void getServerInfo(ServerInfoRequest request, StreamObserver<ServerInfoResponse> responseObserver) {
        requestCount.incrementAndGet();
        logger.info("[{}] GetServerInfo called", serverId);
        
        long uptime = Instant.now().getEpochSecond() - startTime;
          ServerInfoResponse.Builder responseBuilder = ServerInfoResponse.newBuilder()
                .setServerId(serverId)
                .setServerType("Java")
                .setVersion("1.0.0")
                .setUptimeSeconds(uptime)
                .setTotalRequestsHandled((int) requestCount.get());
                
        responseBuilder.addSupportedFeatures("Unary RPC");
        responseBuilder.addSupportedFeatures("Server Streaming");
        responseBuilder.addSupportedFeatures("Client Streaming");
        responseBuilder.addSupportedFeatures("Bidirectional Streaming");
        responseBuilder.addSupportedFeatures("Health Checking");
        responseBuilder.addSupportedFeatures("Load Balancing");
                
        responseObserver.onNext(responseBuilder.build());
        responseObserver.onCompleted();
    }
}
