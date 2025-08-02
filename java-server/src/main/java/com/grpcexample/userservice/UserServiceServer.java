package com.grpcexample.userservice;

import io.grpc.Server;
import io.grpc.ServerBuilder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.util.concurrent.TimeUnit;

/**
 * Java gRPC Server implementation for UserService
 * Demonstrates cross-language gRPC interoperability with C#
 */
public class UserServiceServer {
    private static final Logger logger = LoggerFactory.getLogger(UserServiceServer.class);
    
    private Server server;
    private final int port;
    private final String serverId;

    public UserServiceServer(int port) {
        this.port = port;
        this.serverId = generateServerId();
    }

    private String generateServerId() {
        String machineName = System.getProperty("user.name", "unknown");
        long pid = ProcessHandle.current().pid();
        String uuid = java.util.UUID.randomUUID().toString().substring(0, 8);
        return String.format("JAVA-%s-%d-%s", machineName.toUpperCase(), pid, uuid);
    }

    public void start() throws IOException {
        UserServiceImpl userService = new UserServiceImpl(serverId);
        
        server = ServerBuilder.forPort(port)
                .addService(userService)
                .build()
                .start();
                
        logger.info("Server started, listening on port {}", port);
        logger.info("Server ID: {}", serverId);
        
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            logger.info("*** Shutting down gRPC server since JVM is shutting down");
            try {
                UserServiceServer.this.stop();
            } catch (InterruptedException e) {
                logger.error("Error during shutdown", e);
            }
            logger.info("*** Server shut down");
        }));
    }

    public void stop() throws InterruptedException {
        if (server != null) {
            server.shutdown().awaitTermination(30, TimeUnit.SECONDS);
        }
    }

    public void blockUntilShutdown() throws InterruptedException {
        if (server != null) {
            server.awaitTermination();
        }
    }

    public static void main(String[] args) throws IOException, InterruptedException {
        // Default port, can be overridden by command line argument or environment variable
        int port = 8001;
        
        // Check for port in environment variable (for Docker/PowerShell script compatibility)
        String envPort = System.getenv("GRPC_PORT");
        if (envPort != null && !envPort.isEmpty()) {
            try {
                port = Integer.parseInt(envPort);
            } catch (NumberFormatException e) {
                logger.warn("Invalid GRPC_PORT environment variable: {}, using default {}", envPort, port);
            }
        }
        
        // Check for port in command line arguments
        if (args.length > 0) {
            try {
                port = Integer.parseInt(args[0]);
            } catch (NumberFormatException e) {
                logger.warn("Invalid port argument: {}, using default {}", args[0], port);
            }
        }

        logger.info("Starting Java gRPC UserService Server on port {}", port);
        
        final UserServiceServer server = new UserServiceServer(port);
        server.start();
        server.blockUntilShutdown();
    }
}
