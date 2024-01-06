using System;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using Microsoft.Extensions.Options;
using SeoBoost.Models;

namespace SeoBoost.Business.Url
{
    public class UrlService : IUrlService
    {
        private readonly SeoBoostOptions _configuration;
        private readonly IContentRepository _contentRepository;

        private readonly UrlBuilder _builder;

        public UrlService(UrlBuilder builder, IOptions<SeoBoostOptions> options, IContentRepository contentRepository)
        {
            _builder = builder;
            _contentRepository = contentRepository;
            _configuration = options.Value;
        }

        public string GetExternalUrl(ContentReference contentReference, CultureInfo culture)
        {
            return TransformUrl(_builder.ContentExternalUrl(contentReference, culture));
        }

        public string GetExternalUrl(ContentReference contentReference)
        {
            return TransformUrl(_builder.ContentExternalUrl(contentReference, ContentLanguage.PreferredCulture));
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
                            return TransformUrl(customCanonicalTagFieldName);
                    }
                }
            }
            return GetExternalUrl(contentReference, culture);
        }

        private string TransformUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (_configuration.UseTrailingSlash)
                {
                    if (!url.EndsWith("/"))
                    {
                        url += "/";
                    }
                }
                else
                {
                    if (url.EndsWith("/"))
                    {
                        url = url.TrimEnd('/');
                    }
                }

                return url.ToLower();
            }

            return _configuration.UseTrailingSlash ? "/" : string.Empty;
        }
    }
}