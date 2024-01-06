using System;
using System.Globalization;
using System.IO;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;
using SeoBoost.Models;

namespace SeoBoost.Business.Url
{
    public class UrlService : IUrlService
    {
        private readonly SeoBoostOptions _configuration;
        private readonly IContentRepository _contentRepository;
        private readonly UrlBuilder _builder;
        private readonly RoutingOptions _routingOptions;

        public UrlService(UrlBuilder builder, IOptions<SeoBoostOptions> options, IContentRepository contentRepository, RoutingOptions routingOptions)
        {
            _builder = builder;
            _contentRepository = contentRepository;
            _routingOptions = routingOptions;
            _configuration = options.Value;
        }

        public string GetExternalUrl(ContentReference contentReference, CultureInfo culture)
        {
            return ApplyTrailingSlash(_builder.ContentExternalUrl(contentReference, culture));
        }

        public string GetExternalUrl(ContentReference contentReference)
        {
            return ApplyTrailingSlash(_builder.ContentExternalUrl(contentReference, ContentLanguage.PreferredCulture));
        }

        public string GetCanonicalLink(ContentReference contentReference)
        {
            return GetCanonicalLink(contentReference, ContentLanguage.PreferredCulture);
        }

        public string GetCanonicalLink(ContentReference contentReference, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(_configuration.CustomCanonicalTagFieldName))
            {
                var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(culture) };
                var content = _contentRepository.Get<ContentData>(contentReference, loadingOptions);

                if (content != null)
                {
                    var customCanonicalTagFieldName = content.GetPropertyValue(_configuration.CustomCanonicalTagFieldName);
                    if (!string.IsNullOrEmpty(customCanonicalTagFieldName))
                    {
                        var validUrl = Uri.TryCreate(customCanonicalTagFieldName, UriKind.Absolute, out var uriResult)
                                       && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                        if (validUrl)
                            return ApplyTrailingSlash(customCanonicalTagFieldName);
                    }
                }
            }
            return GetExternalUrl(contentReference, culture);
        }

        private string ApplyTrailingSlash(string url)
        {
            var useTrailingSlash = _routingOptions.UseTrailingSlash;
            if (!string.IsNullOrEmpty(url))
            {
                if (_routingOptions.UseTrailingSlash)
                {
                    if (!url.EndsWith("/"))
                    {
                        url += "/";
                    }
                }
                else if (url.EndsWith("/"))
                {
                    url = url.TrimEnd('/');
                }

                return url.ToLower();
            }

            return useTrailingSlash ? "/" : string.Empty;
        }
    }
}