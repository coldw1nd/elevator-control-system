using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ElevatorControlSystem.Application;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Infrastructure;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<SimulationSession> SimulationSessions => Set<SimulationSession>();

    public DbSet<ElevatorState> ElevatorStates => Set<ElevatorState>();

    public DbSet<Passenger> Passengers => Set<Passenger>();

    public DbSet<FloorCall> FloorCalls => Set<FloorCall>();

    public DbSet<CabinRequest> CabinRequests => Set<CabinRequest>();

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<SessionReport> SessionReports => Set<SessionReport>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureApplicationUsers(modelBuilder.Entity<ApplicationUser>());
        ConfigureSimulationSessions(modelBuilder.Entity<SimulationSession>());
        ConfigureElevatorStates(modelBuilder.Entity<ElevatorState>());
        ConfigurePassengers(modelBuilder.Entity<Passenger>());
        ConfigureFloorCalls(modelBuilder.Entity<FloorCall>());
        ConfigureCabinRequests(modelBuilder.Entity<CabinRequest>());
        ConfigureTrips(modelBuilder.Entity<Trip>());
        ConfigureSessionReports(modelBuilder.Entity<SessionReport>());
        ConfigureAuditLogs(modelBuilder.Entity<AuditLog>());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var generatedIdentifiers = false;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
                generatedIdentifiers = true;
            }
        }

        if (generatedIdentifiers)
        {
            ChangeTracker.DetectChanges();
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = nowUtc;
                entry.Entity.UpdatedAtUtc = nowUtc;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = nowUtc;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureApplicationUsers(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");

        builder.HasIndex(x => x.Username).IsUnique();

        builder.Property(x => x.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }

    private static void ConfigureSimulationSessions(EntityTypeBuilder<SimulationSession> builder)
    {
        builder.ToTable(
            "SimulationSessions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_SimulationSessions_FloorCount",
                    "\"FloorCount\" >= 2 AND \"FloorCount\" <= 50");
            });

        builder.HasIndex(x => x.Status);

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedSessions)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ElevatorState)
            .WithOne(x => x.SimulationSession)
            .HasForeignKey<ElevatorState>(x => x.SimulationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Report)
            .WithOne(x => x.SimulationSession)
            .HasForeignKey<SessionReport>(x => x.SimulationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Passengers)
            .WithOne(x => x.SimulationSession)
            .HasForeignKey(x => x.SimulationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FloorCalls)
            .WithOne(x => x.SimulationSession)
            .HasForeignKey(x => x.SimulationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CabinRequests)
            .WithOne(x => x.SimulationSession)
            .HasForeignKey(x => x.SimulationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Trips)
            .WithOne(x => x.SimulationSession)
            .HasForeignKey(x => x.SimulationSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureElevatorStates(EntityTypeBuilder<ElevatorState> builder)
    {
        builder.ToTable("ElevatorStates");

        builder.HasIndex(x => x.SimulationSessionId).IsUnique();

        builder.Property(x => x.MovementState)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.GoCommandPending)
            .HasDefaultValue(false)
            .IsRequired();
    }

    private static void ConfigurePassengers(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable(
            "Passengers",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Passengers_WeightKg",
                    "\"WeightKg\" > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Passengers_DifferentFloors",
                    "\"SourceFloor\" <> \"TargetFloor\"");
            });

        builder.HasIndex(x => x.SimulationSessionId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }

    private static void ConfigureFloorCalls(EntityTypeBuilder<FloorCall> builder)
    {
        builder.ToTable(
            "FloorCalls",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_FloorCalls_FloorNumber",
                    "\"FloorNumber\" > 0");
            });

        builder.HasIndex(x => new { x.SimulationSessionId, x.FloorNumber }).IsUnique();
    }

    private static void ConfigureCabinRequests(EntityTypeBuilder<CabinRequest> builder)
    {
        builder.ToTable(
            "CabinRequests",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CabinRequests_FloorNumber",
                    "\"FloorNumber\" > 0");
            });

        builder.HasIndex(x => new { x.SimulationSessionId, x.FloorNumber }).IsUnique();
    }

    private static void ConfigureTrips(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable(
            "Trips",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Trips_SequenceNumber",
                    "\"SequenceNumber\" > 0");
            });

        builder.HasIndex(x => new { x.SimulationSessionId, x.SequenceNumber }).IsUnique();

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();
    }

    private static void ConfigureSessionReports(EntityTypeBuilder<SessionReport> builder)
    {
        builder.ToTable("SessionReports");

        builder.HasIndex(x => x.SimulationSessionId).IsUnique();
    }

    private static void ConfigureAuditLogs(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.Username);

        builder.Property(x => x.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(100);
    }
}

public sealed class DatabaseInitializer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;

    public DatabaseInitializer(
        ApplicationDbContext dbContext,
        IPasswordHasherService passwordHasherService)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (_dbContext.Database.IsNpgsql())
        {
            await CreateDatabaseTriggersAsync(cancellationToken);
        }

        await SeedDefaultUsersAsync(cancellationToken);
    }

    private async Task SeedDefaultUsersAsync(CancellationToken cancellationToken)
    {
        var anyUsersExist = await _dbContext.ApplicationUsers.AnyAsync(cancellationToken);

        if (anyUsersExist)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Username = "admin",
            DisplayName = "System Administrator",
            Role = UserRole.Admin,
            IsActive = true
        };

        admin.PasswordHash = _passwordHasherService.HashPassword(admin, "Admin123!");

        var operatorUser = new ApplicationUser
        {
            Username = "operator",
            DisplayName = "Main Operator",
            Role = UserRole.Operator,
            IsActive = true
        };

        operatorUser.PasswordHash = _passwordHasherService.HashPassword(operatorUser, "Operator123!");

        var viewer = new ApplicationUser
        {
            Username = "viewer",
            DisplayName = "Read Only Viewer",
            Role = UserRole.Viewer,
            IsActive = true
        };

        viewer.PasswordHash = _passwordHasherService.HashPassword(viewer, "Viewer123!");

        _dbContext.ApplicationUsers.AddRange(admin, operatorUser, viewer);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateDatabaseTriggersAsync(CancellationToken cancellationToken)
    {
        const string sql = """
ALTER TABLE "ElevatorStates"
    ADD COLUMN IF NOT EXISTS "GoCommandPending" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "ElevatorStates"
    ADD COLUMN IF NOT EXISTS "LastGoPressedAtUtc" timestamp with time zone NULL;

ALTER TABLE "ElevatorStates"
    ADD COLUMN IF NOT EXISTS "LastServicedFloor" integer NULL;

ALTER TABLE "ElevatorStates"
    ADD COLUMN IF NOT EXISTS "LastServicedAtUtc" timestamp with time zone NULL;

CREATE OR REPLACE FUNCTION set_updated_at_utc()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAtUtc" = TIMEZONE('utc', NOW());
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_application_users_updated_at ON "ApplicationUsers";
CREATE TRIGGER trg_application_users_updated_at
BEFORE UPDATE ON "ApplicationUsers"
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_utc();

DROP TRIGGER IF EXISTS trg_simulation_sessions_updated_at ON "SimulationSessions";
CREATE TRIGGER trg_simulation_sessions_updated_at
BEFORE UPDATE ON "SimulationSessions"
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_utc();

DROP TRIGGER IF EXISTS trg_elevator_states_updated_at ON "ElevatorStates";
CREATE TRIGGER trg_elevator_states_updated_at
BEFORE UPDATE ON "ElevatorStates"
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_utc();
""";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
