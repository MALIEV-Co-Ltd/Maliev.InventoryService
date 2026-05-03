using Maliev.InventoryService.Infrastructure.Consumers;
using Maliev.InventoryService.Domain.Clients;
using Maliev.InventoryService.Api.Services;
using Maliev.InventoryService.Infrastructure.Persistence;
using Maliev.InventoryService.Application.Abstractions;
using Maliev.InventoryService.Infrastructure.Services;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Program.Log.StartingHost(bootstrapLogger, "Inventory Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume();

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults();
    builder.AddStandardMiddleware();
    builder.AddServiceMeters("inventory-meter");

    // Register DbContext
    builder.AddPostgresDbContext<InventoryDbContext>(connectionName: "InventoryDbContext");

    // Register Application Services
    builder.Services.AddScoped<IInventoryService, InventoryService>();

    builder.AddStandardCache("inventory:");

    // MassTransit with RabbitMq
    builder.AddMassTransitWithRabbitMq(configure: x =>
    {
        x.AddConsumer<JobStartedEventConsumer>();

        // The underlying implementation handles UsingRabbitMq, but we can add global retry here if needed
        // though it's typically handled in the AddMassTransitWithRabbitMq helper.
    });

    // JWT Authentication (also registers AddPermissionAuthorization internally)
    builder.AddJwtAuthentication();

    // IAM Registration
    builder.AddIAMServiceClient("inventory");
    builder.Services.AddIAMRegistration<InventoryIAMRegistrationService>("inventory");


    // Authenticated client for MaterialService calls
    builder.AddAuthenticatedServiceClient<IMaterialServiceClient, Maliev.InventoryService.Infrastructure.HttpClients.MaterialServiceClient>("MaterialService", sourceServiceName: "InventoryService");

    // --- API Configuration ---
    builder.AddStandardCors();
    builder.AddDefaultApiVersioning();

    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Inventory Service API",
            description: "Manages material inventory and stock levels in the shop floor.");
    }

    builder.AddStandardRateLimiting();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    builder.Services.AddControllers();

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<InventoryDbContext>();

    // --- Middleware Pipeline ---
    app.UseStandardMiddleware();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseResponseCompression();
    app.UseRouting();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // --- Endpoints ---
    app.MapControllers();
    app.MapDefaultEndpoints(servicePrefix: "inventory");
    app.MapApiDocumentation(servicePrefix: "inventory");

    Program.Log.ServiceStarted(logger, "Inventory Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Program.Log.HostTerminated(bootstrapLogger, ex, "Inventory Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the Inventory Service application.
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);
    }
}
