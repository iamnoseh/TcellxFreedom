using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Infrastructure.Configuration;
using TcellxFreedom.Infrastructure.Services;

namespace TcellxFreedom.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOsonSms(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OsonSmsSettings>(configuration.GetSection(OsonSmsSettings.SectionName));
        services.AddScoped<IOsonSmsService, OsonSmsService>();

        return services;
    }
}
