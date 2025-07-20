# gRPC Load Balancing Example

This project demonstrates gRPC communication and load balancing capabilities with both C# and Java implementations.

## Project Structure

```
gRPC-Example/
├── proto/                          # Shared protocol definitions
│   └── user_service.proto         # User service contract
├── csharp-server/                  # C# gRPC Server
├── csharp-client/                  # C# gRPC Client  
├── java-server/                    # Java gRPC Server
├── java-client/                    # Java gRPC Client
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

- .NET 8.0 SDK
- Java 17+ and Maven
- Protocol Buffers Compiler (protoc)

### Next Steps

1. ✅ Step 1: Protocol Buffers Definition (COMPLETED)
2. 🔄 Step 2: C# Server Implementation
3. ⏳ Step 3: C# Client Implementation
4. ⏳ Step 4: Java Server Implementation
5. ⏳ Step 5: Java Client Implementation
6. ⏳ Step 6: Docker Compose & Load Balancing Demo

## Load Balancing Strategy

The project will demonstrate:
- Multiple server instances (3 C# + 2 Java)
- Client-side load balancing with round-robin
- Health checking and automatic failover
- Request distribution visibility through server_id tracking
