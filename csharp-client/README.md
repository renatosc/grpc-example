# C# gRPC Client

This client demonstrates various gRPC communication patterns and load balancing capabilities.

## Features

- **Load Balancing Demo**: Shows requests distributed across multiple server instances
- **Unary Operations**: Simple request-response patterns (GetUser, CreateUser)
- **Server Streaming**: Receiving multiple responses from a single request
- **Client Streaming**: Sending multiple requests and receiving a single response
- **Bidirectional Streaming**: Full-duplex communication

## Usage

1. **Start multiple server instances** on different ports:
   ```powershell
   # Terminal 1 - Server on port 7001
   cd ..\csharp-server\GrpcServer
   dotnet run --urls=https://localhost:7001

   # Terminal 2 - Server on port 7002  
   cd ..\csharp-server\GrpcServer
   dotnet run --urls=https://localhost:7002

   # Terminal 3 - Server on port 7003
   cd ..\csharp-server\GrpcServer
   dotnet run --urls=https://localhost:7003
   ```

2. **Run the client**:
   ```powershell
   dotnet run
   ```

## Expected Output

The client will demonstrate:
- Round-robin load balancing across 3 server instances
- Each server responding with its unique server ID
- Different streaming patterns working seamlessly
- Request distribution statistics

## Configuration

The client is configured to connect to:
- `https://localhost:7001`
- `https://localhost:7002` 
- `https://localhost:7003`

You can modify the `ServerAddresses` list in `Program.cs` to use different ports or addresses.
