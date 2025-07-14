using EFDemo.Dapper;
using Microsoft.Extensions.Logging;

namespace EFDemo.Services;

public class DapperDemoService
{
    private readonly DapperService _dapperService;
    private readonly ILogger<DapperDemoService> _logger;

    public DapperDemoService(DapperService dapperService, ILogger<DapperDemoService> logger)
    {
        _dapperService = dapperService;
        _logger = logger;
    }

    public async Task RunPerformanceDemoAsync()
    {
        _logger.LogInformation("Starting Dapper Performance Demo");
        await _dapperService.RunPerformanceDemoAsync();
        _logger.LogInformation("Finished Dapper Performance Demo");
    }
} 