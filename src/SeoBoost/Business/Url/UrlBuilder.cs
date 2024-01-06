using System;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Routing;
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
            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) };
            var pageData = _contentRepository.Get<PageData>(contentLink, loadingOptions);

            var actualPage = IsMirrored(pageData) ?
            GetMirroredTarget(pageData, contentLanguage) ?? pageData :
            pageData;


            var result = string.Empty;
            if (_configuration.UseSimpleAddress)
            {
                if (actualPage != null && !string.IsNullOrEmpty(actualPage.ExternalURL))
                {
                    result = pageData.ExternalURL;
                }
            }
            if (actualPage != null)
            {
                if (string.IsNullOrEmpty(result))
                {
                    result = _urlResolver.GetUrl(
                        actualPage.ContentLink,
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
                    var siteDefinition = _siteDefinitionResolver.GetByContent(actualPage.ContentLink, true, true);
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
            }

            return string.Empty;
        }

        private PageData GetMirroredTarget(PageData currentPage, CultureInfo contentLanguage)
        {
            var target = (PropertyPageReference)currentPage.Property["PageShortcutLink"];
            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) };
            var targetPage = _contentRepository.Get<PageData>(target.PageLink, loadingOptions);

            return targetPage ?? currentPage;
        }

        private static bool IsMirrored(PageData page)
        {
            return page.LinkType == PageShortcutType.FetchData || page.LinkType == PageShortcutType.Shortcut;
        }
    }
}