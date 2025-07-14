using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EFDemo.Data;
using EFDemo.Services;
using EFDemo.Caching;
using EFDemo.Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

// Setup Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Level:u}: {Message:lj}{NewLine}")
    .CreateLogger();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

// Get connection strings from environment variables or use Docker defaults
var sqlConnectionString = configuration.GetConnectionString("DefaultConnection");
var redisConnectionString = Environment.GetEnvironmentVariable("EFDEMO_REDIS") ?? "127.0.0.1:6379";

// Replace built-in logging with Serilog
services.AddLogging(cfg =>
{
    cfg.ClearProviders();
    cfg.AddSerilog();
});

// EF Core
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(sqlConnectionString)
           .EnableSensitiveDataLogging()
           .EnableDetailedErrors());

// Redis Caching
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "EFDemo:";
});
services.AddSingleton<RedisCacheService>();

// Dapper
services.AddTransient<DapperService>();

// Demo Services
services.AddTransient<EfCoreDemoService>();
services.AddTransient<DapperDemoService>();
services.AddTransient<TransactionDemoService>();
services.AddTransient<MigrationDemoService>();
services.AddTransient<BenchmarkDemoService>();
services.AddTransient<MappingDemoService>();

services.AddHostedService<OutboxPublisherService>();
services.AddHostedService<StudentEventConsumerService>();

var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Main");
logger.LogInformation("Application started.");

Console.WriteLine("EF Core & Dapper Advanced Demo");
Console.WriteLine("1. Performance Optimization Demos (EF Core)");
Console.WriteLine("2. Run Performance Benchmarks (EF Core vs Dapper)");
Console.WriteLine("3. Complex Mappings Demo (TPH/TPT/TPC)");
Console.WriteLine("4. Transactions Demo");
Console.WriteLine("5. Migrations & Seeding Demo");
Console.WriteLine("0. Exit");

while (true)
{
    Console.Write("Select option: ");
    var input = Console.ReadLine();
    logger.LogDebug($"User selected option: {input}");
    switch (input)
    {
        case "1":
            await provider.GetRequiredService<EfCoreDemoService>().RunPerformanceOptimizationDemosAsync();
            break;

        case "2":
            provider.GetRequiredService<BenchmarkDemoService>().RunBenchmarks();
            break;

        case "3":
            await provider.GetRequiredService<MappingDemoService>().RunAsync();
            break;

        case "4":
            await provider.GetRequiredService<TransactionDemoService>().RunDistributedTransactionAsync();
            break;

        case "5":
            await provider.GetRequiredService<MigrationDemoService>().RunMigrationDemoAsync();
            break;

        case "0":
            logger.LogInformation("Application exiting.");
            return;

        default:
            logger.LogWarning($"Invalid option: {input}");
            Console.WriteLine("Invalid option.");
            break;
    }
}