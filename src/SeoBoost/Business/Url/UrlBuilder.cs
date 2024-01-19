using System;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SeoBoost.Models;

namespace SeoBoost.Business.Url
{
    public class UrlBuilder
    {
        private readonly IUrlResolver _urlResolver;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly SeoBoostOptions _configuration;
        private readonly IContentRepository _contentRepository;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly RoutingOptions _routingOptions;
        public UrlBuilder(IOptions<SeoBoostOptions> option, IUrlResolver urlResolver, ISiteDefinitionResolver siteDefinitionResolver, IContentRepository contentRepository, IHttpContextAccessor contextAccessor, RoutingOptions routingOptions)
        {
            _urlResolver = urlResolver;
            _siteDefinitionResolver = siteDefinitionResolver;
            _contentRepository = contentRepository;
            _contextAccessor = contextAccessor;
            _routingOptions = routingOptions;
            _configuration = option.Value;
        }

        public string ContentExternalUrl(ContentReference contentLink, CultureInfo contentLanguage)
        {
            //Partial Router check
            var result = PartialRouterCheck(contentLanguage);

            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) };

            // Custom Canonical Field Check
            if (string.IsNullOrEmpty(result))
            {
                var customCanonicalUrl = GetCustomCanonicalUrl(contentLink, contentLanguage);
                if (!string.IsNullOrEmpty(customCanonicalUrl))
                    return customCanonicalUrl;
            }

            var pageData = _contentRepository.Get<PageData>(contentLink, loadingOptions);

            // Mirror/ Shortcut Check
            if (string.IsNullOrEmpty(result) && _configuration.UseMirrorPageReference)
            {
                pageData = GetMirroredTarget(pageData, contentLanguage);
            }

            if (string.IsNullOrEmpty(result) && _configuration.UseSimpleAddressAsPath && !string.IsNullOrEmpty(pageData.ExternalURL))
            {
                result = pageData.ExternalURL;
            }

            if (string.IsNullOrEmpty(result))
            {
                var pageUrl = _urlResolver.GetUrl(
                pageData.ContentLink,
                contentLanguage.Name,
                new VirtualPathArguments
                {
                    ContextMode = ContextMode.Default,
                    ForceCanonical = true
                });

                result = pageUrl;
            }

            if (!Uri.TryCreate(result, UriKind.RelativeOrAbsolute, out var relativeUri))
                return ApplyTrailingSlash(result);

            if (relativeUri.IsAbsoluteUri)
                return ApplyTrailingSlash(result);

            var hostLanguage = string.Empty;

            var siteDefinition = _siteDefinitionResolver.GetByContent(pageData.ContentLink, true, true);

            var hosts = siteDefinition.GetHosts(contentLanguage, true).ToList();

            var host = hosts.FirstOrDefault(h => h.Language != null && h.Language.Equals(contentLanguage));

            if (!_configuration.UseSiteUrlAsHost)
            {
                host ??= hosts.FirstOrDefault(h => h.Type == HostDefinitionType.Primary);
            }

            var baseUri = siteDefinition.SiteUrl;

            if (host != null && host.Name.Equals("*") == false)
            {
                Uri.TryCreate(siteDefinition.SiteUrl.Scheme + "://" + host.Name, UriKind.Absolute, out baseUri);

                if (host.Language != null)
                {
                    hostLanguage = "/" + host.Language.Name.ToLower() + "/";
                }
            }

            if (baseUri == null)
            {
                Uri.TryCreate(SiteDefinition.Current.SiteUrl.AbsoluteUri, UriKind.Absolute, out baseUri);
            }

            if (baseUri != null)
            {
                var absoluteUri = new Uri(baseUri, relativeUri);
                if (!string.IsNullOrEmpty(hostLanguage))
                {
                    var absoluteUrl = absoluteUri.AbsoluteUri.Replace(hostLanguage, "/");
                    return ApplyTrailingSlash(absoluteUrl);
                }
                return ApplyTrailingSlash(absoluteUri.AbsoluteUri);
            }
            return string.Empty;
        }

        private string GetCustomCanonicalUrl(ContentReference contentLink, CultureInfo contentLanguage)
        {
            if (string.IsNullOrEmpty(_configuration.CustomCanonicalTagFieldName))
                return null;

            var content = _contentRepository.Get<ContentData>(contentLink, new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) });

            if (content == null)
                return null;

            var propertyValue = content.GetPropertyValue(_configuration.CustomCanonicalTagFieldName);

            if (string.IsNullOrEmpty(propertyValue))
                return null;

            if (!Uri.TryCreate(propertyValue, UriKind.RelativeOrAbsolute, out var customRelativeUri))
                return null;

            if (customRelativeUri.IsAbsoluteUri)
                return ApplyTrailingSlash(propertyValue);

            var siteUri = new Uri(SiteDefinition.Current.SiteUrl.AbsoluteUri);
            var absoluteUri = new Uri(siteUri, customRelativeUri);

            return ApplyTrailingSlash(absoluteUri.AbsoluteUri);
        }

        private PageData GetMirroredTarget(PageData currentPage, CultureInfo contentLanguage)
        {
            if (IsMirrored(currentPage))
            {
                var target = (PropertyPageReference)currentPage.Property["PageShortcutLink"];
                var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) };
                var targetPage = _contentRepository.Get<PageData>(target.PageLink, loadingOptions);
                return targetPage ?? currentPage;
            }

            return currentPage;
        }

        private static bool IsMirrored(PageData page)
        {
            return page.LinkType == PageShortcutType.FetchData || page.LinkType == PageShortcutType.Shortcut;
        }


        private string ApplyTrailingSlash(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return _routingOptions.UseTrailingSlash ? "/" : string.Empty;
            }

            if (_routingOptions.UseTrailingSlash && !url.EndsWith("/"))
            {
                url += "/";
            }
            else if (!_routingOptions.UseTrailingSlash && url.EndsWith("/"))
            {
                url = url.TrimEnd('/');
            }

            return url.ToLower();
        }


        private string PartialRouterCheck(CultureInfo contentLanguage)
        {
            if (_contextAccessor.HttpContext == null)
            {
                return string.Empty;
            }

            var partialRoutedObject = _contextAccessor.HttpContext.Features.Get<IContentRouteFeature>()
                ?.RoutedContentData
                .PartialRoutedObject;
            
            if (partialRoutedObject == null)
            {
                return string.Empty;
            }

            var currentUrl = _contextAccessor.HttpContext.Request.Path;

            if (string.IsNullOrEmpty(currentUrl.Value) || !currentUrl.HasValue)
            {
                return string.Empty;
            }

            var urlSegments = currentUrl.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (urlSegments.Length <= 1)
            {
                return string.Empty;
            }

            var currentCulture = ContentLanguage.PreferredCulture;
            if (!currentCulture.Name.Equals(contentLanguage.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var url = currentUrl.Value.ToLower().Replace("/" + currentCulture.Name.ToLower() + "/", "/" + contentLanguage.Name.ToLower() + "/");
                return url;
            }

            return currentUrl.Value.ToLower();
        }
    }
}