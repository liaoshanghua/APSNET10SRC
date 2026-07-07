using EasyManufacture.Domain.Data;
using EasyManufacture.Domain.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EasyManufacture.Licence;

public static class LicenceServiceCollectionExtensions
{
    public static IServiceCollection AddEasyManufactureLicence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<JDRegister>();
        return services;
    }

    public static IApplicationBuilder UseEasyManufactureLicence(this IApplicationBuilder app)
    {
        var scope = app.ApplicationServices.CreateScope();
        var sp = scope.ServiceProvider;
        LicenceRuntime.Configure(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            sp.GetRequiredService<IHostEnvironment>(),
            SqlConnectionStringHelper.Normalize(
                sp.GetRequiredService<IOptions<DatabaseSettings>>().Value.MSSQLConnectionString));
        scope.Dispose();
        return app;
    }
}
