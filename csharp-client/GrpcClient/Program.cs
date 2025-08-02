using Grpc.Net.Client;
using GrpcExample.UserService;
using Grpc.Core;
using System.Text.Json;

namespace GrpcClient;

class Program
{
    private static List<string> ServerAddresses = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 gRPC Load Balancing Client Demo");
        Console.WriteLine("=====================================");

        // Parse command line arguments for server addresses
        if (!ParseServerAddresses(args))
        {
            PrintUsageAndExit();
            return;
        }

        Console.WriteLine($"📡 Configured servers: {string.Join(", ", ServerAddresses)}");
        Console.WriteLine();

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
    }    static bool ParseServerAddresses(string[] args)
    {
        if (args.Length == 0)
        {
            // Use default servers when no arguments provided
            Console.WriteLine("🔧 No server addresses provided, using default servers: 7001, 7002, 7003");
            ServerAddresses.AddRange(new[] { "https://localhost:7001", "https://localhost:7002", "https://localhost:7003" });
            return true;
        }

        foreach (var arg in args)
        {
            // Support different input formats
            string serverAddress;
            
            if (arg.StartsWith("http://") || arg.StartsWith("https://"))
            {
                // Full URL provided
                serverAddress = arg;
            }
            else if (arg.Contains(":"))
            {
                // Host:port format, assume HTTP
                serverAddress = $"http://{arg}";
            }
            else if (int.TryParse(arg, out int port))
            {
                // Just port number, assume localhost
                serverAddress = $"https://localhost:{port}";
            }
            else
            {
                Console.WriteLine($"❌ Invalid server address format: {arg}");
                return false;
            }

            ServerAddresses.Add(serverAddress);
        }

        return ServerAddresses.Count > 0;
    }

    static void PrintUsageAndExit()
    {
        Console.WriteLine("Usage: GrpcClient <server1> <server2> ... <serverN>");
        Console.WriteLine();
        Console.WriteLine("Server address formats supported:");
        Console.WriteLine("  7001                    → https://localhost:7001");
        Console.WriteLine("  localhost:7001          → http://localhost:7001");
        Console.WriteLine("  http://localhost:7001   → http://localhost:7001");
        Console.WriteLine("  https://localhost:7001  → https://localhost:7001");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  GrpcClient 7001 7002 7003");
        Console.WriteLine("  GrpcClient localhost:7001 localhost:7002");
        Console.WriteLine("  GrpcClient https://localhost:7001 http://localhost:7011");
        Console.WriteLine();
        Console.WriteLine("Default servers (if no arguments): 7001, 7002, 7003");
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
