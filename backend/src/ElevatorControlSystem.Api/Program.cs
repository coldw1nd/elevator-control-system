using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ElevatorControlSystem.Api;
using ElevatorControlSystem.Application;
using ElevatorControlSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            "logs/application-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<ISimulationNotifier, SignalRSimulationNotifier>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(
                x => $"{x.Key}: {string.Join("; ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
            .ToList();

        var details = new ProblemDetails
        {
            Title = "Некорректные входные данные",
            Detail = string.Join(" | ", errors),
            Status = StatusCodes.Status400BadRequest
        };

        return new BadRequestObjectResult(details);
    };
});

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT options are not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken)
                    && path.StartsWithSegments("/hubs/simulation"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdRaw = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var usernameRaw = context.Principal?.FindFirstValue(ClaimTypes.Name);
                var roleRaw = context.Principal?.FindFirstValue(ClaimTypes.Role);

                if (!Guid.TryParse(userIdRaw, out var userId))
                {
                    context.Fail("Некорректный идентификатор пользователя в токене.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

                var user = await dbContext.ApplicationUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == userId,
                        context.HttpContext.RequestAborted);

                if (user is null)
                {
                    context.Fail("Пользователь токена не найден.");
                    return;
                }

                if (!user.IsActive)
                {
                    context.Fail("Учетная запись пользователя деактивирована.");
                    return;
                }

                // Старый JWT не должен жить вечно "сам по себе".
                // Если администратор деактивировал пользователя или поменял ему роль,
                // токен должен перестать считаться актуальным уже на следующем запросе,
                // даже если его срок формально еще не истек.
                var roleMatches = string.Equals(
                    user.Role.ToString(),
                    roleRaw,
                    StringComparison.Ordinal);

                var usernameMatches = string.Equals(
                    user.Username,
                    usernameRaw,
                    StringComparison.Ordinal);

                if (!roleMatches || !usernameMatches)
                {
                    context.Fail("Данные учетной записи были изменены. Требуется повторный вход.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "FrontendPolicy",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Elevator Control System API",
            Version = "v1"
        });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Введите JWT токен в формате: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            { jwtSecurityScheme, Array.Empty<string>() }
        });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

var hasStaticFrontend = !string.IsNullOrWhiteSpace(app.Environment.WebRootPath)
    && File.Exists(Path.Combine(app.Environment.WebRootPath!, "index.html"));

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (hasStaticFrontend)
{
    // Если собранный MPA-фронтенд положен в wwwroot,
    // backend может отдавать его сам.
    // Это не SPA-fallback, а обычная раздача статических HTML-страниц.
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SimulationHub>("/hubs/simulation");

if (!hasStaticFrontend)
{
    app.MapGet(
        "/",
        () => Results.Redirect("/swagger"));
}

app.Run();

public partial class Program
{
}
