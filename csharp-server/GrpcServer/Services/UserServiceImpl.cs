using Grpc.Core;
using GrpcExample.UserService;
using System.Collections.Concurrent;

namespace GrpcServer.Services;

public class UserServiceImpl : UserService.UserServiceBase
{
    private readonly ILogger<UserServiceImpl> _logger;
    private readonly string _serverId;
    private readonly DateTime _startTime;
    private int _requestCount;
    
    // In-memory user store for demo purposes 
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextUserId = 1;

    public UserServiceImpl(ILogger<UserServiceImpl> logger)
    {
        _logger = logger;
        _serverId = $"CSHARP-{Environment.MachineName}-{Environment.ProcessId}-{Guid.NewGuid().ToString()[..8]}";
        _startTime = DateTime.UtcNow;
        
        // Seed some initial data
        SeedInitialData();
    }

    private void SeedInitialData()
    {
        var users = new[]
        {
            new User { Id = 1, Name = "Alice Johnson", Email = "alice@company.com", Department = "Engineering", CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new User { Id = 2, Name = "Bob Smith", Email = "bob@company.com", Department = "Marketing", CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new User { Id = 3, Name = "Carol Davis", Email = "carol@company.com", Department = "Engineering", CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new User { Id = 4, Name = "David Wilson", Email = "david@company.com", Department = "Sales", CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            new User { Id = 5, Name = "Eva Brown", Email = "eva@company.com", Department = "Engineering", CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        foreach (var user in users)
        {
            _users[user.Id] = user;
        }
        _nextUserId = 6;
    }

    public override Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        Interlocked.Increment(ref _requestCount);
        _logger.LogInformation("[{ServerId}] GetUser called for ID: {RequestedUserId}", _serverId, request.UserId);

        var response = new GetUserResponse { ServerId = _serverId };
        
        if (_users.TryGetValue(request.UserId, out var user))
        {
            response.User = user;
        }
        
        return Task.FromResult(response);
    }

    public override Task<CreateUserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        Interlocked.Increment(ref _requestCount);
        _logger.LogInformation("[{ServerId}] CreateUser called for: {RequestedName}", _serverId, request.Name);

        var user = new User
        {
            Id = Interlocked.Increment(ref _nextUserId),
            Name = request.Name,
            Email = request.Email,
            Department = request.Department,
            CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _users[user.Id] = user;

        var response = new CreateUserResponse
        {
            User = user,
            ServerId = _serverId,
            Success = true,
            Message = "User created successfully"
        };

        return Task.FromResult(response);
    }

    public override async Task GetUsersByDepartment(GetUsersByDepartmentRequest request, IServerStreamWriter<User> responseStream, ServerCallContext context)
    {
        Interlocked.Increment(ref _requestCount);
        _logger.LogInformation("[{ServerId}] GetUsersByDepartment called for: {RequestedDepartment}", _serverId, request.Department);

        var users = _users.Values
            .Where(u => string.Equals(u.Department, request.Department, StringComparison.OrdinalIgnoreCase))
            .Take(request.Limit > 0 ? request.Limit : int.MaxValue);

        foreach (var user in users)
        {
            await responseStream.WriteAsync(user);
            await Task.Delay(100); // Simulate some processing time
        }
    }

    public override async Task<BulkCreateUsersResponse> BulkCreateUsers(IAsyncStreamReader<CreateUserRequest> requestStream, ServerCallContext context)
    {
        Interlocked.Increment(ref _requestCount);
        _logger.LogInformation("[{_serverId}] BulkCreateUsers called", _serverId);

        var createdUsers = new List<int>();
        var errors = new List<string>();
        var count = 0;

        await foreach (var request in requestStream.ReadAllAsync())
        {
            try
            {
                var user = new User
                {
                    Id = Interlocked.Increment(ref _nextUserId),
                    Name = request.Name,
                    Email = request.Email,
                    Department = request.Department,
                    CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                _users[user.Id] = user;
                createdUsers.Add(user.Id);
                count++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create user {request.Name}: {ex.Message}");
            }
        }

        var response = new BulkCreateUsersResponse
        {
            CreatedCount = count,
            ServerId = _serverId
        };
        
        response.UserIds.AddRange(createdUsers);
        response.Errors.AddRange(errors);

        return response;
    }

    public override async Task UserUpdatesStream(IAsyncStreamReader<UserUpdateRequest> requestStream, IServerStreamWriter<UserUpdateResponse> responseStream, ServerCallContext context)
    {
        Interlocked.Increment(ref _requestCount);
        _logger.LogInformation("[{ServerId}] UserUpdatesStream called", _serverId);

        await foreach (var request in requestStream.ReadAllAsync())
        {
            var response = new UserUpdateResponse { ServerId = _serverId };

            if (_users.TryGetValue(request.UserId, out var user))
            {
                // Update user based on the update type
                switch (request.UpdateTypeCase)
                {
                    case UserUpdateRequest.UpdateTypeOneofCase.NewName:
                        user.Name = request.NewName;
                        break;
                    case UserUpdateRequest.UpdateTypeOneofCase.NewEmail:
                        user.Email = request.NewEmail;
                        break;
                    case UserUpdateRequest.UpdateTypeOneofCase.NewDepartment:
                        user.Department = request.NewDepartment;
                        break;
                }

                response.UpdatedUser = user;
                response.Success = true;
                response.Message = "User updated successfully";
            }
            else
            {
                response.Success = false;
                response.Message = $"User with ID {request.UserId} not found";
            }

            await responseStream.WriteAsync(response);
        }
    }

    public override Task<ServerInfoResponse> GetServerInfo(ServerInfoRequest request, ServerCallContext context)
    {
        Interlocked.Increment(ref _requestCount);
        _logger.LogInformation("[{ServerId}] GetServerInfo called", _serverId);

        var uptime = (long)(DateTime.UtcNow - _startTime).TotalSeconds;
        
        var response = new ServerInfoResponse
        {
            ServerId = _serverId,
            ServerType = "C#",
            Version = "1.0.0",
            UptimeSeconds = uptime,
            TotalRequestsHandled = _requestCount
        };

        response.SupportedFeatures.AddRange([ 
            "Unary RPC", 
            "Server Streaming", 
            "Client Streaming", 
            "Bidirectional Streaming",
            "Health Checking",
            "Load Balancing"
        ]);

        return Task.FromResult(response);
    }
}
