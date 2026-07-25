using TruthGate_Web.Middleware;
using TruthGate_Web.Services;
using Microsoft.EntityFrameworkCore;

namespace TruthGate_Web.Security
{
    public static class RateLimiterRegistration
    {
        public static IServiceCollection AddTruthGateRateLimiter(
            this IServiceCollection services,
            string? connectionString = null)
        {
            services.AddDbContextFactory<RateLimiterDbContext>((sp, b) =>
            {
                var cfg = sp.GetRequiredService<IConfigService>();
                var configuredDatabasePath = Environment.GetEnvironmentVariable("TRUTHGATE_DATABASE_PATH");

                string dbPath;
                if (string.IsNullOrWhiteSpace(configuredDatabasePath))
                {
                    // Preserve the historical behavior for non-container installations.
                    var configDirectory = Path.GetDirectoryName(cfg.ConfigPath) ?? AppContext.BaseDirectory;
                    Directory.CreateDirectory(configDirectory);
                    dbPath = Path.Combine(configDirectory, "ratelimiter.db");
                }
                else
                {
                    var resolved = Path.GetFullPath(configuredDatabasePath);
                    if (string.Equals(Path.GetExtension(resolved), ".db", StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
                        dbPath = resolved;
                    }
                    else
                    {
                        Directory.CreateDirectory(resolved);
                        dbPath = Path.Combine(resolved, "ratelimiter.db");
                    }
                }

                var cs = connectionString ?? $"Data Source={dbPath};Cache=Shared";

                b.UseSqlite(cs);
                b.EnableSensitiveDataLogging(false);
            });

            services.AddSingleton<IRateLimiterService, RateLimiterService>();
            services.AddHostedService<RateLimiterFlushWorker>();
            services.AddHostedService<RateLimiterPurgeWorker>();

            // Don't re-Configure<RateLimiterOptions> here; the app already did it.
            return services;
        }

        public static IApplicationBuilder UseTruthGateRateLimiter(this IApplicationBuilder app)
            => app.UseMiddleware<RateLimiterMiddleware>();
    }
}
