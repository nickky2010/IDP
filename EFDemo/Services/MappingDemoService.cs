using EFDemo.Data;
using EFDemo.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace EFDemo.Services;

public class MappingDemoService
{
    private readonly string _connectionString;
    private readonly ILogger<MappingDemoService> _logger;

    public MappingDemoService(IConfiguration config, ILogger<MappingDemoService> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public async Task RunAsync()
    {
        foreach (var strategy in new[] { MappingStrategy.Tph, MappingStrategy.Tpt, MappingStrategy.Tpc })
        {
            _logger.LogInformation("\n==============================");
            _logger.LogInformation($"Testing {strategy} mapping strategy");
            _logger.LogInformation("==============================");

            PrintStrategyExplanation(strategy);

            var options = new DbContextOptionsBuilder<MappingDemoContext>()
                .UseSqlServer(_connectionString)
                .Options;

            // Drop and recreate the database for each strategy
            using (var context = new MappingDemoContext(options, strategy))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                // Seed
                context.Users.Add(new Employee { Name = "Alice", Salary = 1000 });
                context.Users.Add(new Manager { Name = "Bob", Department = "IT" });
                await context.SaveChangesAsync();

                // Print schema
                await PrintSchemaAsync();

                // Print sample data
                _logger.LogInformation("Sample Data:");
                if (strategy == MappingStrategy.Tph)
                {
                    var users = await context.Users.ToListAsync();
                    foreach (var user in users)
                    {
                        _logger.LogInformation($"  User: {user.Id}, {user.Name}, Type: {user.GetType().Name}");
                    }
                }
                else // TPT or TPC
                {
                    var users = await context.Users.ToListAsync();
                    var employees = await context.Employees.ToListAsync();
                    var managers = await context.Managers.ToListAsync();
                    _logger.LogInformation("  Users table:");
                    foreach (var user in users)
                        _logger.LogInformation($"    User: {user.Id}, {user.Name}, Type: {user.GetType().Name}");
                    _logger.LogInformation("  Employees table:");
                    foreach (var emp in employees)
                        _logger.LogInformation($"    Employee: {emp.Id}, {emp.Name}, Salary: {emp.Salary}, Type: {emp.GetType().Name}");
                    _logger.LogInformation("  Managers table:");
                    foreach (var mgr in managers)
                        _logger.LogInformation($"    Manager: {mgr.Id}, {mgr.Name}, Department: {mgr.Department}, Type: {mgr.GetType().Name}");
                }
            }
        }
    }

    private void PrintStrategyExplanation(MappingStrategy strategy)
    {
        switch (strategy)
        {
            case MappingStrategy.Tph:
                _logger.LogInformation("TPH (Table-per-Hierarchy): All types are stored in a single table with a discriminator column. This is the default and most efficient for simple inheritance.");
                break;

            case MappingStrategy.Tpt:
                _logger.LogInformation("TPT (Table-per-Type): Each type gets its own table. Base and derived data are joined by PK. Useful for normalized schemas, but can be slower due to joins.");
                break;

            case MappingStrategy.Tpc:
                _logger.LogInformation("TPC (Table-per-Concrete-Type): Each concrete type gets its own table with all properties. No joins, but data is duplicated. Useful for polymorphic queries without joins.");
                break;
        }
    }

    private async Task PrintSchemaAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
        var tableNames = new List<string>();
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }
        _logger.LogInformation("Tables and Columns:");
        foreach (var table in tableNames)
        {
            _logger.LogInformation($"  - {table}");
            await PrintColumnsAsync(conn, table);
        }
    }

    private async Task PrintColumnsAsync(SqlConnection conn, string table)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = $@"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' ORDER BY ORDINAL_POSITION";
        using var reader = await cmd.ExecuteReaderAsync();
        _logger.LogInformation($"    Columns for {table}:");
        while (await reader.ReadAsync())
        {
            _logger.LogInformation($"      - {reader.GetString(0)} ({reader.GetString(1)})");
        }
        reader.Close();
    }
}