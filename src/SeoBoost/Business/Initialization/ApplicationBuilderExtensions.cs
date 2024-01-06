using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SeoBoost.Business.Events;

namespace SeoBoost.Business.Initialization
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSeoBoost(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var initializer = services.GetRequiredService<SeoBoostInitializer>();
            initializer.Initialize();

            return app;
        }
    }
}
