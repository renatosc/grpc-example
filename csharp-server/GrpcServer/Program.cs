using GrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<UserServiceImpl>();
app.MapGet("/", () => "gRPC User Service Server is running. Use a gRPC client to connect.");

app.Run();
