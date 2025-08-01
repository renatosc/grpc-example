using Grpc.Net.Client;
using GrpcExample.UserService;
using Grpc.Core;
using System.Text.Json;

namespace GrpcClient;

class Program
{
    private static readonly List<string> ServerAddresses = new()
    {
        "https://localhost:7001", // Server 1
        "https://localhost:7002", // Server 2  
        "https://localhost:7003"  // Server 3
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 gRPC Load Balancing Client Demo");
        Console.WriteLine("=====================================");

        try
        {
            // Demonstrate different client scenarios
            await DemonstrateLoadBalancing();
            await DemonstrateUnaryOperations();
            await DemonstrateServerStreaming();
            await DemonstrateClientStreaming();
            await DemonstrateBidirectionalStreaming();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        Console.WriteLine("\n✅ Demo completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task DemonstrateLoadBalancing()
    {
        Console.WriteLine("\n📊 Load Balancing Demonstration");
        Console.WriteLine("--------------------------------");

        var serverHitCount = new Dictionary<string, int>();

        // Make 15 requests using round-robin across servers
        for (int i = 1; i <= 15; i++)
        {
            var serverAddress = ServerAddresses[(i - 1) % ServerAddresses.Count];
            
            try
            {
                using var channel = GrpcChannel.ForAddress(serverAddress);
                var client = new UserService.UserServiceClient(channel);

                var response = await client.GetServerInfoAsync(new ServerInfoRequest());
                
                var serverId = response.ServerId;
                serverHitCount[serverId] = serverHitCount.GetValueOrDefault(serverId, 0) + 1;

                Console.WriteLine($"Request {i:D2}: {serverAddress} → Server ID: {serverId} ({response.ServerType})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request {i:D2}: {serverAddress} → ❌ Failed: {ex.Message}");
            }

            await Task.Delay(100); // Small delay between requests
        }

        Console.WriteLine("\n📈 Load Distribution Summary:");
        foreach (var kvp in serverHitCount.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} requests");
        }
    }
    static async Task DemonstrateUnaryOperations()
    {
        Console.WriteLine("\n🔄 Unary Operations Demo");
        Console.WriteLine("------------------------");

        // Use the same channel/connection for both operations to ensure they hit the same server instance
        using var channel = GrpcChannel.ForAddress(ServerAddresses[0]);
        var client = new UserService.UserServiceClient(channel);

        // Create a new user
        Console.WriteLine("Creating new user...");
        var createResponse = await client.CreateUserAsync(new CreateUserRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Department = "Engineering"
        });

        Console.WriteLine($"✅ User created: ID={createResponse.User.Id}, Server={createResponse.ServerId}");

        // Get the user back using the SAME client instance
        Console.WriteLine($"Fetching user with ID {createResponse.User.Id}...");
        var getResponse = await client.GetUserAsync(new GetUserRequest
        {
            UserId = createResponse.User.Id
        });

        if (getResponse.User != null)
        {
            Console.WriteLine($"✅ User retrieved: {getResponse.User.Name} ({getResponse.User.Email})");
            Console.WriteLine($"   Handled by server: {getResponse.ServerId}");
        }
        else
        {
            Console.WriteLine("❌ User not found - This can happen in distributed systems without shared storage!");
        }
    }
    static async Task DemonstrateServerStreaming()
    {
        Console.WriteLine("\n📡 Server Streaming Demo");
        Console.WriteLine("------------------------");

        // Try to find an available server
        foreach (var serverAddress in ServerAddresses)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress(serverAddress);
                var client = new UserService.UserServiceClient(channel);

                Console.WriteLine($"Getting users from Engineering department via {serverAddress}...");

                using var stream = client.GetUsersByDepartment(new GetUsersByDepartmentRequest
                {
                    Department = "Engineering",
                    Limit = 10
                });

                var userCount = 0;
                while (await stream.ResponseStream.MoveNext())
                {
                    var user = stream.ResponseStream.Current;
                    userCount++;
                    Console.WriteLine($"  👤 {user.Name} ({user.Email}) - ID: {user.Id}");
                }

                Console.WriteLine($"✅ Streamed {userCount} users from Engineering department");
                return; // Success, exit the method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Server {serverAddress} unavailable: {ex.Message.Split('.')[0]}");
            }
        }

        Console.WriteLine("❌ No servers available for streaming demo");
    }   
    static async Task DemonstrateClientStreaming()
    {
        Console.WriteLine("\n📤 Client Streaming Demo");
        Console.WriteLine("------------------------");

        // Use the first available server
        using var channel = GrpcChannel.ForAddress(ServerAddresses[0]);
        var client = new UserService.UserServiceClient(channel);

        Console.WriteLine("Bulk creating users...");

        using var stream = client.BulkCreateUsers();

        // Send multiple user creation requests
        var users = new[]
        {
            new CreateUserRequest { Name = "Alice Smith", Email = "alice@company.com", Department = "Marketing" },
            new CreateUserRequest { Name = "Bob Johnson", Email = "bob@company.com", Department = "Sales" },
            new CreateUserRequest { Name = "Carol Davis", Email = "carol@company.com", Department = "Engineering" },
            new CreateUserRequest { Name = "David Wilson", Email = "david@company.com", Department = "Support" }
        };

        foreach (var userRequest in users)
        {
            await stream.RequestStream.WriteAsync(userRequest);
            Console.WriteLine($"  📤 Sent: {userRequest.Name}");
            await Task.Delay(200); // Simulate some delay
        }

        await stream.RequestStream.CompleteAsync();

        var response = await stream;
        Console.WriteLine($"✅ Bulk creation completed on server {response.ServerId}");
        Console.WriteLine($"   Created {response.CreatedCount} users");
        Console.WriteLine($"   New user IDs: [{string.Join(", ", response.UserIds)}]");

        if (response.Errors.Any())
        {
            Console.WriteLine($"   Errors: {string.Join(", ", response.Errors)}");
        }
    }

    static async Task DemonstrateBidirectionalStreaming()
    {
        Console.WriteLine("\n🔄 Bidirectional Streaming Demo");
        Console.WriteLine("-------------------------------");

        using var channel = GrpcChannel.ForAddress(ServerAddresses[0]);
        var client = new UserService.UserServiceClient(channel);

        Console.WriteLine("Starting bidirectional user updates stream...");

        using var stream = client.UserUpdatesStream();        // Start a task to read responses
        var readTask = Task.Run(async () =>
        {
            while (await stream.ResponseStream.MoveNext())
            {
                var response = stream.ResponseStream.Current;
                if (response.Success)
                {
                    Console.WriteLine($"  ✅ Updated: {response.UpdatedUser.Name} (Server: {response.ServerId})");
                }
                else
                {
                    Console.WriteLine($"  ❌ Update failed: {response.Message} (Server: {response.ServerId})");
                }
            }
        });

        // Send update requests
        var updates = new[]
        {
            new UserUpdateRequest { UserId = 1, NewName = "Alice Johnson Updated" },
            new UserUpdateRequest { UserId = 2, NewEmail = "bob.updated@company.com" },
            new UserUpdateRequest { UserId = 3, NewDepartment = "DevOps" },
            new UserUpdateRequest { UserId = 999, NewName = "Non-existent User" } // This should fail
        };

        foreach (var update in updates)
        {
            await stream.RequestStream.WriteAsync(update);
            Console.WriteLine($"  📤 Sent update for user {update.UserId}");
            await Task.Delay(500);
        }

        await stream.RequestStream.CompleteAsync();
        await readTask;

        Console.WriteLine("✅ Bidirectional streaming completed");
    }
}
