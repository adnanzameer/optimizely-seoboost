using System;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SeoBoost.Models;

namespace SeoBoost.Business.Url
{
    public class UrlBuilder
    {
        private readonly UrlResolver _urlResolver;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly SeoBoostOptions _configuration;
        private readonly IContentRepository _contentRepository;
        public UrlBuilder(IOptions<SeoBoostOptions> option, UrlResolver urlResolver, ISiteDefinitionResolver siteDefinitionResolver, IContentRepository contentRepository)
        {
            _urlResolver = urlResolver;
            _siteDefinitionResolver = siteDefinitionResolver;
            _contentRepository = contentRepository;
            _configuration = option.Value;
        }

        public string ContentExternalUrl(ContentReference contentLink, CultureInfo contentLanguage)
        {
            string result = string.Empty;

            if (_configuration.UseSimpleAddress)
            {
                _contentRepository.TryGet<PageData>(contentLink, out var pageData);
                if (pageData != null && !string.IsNullOrEmpty(pageData.ExternalURL))
                {
                    result = pageData.ExternalURL;
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                result = _urlResolver.GetUrl(
                    contentLink,
                    contentLanguage.Name,
                    new VirtualPathArguments
                    {
                        ContextMode = ContextMode.Default,
                        ForceCanonical = true
                    });
            }

            if (!Uri.TryCreate(result, UriKind.RelativeOrAbsolute, out var relativeUri)) return result;

            if (relativeUri.IsAbsoluteUri) return result;

            Uri baseUri = null;
            if (!_configuration.UseSiteUrlAsDefault)
            {
                var siteDefinition = _siteDefinitionResolver.GetByContent(contentLink, true, true);
                var hosts = siteDefinition.GetHosts(contentLanguage, true).ToList();

                var host = hosts.FirstOrDefault(h => h.Type == HostDefinitionType.Primary && h.Language.Equals(contentLanguage));

                baseUri = siteDefinition.SiteUrl;

                if (host != null && host.Name.Equals("*") == false)
                {
                    Uri.TryCreate(siteDefinition.SiteUrl.Scheme + "://" + host.Name, UriKind.Absolute, out baseUri);
                }
            }
            if (baseUri == null)
            {
                Uri.TryCreate(SiteDefinition.Current.SiteUrl.AbsoluteUri, UriKind.Absolute, out baseUri);
            }

            if (baseUri != null)
            {
                var absoluteUri = new Uri(baseUri, relativeUri);

                return absoluteUri.AbsoluteUri;
            }

            return string.Empty;
        }
    }
}