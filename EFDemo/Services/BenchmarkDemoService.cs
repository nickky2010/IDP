using BenchmarkDotNet.Running;
using EFDemo.Benchmarks;
using Microsoft.Extensions.Logging;
using System;

namespace EFDemo.Services
{
    public class BenchmarkDemoService
    {
        private readonly ILogger<BenchmarkDemoService> _logger;

        public BenchmarkDemoService(ILogger<BenchmarkDemoService> logger)
        {
            _logger = logger;
        }

        public void RunBenchmarks()
        {
            _logger.LogInformation("--- Running Benchmarks ---");
            _logger.LogWarning("BenchmarkDotNet will run the process multiple times. This will take a few minutes.");
            _logger.LogInformation("For best results, run this in RELEASE mode from the command line: dotnet run -c Release");
            
            Console.WriteLine("Press any key to start the benchmarks...");
            Console.ReadKey();

            BenchmarkRunner.Run<QueryBenchmarks>();
            
            _logger.LogInformation("--- Benchmarks Complete ---");
        }
    }
} 