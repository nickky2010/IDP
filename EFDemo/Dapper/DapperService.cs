using Dapper;
using Microsoft.Data.SqlClient;
using EFDemo.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace EFDemo.Dapper;

public class DapperService
{
    private readonly RedisCacheService _cache;
    private readonly ILogger<DapperService> _logger;
    private readonly string _connStr;

    public DapperService(RedisCacheService cache, ILogger<DapperService> logger, IConfiguration config)
    {
        _cache = cache;
        _logger = logger;
        _connStr = config.GetConnectionString("DefaultConnection");
    }

    public async Task RunPerformanceDemoAsync()
    {
        _logger.LogInformation("Dapper Performance Demo");

        using IDbConnection conn = new SqlConnection(_connStr);
        conn.Open();

        // Advanced querying
        var students = await conn.QueryAsync("SELECT * FROM People WHERE PersonType = 'Student'");
        _logger.LogInformation($"Dapper found {students.Count()} students.");

        // Caching
        var cached = await _cache.GetOrSetAsync("dapper_students", async () =>
            (await conn.QueryAsync("SELECT * FROM People WHERE PersonType = 'Student'")).ToList(), TimeSpan.FromMinutes(5));
        _logger.LogInformation($"Cached {cached.Count} students.");
    }
}