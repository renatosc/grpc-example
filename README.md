# gRPC Example

This project demonstrates gRPC communication and load balancing capabilities with both C# and Java implementations.

## Project Structure

```
gRPC-Example/
├── proto/                          # Shared protocol definitions
│   └── user_service.proto         # User service contract
├── csharp-server/                  # C# gRPC Server ✅
│   └── GrpcServer/                 # ASP.NET Core implementation
├── csharp-client/                  # C# gRPC Client ✅
│   └── GrpcClient/                 # Console client with load balancing
├── java-server/                    # Java gRPC Server ✅
│   ├── pom.xml                     # Maven project configuration
│   ├── src/main/java/...           # Server implementation
│   └── target/java-server-1.0.0.jar # Standalone executable JAR
├── java-client/                    # Java gRPC Client ⏳
├── start-servers.ps1               # Multi-server startup script ✅
├── stop-servers.ps1                # Server cleanup script ✅
├── test-servers.ps1                # Health check script ✅
├── docker-compose.yml              # Multi-server setup (coming soon)
└── README.md                       # This file
```

## Features Demonstrated

- ✅ **Cross-language interoperability** (C# ↔ Java)
- ✅ **Multiple RPC patterns**:
  - Unary calls (GetUser, CreateUser)
  - Server streaming (GetUsersByDepartment)
  - Client streaming (BulkCreateUsers)
  - Bidirectional streaming (UserUpdatesStream)
- ✅ **Load balancing** with multiple server instances
- ✅ **Service discovery** via DNS
- ✅ **Health checking** and server identification

## Protocol Buffer Definition

The shared contract is defined in `proto/user_service.proto` and includes:

- **UserService**: Main service with 6 RPC methods
- **User**: Core data model
- **Request/Response messages**: For each RPC method
- **Server identification**: Each response includes server_id for load balancing visibility

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Java 17+ and Maven
- Protocol Buffers Compiler (protoc) - *automatically downloaded by Maven*

### Building the Project

1. **Build C# components**:
   ```powershell
   dotnet build
   ```

2. **Build Java server**:
   ```powershell
   cd java-server
   mvn clean package
   ```
   This creates a standalone JAR with all dependencies included.

### Client Features

The C# client supports flexible server configuration via command-line arguments:

- **Port numbers**: `7001` → `https://localhost:7001`
- **Host:port**: `localhost:7001` → `http://localhost:7001`
- **Full URLs**: `http://localhost:7011` → `http://localhost:7011`

**Examples**:
```powershell
# Use default C# servers (7001, 7002, 7003)
dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj

# Mix of C# and Java servers
dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj 7001 7002 http://localhost:7011

# Java servers only
dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj http://localhost:7011 http://localhost:7012
```

### Running the Load Balancing Demo

**Option 1: Using PowerShell Scripts (Recommended)**

1. **Start multiple server instances**:
   ```powershell
   # Start C# servers only (default)
   .\start-servers.ps1
   
   # Start mixed C# and Java servers
   .\start-servers.ps1 -ServerType mixed
   
   # Start Java servers only
   .\start-servers.ps1 -ServerType java -Ports 7011,7012,7013
   ```

2. **Test server connectivity** (optional):
   ```powershell
   .\test-servers.ps1
   ```

3. **Run the client demo**:
   ```powershell
   # Test with default C# servers
   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj
   
   # Test with mixed servers (C# + Java)
   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj 7001 7002 7003 http://localhost:7011
   
   # Test with specific server addresses
   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj https://localhost:7001 http://localhost:7011
   ```

4. **Stop all servers**:
   ```powershell
   .\stop-servers.ps1
   ```

**Option 2: Manual Server Start**

Start C# servers in separate terminals:
```powershell
# Terminal 1 - C# Server on port 7001
cd csharp-server\GrpcServer
dotnet run --urls=https://localhost:7001

# Terminal 2 - C# Server on port 7002  
cd csharp-server\GrpcServer
dotnet run --urls=https://localhost:7002

# Terminal 3 - C# Server on port 7003
cd csharp-server\GrpcServer
dotnet run --urls=https://localhost:7003
```

Start Java server:
```powershell
# Terminal 4 - Java Server on port 7011
cd java-server
java -jar target\java-server-1.0.0.jar 7011
```

## Cross-Language Interoperability Demonstration

The project successfully demonstrates cross-language gRPC communication. Recent testing shows:

### Test Results
- ✅ **Load Balancing**: Perfect round-robin distribution across mixed servers
- ✅ **C# Client → Java Server**: All gRPC patterns working seamlessly
- ✅ **Protocol Compatibility**: Same `.proto` contract used by both languages
- ✅ **Server Identification**: Unique IDs distinguish C# vs Java servers
  - C# servers: `CSHARP-hostname-pid-uuid`
  - Java servers: `JAVA-hostname-pid-uuid`

### Example Load Distribution
```
Request 01: https://localhost:7001 → CSHARP-LAPTOP-...-301147d6 (C#)
Request 02: https://localhost:7002 → CSHARP-LAPTOP-...-cacd653c (C#)
Request 03: https://localhost:7003 → CSHARP-LAPTOP-...-c4933e9c (C#)
Request 04: http://localhost:7011 → JAVA-RENAT-23688-d3a0f2b6 (Java)
Request 05: https://localhost:7001 → CSHARP-LAPTOP-...-5a4f0e7d (C#)
...
```

All gRPC streaming patterns work identically across both server implementations:
- **Unary**: CreateUser, GetUser
- **Server Streaming**: GetUsersByDepartment
- **Client Streaming**: BulkCreateUsers  
- **Bidirectional Streaming**: UserUpdatesStream

### Next Steps

1. ✅ Step 1: Protocol Buffers Definition (COMPLETED)
2. ✅ Step 2: C# Server Implementation (COMPLETED)
3. ✅ Step 3: C# Client Implementation (COMPLETED)
4. ✅ Step 4: Java Server Implementation (COMPLETED)
5. ⏳ Step 5: Java Client Implementation
6. ⏳ Step 6: Docker Compose & Load Balancing Demo

## Load Balancing Strategy

The project demonstrates:
- Multiple server instances (3 C# + 1 Java in mixed mode)
- Client-side load balancing with round-robin distribution
- Cross-language interoperability (C# client ↔ Java server)
- Request distribution visibility through server_id tracking
- All gRPC streaming patterns working across languages
