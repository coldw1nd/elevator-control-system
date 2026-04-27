using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ElevatorControlSystem.Application;

namespace ElevatorControlSystem.Infrastructure;

public sealed class SimulationEngineHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SimulationEngineHostedService> _logger;
    private readonly TimeSpan _tickInterval;

    public SimulationEngineHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<SimulationEngineHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        var intervalMs = configuration.GetValue<int?>("Simulation:TickIntervalMilliseconds") ?? 500;
        _tickInterval = TimeSpan.FromMilliseconds(intervalMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновый движок симуляции лифта запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var runtimeService = scope.ServiceProvider
                    .GetRequiredService<SimulationRuntimeService>();

                await runtimeService.ProcessRunningSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Ошибка фонового движка симуляции.");
            }

            await Task.Delay(_tickInterval, stoppingToken);
        }

        _logger.LogInformation("Фоновый движок симуляции лифта остановлен.");
    }
}
