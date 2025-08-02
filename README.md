# gRPC Cross-Language Interoperability Example

This project demonstrates comprehensive gRPC communication and load balancing capabilities with full cross-language interoperability between C# and Java implementations. **All components are now complete and fully tested!**

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
├── java-client/                    # Java gRPC Client ✅
│   ├── pom.xml                     # Maven project configuration
│   ├── src/main/java/...           # Client implementation with load balancing
│   └── target/java-client-1.0.0.jar # Standalone executable JAR
├── start-servers.ps1               # Multi-server startup script ✅
├── stop-servers.ps1                # Server cleanup script ✅
├── test-servers.ps1                # Health check script ✅
├── docker-compose.yml              # Multi-server setup (coming soon)
└── README.md                       # This file
```

## Features Demonstrated

- ✅ **Full cross-language interoperability** (C# ↔ Java both directions)
- ✅ **Multiple RPC patterns** (all working across languages):
  - Unary calls (GetUser, CreateUser)
  - Server streaming (GetUsersByDepartment)
  - Client streaming (BulkCreateUsers)
  - Bidirectional streaming (UserUpdatesStream)
- ✅ **Load balancing** with multiple server instances
- ✅ **Mixed server environments** (C# + Java servers simultaneously)
- ✅ **Service discovery** via DNS
- ✅ **Health checking** and server identification
- ✅ **HTTP/2 plaintext** configuration for cross-language compatibility

## Protocol Buffer Definition

The shared contract is defined in `proto/user_service.proto` and includes:

- **UserService**: Main service with 6 RPC methods
- **User**: Core data model
- **Request/Response messages**: For each RPC method
- **Server identification**: Each response includes server_id for load balancing visibility

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Java 11+ and Maven 3.6+
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

3. **Build Java client**:
   ```powershell
   cd java-client
   mvn clean package
   ```
   
   Both Java components create standalone JARs with all dependencies included.

### Client Features

Both C# and Java clients support flexible server configuration via command-line arguments:

**C# Client**:
- **Port numbers**: `7001` → `https://localhost:7001`
- **Host:port**: `localhost:7001` → `http://localhost:7001`  
- **Full URLs**: `http://localhost:7011` → `http://localhost:7011`

**Java Client**:
- **Port numbers**: `7001` → `localhost:7001` (plaintext)
- **Host:port**: `localhost:7001` → `localhost:7001` (plaintext)
- **Full URLs**: `http://localhost:7011` → `localhost:7011` (plaintext)

**Examples**:
```powershell
# C# Client Examples
# Use default C# servers (7001, 7002, 7003)
dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj

# Mix of C# and Java servers
dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj 7001 7002 http://localhost:7011

# Java Client Examples  
# Test against single C# server
java -jar .\java-client\target\java-client-1.0.0.jar 7001

# Test against multiple C# servers
java -jar .\java-client\target\java-client-1.0.0.jar 7001 7002 7003

# Test against mixed environment
java -jar .\java-client\target\java-client-1.0.0.jar 7001 7002 7003 7011
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

3. **Run client demos**:
   ```powershell
   # C# Client - Test with default C# servers
   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj
   
   # C# Client - Test with mixed servers (C# + Java)
   dotnet run --project .\csharp-client\GrpcClient\GrpcClient.csproj 7001 7002 7003 http://localhost:7011
   
   # Java Client - Test with C# servers
   java -jar .\java-client\target\java-client-1.0.0.jar 7001 7002 7003
   
   # Java Client - Test with mixed servers  
   java -jar .\java-client\target\java-client-1.0.0.jar 7001 7002 7003 7011
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

🎯 **The project successfully demonstrates complete bi-directional cross-language gRPC communication!**

### Verified Compatibility Matrix

| Client | Server | Status | All gRPC Patterns |
|--------|--------|--------|--------------------|
| C# | C# | ✅ Working | ✅ Complete |  
| C# | Java | ✅ Working | ✅ Complete |
| Java | C# | ✅ Working | ✅ Complete |
| Java | Java | ✅ Working | ✅ Complete |

### Recent Test Results
- ✅ **Load Balancing**: Perfect round-robin distribution across mixed servers
- ✅ **Java Client → C# Server**: All gRPC patterns working seamlessly  
- ✅ **Java Client → Java Server**: All gRPC patterns working seamlessly
- ✅ **C# Client → Java Server**: All gRPC patterns working seamlessly (previously tested)
- ✅ **Protocol Compatibility**: Same `.proto` contract used by both languages
- ✅ **Server Identification**: Unique IDs distinguish C# vs Java servers
  - C# servers: `CSHARP-hostname-pid-uuid`
  - Java servers: `JAVA-hostname-pid-uuid`

### Example Load Distribution (Java Client → Mixed Servers)
```
🚀 Java gRPC Load Balancing Client Demo
========================================
📡 Configured servers: 7001, 7002, 7003, 7011
📡 Connected to server: localhost:7001
📡 Connected to server: localhost:7002  
📡 Connected to server: localhost:7003
📡 Connected to server: localhost:7011

📊 Load Balancing Demonstration
--------------------------------
Request 01: 7001 → Server ID: CSHARP-LAPTOP-...-a8ac3ffd (C#)
Request 02: 7002 → Server ID: CSHARP-LAPTOP-...-933645ef (C#)
Request 03: 7003 → Server ID: CSHARP-LAPTOP-...-8bcb7b64 (C#)
Request 04: 7011 → Server ID: JAVA-RENAT-23688-d3a0f2b6 (Java)
Request 05: 7001 → Server ID: CSHARP-LAPTOP-...-ad03e728 (C#)
...
```

All gRPC streaming patterns work identically across both client and server implementations:
- **Unary**: CreateUser, GetUser  
- **Server Streaming**: GetUsersByDepartment
- **Client Streaming**: BulkCreateUsers
- **Bidirectional Streaming**: UserUpdatesStream

### Technical Implementation Notes

**For cross-language compatibility, the C# servers are configured with:**
- HTTP/2 plaintext (no TLS) for Java client compatibility
- Port-based configuration via command line arguments
- Automatic protocol detection

**Java clients use:**
- gRPC with Netty transport
- Plaintext connections (no TLS)
- Automatic load balancing across multiple server connections

### Next Steps

1. ✅ Step 1: Protocol Buffers Definition (COMPLETED)
2. ✅ Step 2: C# Server Implementation (COMPLETED)
3. ✅ Step 3: C# Client Implementation (COMPLETED)
4. ✅ Step 4: Java Server Implementation (COMPLETED)
5. ✅ Step 5: Java Client Implementation (COMPLETED)
6. ⏳ Step 6: Docker Compose & Load Balancing Demo

## Load Balancing Strategy

The project demonstrates:
- Multiple server instances (3 C# + 1 Java in mixed mode)
- Client-side load balancing with round-robin distribution
- Cross-language interoperability (C# client ↔ Java server)
- Request distribution visibility through server_id tracking
- All gRPC streaming patterns working across languages
