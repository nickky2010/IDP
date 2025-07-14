using EFDemo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFDemo.Services;

public class MigrationDemoService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MigrationDemoService> _logger;

    public MigrationDemoService(AppDbContext db, ILogger<MigrationDemoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunMigrationDemoAsync()
    {
        _logger.LogInformation("Starting Migration & Seeding Demo");

        // Automated migration
        _logger.LogDebug("Applying migrations");
        await _db.Database.MigrateAsync();

        // Seeding
        const int requiredCount = 5;
        var currentCount = await _db.People.CountAsync();
        if (currentCount < requiredCount)
        {
            _logger.LogWarning($"Seeding {requiredCount} people with addresses for benchmarks (existing: {currentCount})...");
            _db.People.RemoveRange(await _db.People.ToListAsync());
            await _db.SaveChangesAsync();

            var people = new List<EFDemo.Models.Person>(requiredCount);
            for (int i = 0; i < requiredCount; i++)
            {
                if (i % 2 == 0)
                {
                    people.Add(new EFDemo.Models.Student
                    {
                        Name = $"Student {i}",
                        School = $"School {i % 100}",
                        Address = new EFDemo.Models.Address { Street = $"{i} Main St", City = $"City {i % 50}" }
                    });
                }
                else
                {
                    people.Add(new EFDemo.Models.Teacher
                    {
                        Name = $"Teacher {i}",
                        Subject = $"Subject {i % 20}",
                        Address = new EFDemo.Models.Address { Street = $"{i} Oak Ave", City = $"City {i % 50}" }
                    });
                }
            }
            _db.People.AddRange(people);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"Seeded {requiredCount} people with addresses.");
        }
        else
        {
            _logger.LogInformation($"Data already seeded with {currentCount} people.");
        }
    }
} 