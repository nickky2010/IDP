using EFDemo.Data;
using EFDemo.Models;
using Microsoft.EntityFrameworkCore;
using EFDemo.Caching;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace EFDemo.Services;

public class EfCoreDemoService
{
    private readonly AppDbContext _db;
    private readonly RedisCacheService _cache;
    private readonly ILogger<EfCoreDemoService> _logger;

    public EfCoreDemoService(AppDbContext db, RedisCacheService cache, ILogger<EfCoreDemoService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task RunPerformanceOptimizationDemosAsync()
    {
        _logger.LogInformation("=============================================");
        _logger.LogInformation("  RUNNING PERFORMANCE OPTIMIZATION DEMOS");
        _logger.LogInformation("=============================================\n");

        await DemonstrateNPlusOneProblem();
        await DemonstrateEagerLoading();
        await DemonstrateExplicitLoading();
        await DemonstrateNoTracking();
        await DemonstrateProjection();
        await DemonstrateCaching();
    }

    private async Task DemonstrateNPlusOneProblem()
    {
        _logger.LogInformation("--- 1. N+1 Problem Demonstration ---");
        _logger.LogInformation("Fetching all people, then iterating and LAZILY fetching each address.");
        _logger.LogWarning("Watch the logs: you will see one query for all people, then N queries for each address.");

        var people = await _db.People.ToListAsync(); // Query 1: Get all people
        foreach (var person in people)
        {
            // For each person, a NEW query is sent to get their address. This is the N+1 problem.
            var address = await _db.Addresses.FirstOrDefaultAsync(a => a.PersonId == person.Id);
            _logger.LogInformation($"  > Person: {person.Name}, Address: {address?.Street ?? "N/A"}");
        }
        _logger.LogInformation("--- N+1 Problem Complete ---\n");
    }

    private async Task DemonstrateEagerLoading()
    {
        _logger.LogInformation("--- 2. Eager Loading (Fix for N+1) ---");
        _logger.LogInformation("Fetching all people and their addresses in a SINGLE query using .Include().");
        _logger.LogWarning("Watch the logs: you will see only ONE query with a JOIN.");

        var peopleWithAddresses = await _db.People.Include(p => p.Address).ToListAsync();
        foreach (var person in peopleWithAddresses)
        {
            _logger.LogInformation($"  > Person: {person.Name}, Address: {person.Address?.Street ?? "N/A"}");
        }
        _logger.LogInformation("--- Eager Loading Complete ---\n");
    }

    private async Task DemonstrateExplicitLoading()
    {
        _logger.LogInformation("--- 3. Explicit Loading Demonstration ---");
        _logger.LogInformation("Fetching a single person first, then explicitly loading their address on demand.");

        var person = await _db.People.FirstOrDefaultAsync();
        if (person != null)
        {
            _logger.LogWarning($"Step 1: Person '{person.Name}' loaded. Address is initially NOT loaded (null).");

            // The Address property is null here. Now, we explicitly load it.
            await _db.Entry(person).Reference(p => p.Address).LoadAsync();
            _logger.LogWarning($"Step 2: Address for '{person.Name}' explicitly loaded in a separate query.");
            _logger.LogInformation($"  > Loaded Address: {person.Address?.Street}, {person.Address?.City}");
        }
        _logger.LogInformation("--- Explicit Loading Complete ---\n");
    }

    private async Task DemonstrateNoTracking()
    {
        _logger.LogInformation("--- 4. AsNoTracking() Demonstration ---");
        _logger.LogInformation("Fetching read-only data. EF Core will not track changes, improving performance.");

        var readOnlyPeople = await _db.People.AsNoTracking().ToListAsync();
        _logger.LogInformation($"  > Fetched {readOnlyPeople.Count} people for read-only purposes.");
        _logger.LogInformation("--- AsNoTracking() Complete ---\n");
    }

    private async Task DemonstrateProjection()
    {
        _logger.LogInformation("--- 5. Projection Demonstration ---");
        _logger.LogInformation("Fetching only the 'Name' property instead of the whole entity.");
        _logger.LogWarning("This is highly efficient as it reduces data transfer and avoids tracking overhead.");

        var names = await _db.People.Select(p => p.Name).ToListAsync();
        foreach (var name in names)
        {
            _logger.LogInformation($"  > Fetched Name: {name}");
        }
        _logger.LogInformation("--- Projection Complete ---\n");
    }

    private async Task DemonstrateCaching()
    {
        _logger.LogInformation("--- 6. Caching Demonstration (Redis) ---");
        _logger.LogInformation("Attempting to fetch students from cache. If not found, query DB and cache the result.");

        var cachedStudents = await _cache.GetOrSetAsync("all_students", async () =>
        {
            _logger.LogWarning("  > Cache miss! Fetching from database and setting cache for next time.");
            // Use projection to avoid circular reference issues when serializing to JSON
            return await _db.Students
                .Include(s => s.Address)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.School,
                    AddressStreet = s.Address != null ? s.Address.Street : null,
                    AddressCity = s.Address != null ? s.Address.City : null
                })
                .ToListAsync();
        }, TimeSpan.FromSeconds(30));

        _logger.LogInformation($"  > Fetched {cachedStudents.Count} students from cache/DB.");
        _logger.LogInformation("--- Caching Complete ---\n");
    }

    public async Task RunMappingsDemoAsync()
    {
        _logger.LogInformation("Starting Mappings Demo (TPH/TPT/TPC)");
        var people = await _db.People.ToListAsync();
        foreach (var p in people)
        {
            _logger.LogInformation($"Person: {p.Name} ({p.GetType().Name})");
        }
    }
}