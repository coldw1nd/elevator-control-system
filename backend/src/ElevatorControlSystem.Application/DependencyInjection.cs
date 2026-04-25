using Microsoft.Extensions.DependencyInjection;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDestinationSelectionStrategy, NearestCollectiveDestinationStrategy>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISessionSnapshotBuilder, SessionSnapshotBuilder>();

        services.AddScoped<AuthService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<SessionService>();
        services.AddScoped<PassengerService>();
        services.AddScoped<ReportService>();
        services.AddScoped<AuditQueryService>();

        services.AddScoped<SimulationOperations>();
        services.AddScoped<SimulationRuntimeService>();

        services.AddScoped<IElevatorStateHandler, IdleClosedStateHandler>();
        services.AddScoped<IElevatorStateHandler, IdleOpenStateHandler>();
        services.AddScoped<IElevatorStateHandler, MovingUpStateHandler>();
        services.AddScoped<IElevatorStateHandler, MovingDownStateHandler>();
        services.AddScoped<ElevatorStateHandlerFactory>();

        return services;
    }
}
