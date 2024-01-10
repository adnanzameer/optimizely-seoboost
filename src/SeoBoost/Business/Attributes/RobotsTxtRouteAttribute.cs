using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using SeoBoost.Models;
using SeoBoost.Models.Pages;

public class RobotsTxtRouteAttribute : Attribute, IRouteTemplateProvider
{
    private int? _order;

    public RobotsTxtRouteAttribute()
    {
        Template = "testrobots.txt";

        var options = ServiceLocator.Current.GetInstance<IOptions<SeoBoostOptions>>();
        if (options.Value.EnableRobotsTxtSupport)
        {
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            var items = contentLoader.GetChildren<SBRobotsTxt>(ContentReference.StartPage,
                new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) });

            if (items != null && items.Any())
            {
                Template = "robots.txt";
            }
        }
    }

    public string Template { get; }

    public int Order
    {
        get => _order ?? 0;
        set => _order = value;
    }

    int? IRouteTemplateProvider.Order => _order;

    public string? Name { get; set; }
}