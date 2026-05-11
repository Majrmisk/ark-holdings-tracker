using Application.Clients;
using Application.Interfaces;
using Application.Services;
using Application.Settings;
using Domain;
using Infrastructure.CronJobs;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddScoped<IAuthService, AuthService>();

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
        services.Configure<AdminUserSettings>(configuration.GetSection("AdminUser"));
        return services;
    }

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
        });

        return services;
    }

    public static async Task UseInfrastructureAsync(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            await DataSeeder.SeedAsync(scope.ServiceProvider);
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
    }
}
