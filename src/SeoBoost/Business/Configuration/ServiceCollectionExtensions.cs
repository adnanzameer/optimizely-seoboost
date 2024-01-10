using System;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeoBoost.Business.Initialization;
using SeoBoost.Business.Url;
using SeoBoost.Helper.AlternateLinks;
using SeoBoost.Models;

namespace SeoBoost.Business.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSeoBoost(this IServiceCollection services)
        {
            return AddSeoBoost(services, _ => { });
        }

        public static IServiceCollection AddSeoBoost(this IServiceCollection services, Action<SeoBoostOptions> setupAction)
        {
            services.AddHttpContextOrThreadScoped<IUrlService, UrlService>();
            services.AddTransient<IViewTemplateModelRegistrator, TemplateCoordinator>();
            services.AddTransient<IAlternateLinksHelper, AlternateLinksHelper>();
            services.AddSingleton<SeoBoostInitializer>();
            services.AddSingleton<UrlBuilder>();

            
            var providerOptions = new SeoBoostOptions();
            setupAction(providerOptions);

            services.AddOptions<SeoBoostOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("SeoBoost").Bind(options);
            });

            return services;
        }

    }
}