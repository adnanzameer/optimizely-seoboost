using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SeoBoost.Business.Attributes;
using SeoBoost.Helper;
using SeoBoost.Models;
#pragma warning disable CS0618 // ISiteDefinitionResolver is obsolete in CMS 13; IApplicationResolver returns a different type requiring a larger migration

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
            if (!string.IsNullOrEmpty(_configuration.CustomCanonicalTagFieldName))
            {
                var dynamicCanonicalLink = HttpContextUtility.GetItem(_contextAccessor.HttpContext, _configuration.CustomCanonicalTagFieldName);

                if (!string.IsNullOrEmpty(dynamicCanonicalLink))
                {
                    var customCanonicalUrl = GetCustomCanonicalUrl(dynamicCanonicalLink);
                    if (!string.IsNullOrEmpty(customCanonicalUrl))
                        return customCanonicalUrl;
                }
            }

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
                var simpleAddress = pageData.ExternalURL.StartsWith("/")
                    ? pageData.ExternalURL
                    : "/" + pageData.ExternalURL;

                var langPrefix = "/" + contentLanguage.Name.ToLower() + "/";
                if (!simpleAddress.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Resolve the standard URL to detect whether a path-based language prefix
                    // is expected. A relative result means a wildcard host (language is in the
                    // path); an absolute result means a language-specific domain (language is
                    // in the host, handled later by the hostLanguage block).
                    var resolvedUrl = _urlResolver.GetUrl(pageData.ContentLink, contentLanguage.Name,
                        new VirtualPathArguments { ContextMode = ContextMode.Default, ForceCanonical = true });

                    if (!string.IsNullOrEmpty(resolvedUrl)
                        && !Uri.TryCreate(resolvedUrl, UriKind.Absolute, out _)
                        && resolvedUrl.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        simpleAddress = langPrefix.TrimEnd('/') + simpleAddress;
                    }
                }

                result = simpleAddress;
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

            var languageSegmentStripped = false;
            if (_configuration.UseStartPageCanonicalWithoutLanguageSegment
                && !string.IsNullOrEmpty(result)
                && contentLink.ID == ContentReference.StartPage.ID
                && pageData.IsMasterLanguageBranch
                && pageData.Language.Name.Equals(contentLanguage.Name, StringComparison.OrdinalIgnoreCase))
            {
                var langPrefix = "/" + contentLanguage.Name.ToLower() + "/";
                if (result.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    result = "/" + result.Substring(langPrefix.Length);
                    languageSegmentStripped = true;
                }
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
                Uri.TryCreate(siteDefinition.SiteUrl.AbsoluteUri, UriKind.Absolute, out baseUri);
            }

            // For same-domain language hosts the URL resolver omits the language segment from
            // the path (it considers the host itself the language identifier). Re-add the
            // segment so the canonical always carries the language prefix — unless we
            // deliberately stripped it above (UseStartPageCanonicalWithoutLanguageSegment).
            if (!languageSegmentStripped
                && !string.IsNullOrEmpty(hostLanguage)
                && baseUri != null
                && baseUri.Host.Equals(siteDefinition.SiteUrl.Host, StringComparison.OrdinalIgnoreCase))
            {
                var normalizedPath = result.StartsWith("/") ? result : "/" + result;
                if (!normalizedPath.StartsWith(hostLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    result = hostLanguage.TrimEnd('/') + normalizedPath;
                    Uri.TryCreate(result, UriKind.Relative, out relativeUri);
                }
            }

            if (baseUri != null)
            {
                var absoluteUri = new Uri(baseUri, relativeUri);
                if (!string.IsNullOrEmpty(hostLanguage)
                    && !baseUri.Host.Equals(siteDefinition.SiteUrl.Host, StringComparison.OrdinalIgnoreCase))
                {
                    // Language-specific domain (e.g. en.example.com): strip language from path
                    // because the domain already encodes the language.
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

            var siteDefinition = _siteDefinitionResolver.GetByContent(contentLink, true, true);
            var siteUri = new Uri((siteDefinition?.SiteUrl ?? SiteDefinition.Empty.SiteUrl).AbsoluteUri);
            var absoluteUri = new Uri(siteUri, customRelativeUri);

            return ApplyTrailingSlash(absoluteUri.AbsoluteUri);
        }

        private string GetCustomCanonicalUrl(string dynamicCanonicalUrl)
        {
            if (string.IsNullOrEmpty(_configuration.CustomCanonicalTagFieldName))
                return null;

            if (string.IsNullOrEmpty(dynamicCanonicalUrl))
                return null;

            if (!Uri.TryCreate(dynamicCanonicalUrl, UriKind.RelativeOrAbsolute, out var customRelativeUri))
                return null;

            if (customRelativeUri.IsAbsoluteUri)
                return ApplyTrailingSlash(dynamicCanonicalUrl);

            var request = _contextAccessor.HttpContext?.Request;
            if (request == null)
                return null;

            var siteUri = new Uri($"{request.Scheme}://{request.Host}");
            var absoluteUri = new Uri(siteUri, customRelativeUri);

            return ApplyTrailingSlash(absoluteUri.AbsoluteUri);
        }


        private PageData GetMirroredTarget(PageData currentPage, CultureInfo contentLanguage)
        {
            if (IsMirrored(currentPage))
            {
                var target = (PropertyContentReference)currentPage.Property["PageShortcutLink"];
                var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) };
                var targetPage = _contentRepository.Get<PageData>(target.ContentLink, loadingOptions);
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

            return _configuration.PreserveUrlCasing ? url : url.ToLower();
        }
        

        private string PartialRouterCheck(CultureInfo contentLanguage)
        {
            if (_contextAccessor.HttpContext?.Features.Get<IContentRouteFeature>()?.RoutedContentData?.PartialRoutedObject is not PageData partialRoutedObject)
            {
                return string.Empty;
            }

            var hasAttribute = HasPartialRouterAttribute(partialRoutedObject.GetOriginalType());

            if (!hasAttribute)
            {
                return string.Empty;
            }

            var currentUrl = _contextAccessor.HttpContext.Request.Path;

            if (!currentUrl.HasValue || string.IsNullOrEmpty(currentUrl.Value))
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

        private static bool HasPartialRouterAttribute(Type type)
        {
            return type.GetCustomAttribute<PartialRouterAttribute>() != null;
        }
    }
}