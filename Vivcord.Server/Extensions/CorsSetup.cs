using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vivcord.Server.Extensions
{
    public static class CorsSetup
    {
        public static IServiceCollection AddVivcordCors(this IServiceCollection services, IConfiguration configuration)
        {
            var corsOrigins = configuration["CorsOrigins"];
            return services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularOrigin",
                    policy =>
                    {
                        var origins = new List<string> { "https://localhost:62667", "https://127.0.0.1:62667", "http://localhost:4200" };
                        if (!string.IsNullOrEmpty(corsOrigins))
                        {
                            origins.AddRange(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()));
                        }
                        policy.WithOrigins(origins.ToArray())
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });
        }
    }
}
