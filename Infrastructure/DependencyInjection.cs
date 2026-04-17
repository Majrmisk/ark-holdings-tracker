using Application.Interfaces;
using Domain;
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

        return services;
    }
}
