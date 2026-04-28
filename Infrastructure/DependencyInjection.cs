using Application.Clients;
using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Domain;
using Infrastructure.CronJobs;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found."
            );

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.Schema.For<HoldingSnapshot>();
            options.Schema.For<HoldingDiff>();
        }).UseLightweightSessions();

        services.AddScoped<IHoldingsRepository, HoldingsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHoldingService, HoldingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddHttpClient<IHoldingClient, HoldingClient>();
        return services;
    }

    public static IServiceCollection AddCronJobs(this IServiceCollection services)
    {
        services.AddHostedService<FetchJob>();
        services.AddHostedService<DeleteJob>();
        return services;
    }

    public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ArkFundClientSettings>(configuration.GetSection("ArkFundClient"));
        services.Configure<DataRetentionSettings>(configuration.GetSection("DataRetention"));
        return services;
    }
}
