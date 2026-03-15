using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Domain.Interfaces;
using TcellxFreedom.Infrastructure.Data;
using TcellxFreedom.Infrastructure.Extensions;
using TcellxFreedom.Infrastructure.Identity;
using TcellxFreedom.Infrastructure.Repositories;
using TcellxFreedom.Infrastructure.Services;

namespace TcellxFreedom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 1;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddOsonSms(configuration);

        return services;
    }
}
