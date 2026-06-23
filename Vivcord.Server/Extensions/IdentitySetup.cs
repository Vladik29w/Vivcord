using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Vivcord.Server.DbContext;
using Vivcord.Server.Models;

namespace Vivcord.Server.Extensions
{
    public static class IdentitySetup
    {
        public static IdentityBuilder AddVivcordIdentity(this IServiceCollection services)
        {
            services.AddDataProtection();

            return services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MainDbContext>()
            .AddDefaultTokenProviders();
        }
    }
}
