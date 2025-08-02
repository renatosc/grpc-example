using GrpcServer.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTP/2 without TLS for cross-language compatibility
builder.WebHost.ConfigureKestrel(options =>
{
    // Get port from command line args or environment variable
    var port = args.Length > 0 && int.TryParse(args[0], out int argPort) ? argPort :
               Environment.GetEnvironmentVariable("GRPC_PORT") != null && int.TryParse(Environment.GetEnvironmentVariable("GRPC_PORT"), out int envPort) ? envPort :
               5000;
    
    options.ListenLocalhost(port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<UserServiceImpl>();

app.Run();
