using EFDemo.Data;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace EFDemo.Services;

public class TransactionDemoService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TransactionDemoService> _logger;
    private readonly IDistributedCache _redis;
    private readonly IConfiguration _configuration;

    public TransactionDemoService(AppDbContext db, ILogger<TransactionDemoService> logger, IDistributedCache redis, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _redis = redis;
        _configuration = configuration;
    }

    public async Task RunTransactionDemoAsync()
    {
        _logger.LogInformation("Starting Transaction Demo");

        // Local transaction
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _logger.LogDebug("Performing operations in local transaction");
            // ... perform operations
            await tx.CommitAsync();
            _logger.LogInformation("Transaction committed.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Transaction rolled back due to error.");
        }
    }

    public async Task CheckStudentDataConsistencyAsync(int studentId)
    {
        // Read from SQL Server
        var student = await _db.Students.Include(s => s.Address).FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null)
        {
            _logger.LogWarning($"Student with Id={studentId} not found in SQL Server.");
            return;
        }

        // Read from Redis
        var redisKey = $"student:{studentId}";
        var redisValue = await _redis.GetStringAsync(redisKey);
        if (redisValue == null)
        {
            _logger.LogWarning($"Student with Id={studentId} not found in Redis.");
            return;
        }

        // Deserialize Redis value
        dynamic redisStudent = Newtonsoft.Json.JsonConvert.DeserializeObject(redisValue);

        // Compare fields
        bool isConsistent = student.Name == (string)redisStudent.Name &&
                            student.School == (string)redisStudent.School &&
                            student.Address.Street == (string)redisStudent.Street &&
                            student.Address.City == (string)redisStudent.City;

        if (isConsistent)
        {
            _logger.LogInformation($"Data consistency check PASSED for Student Id={studentId}.");
        }
        else
        {
            _logger.LogWarning($"Data consistency check FAILED for Student Id={studentId}.");
            _logger.LogWarning($"SQL: {student.Name}, {student.School}, {student.Address.Street}, {student.Address.City}");
            _logger.LogWarning($"Redis: {redisStudent.Name}, {redisStudent.School}, {redisStudent.Street}, {redisStudent.City}");
        }
    }

    public async Task RunDistributedTransactionAsync()
    {
        _logger.LogInformation("Starting Outbox Pattern Distributed Transaction Demo");
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Create and save a real Student entity with Address
            var student = new EFDemo.Models.Student
            {
                School = "Distributed School",
                Address = new EFDemo.Models.Address
                {
                    Street = "123 EventBus Ave",
                    City = "Rollback City"
                }
            };
            _db.Students.Add(student);
            await _db.SaveChangesAsync();

            // Create Outbox message
            var payload = JsonConvert.SerializeObject(new
            {
                student.Id,
                student.Name,
                student.School,
                student.Address.Street,
                student.Address.City
            });
            var outboxMessage = new EFDemo.Models.OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = "StudentCreated",
                Payload = payload,
                IsProcessed = false
            };
            _db.OutboxMessages.Add(outboxMessage);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            _logger.LogInformation($"Student saved and Outbox message created in the same transaction. Student Id={student.Id}");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Outbox pattern distributed transaction failed.");
            throw;
        }
    }
} 