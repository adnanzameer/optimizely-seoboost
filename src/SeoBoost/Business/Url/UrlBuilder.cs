using System;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Web;
using EPiServer.Web.Routing;
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
            var result = PartialRouterCheck(contentLink, contentLanguage);

            var loadingOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(contentLanguage) };

            // Custom Canonical Field Check
            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(_configuration.CustomCanonicalTagFieldName))
            {
                var content = _contentRepository.Get<ContentData>(contentLink, loadingOptions);

                if (content != null)
                {
                    var propertyValue = content.GetPropertyValue(_configuration.CustomCanonicalTagFieldName);

                    if (!string.IsNullOrEmpty(propertyValue))
                    {
                        var validUrl = Uri.TryCreate(propertyValue, UriKind.Absolute, out var uriResult)
                                       && (uriResult.Scheme == Uri.UriSchemeHttp ||
                                           uriResult.Scheme == Uri.UriSchemeHttps);

                        if (validUrl)
                        {
                            if (!Uri.TryCreate(propertyValue, UriKind.RelativeOrAbsolute, out var customRelativeUri))
                                return ApplyTrailingSlash(propertyValue);

                            if (customRelativeUri.IsAbsoluteUri)
                                return ApplyTrailingSlash(propertyValue);

                            Uri.TryCreate(SiteDefinition.Current.SiteUrl.AbsoluteUri, UriKind.Absolute, out var uri);

                            if (uri != null)
                            {
                                var absoluteUri = new Uri(uri, customRelativeUri);

                                return ApplyTrailingSlash(absoluteUri.AbsoluteUri);
                            }

                        }
                    }
                }
            }

            var pageData = _contentRepository.Get<PageData>(contentLink, loadingOptions);

            // Mirror/ Shortcut Check
            if (string.IsNullOrEmpty(result) && _configuration.UseMirrorPageReference)
            {
                pageData = GetMirroredTarget(pageData, contentLanguage);
            }

            if (string.IsNullOrEmpty(result) && _configuration.UseSimpleAddress && !string.IsNullOrEmpty(pageData.ExternalURL))
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

            Uri baseUri = null;

            var hostLanguage = string.Empty;

            if (!_configuration.UseSiteUrlAsDefault)
            {
                var siteDefinition = _siteDefinitionResolver.GetByContent(pageData.ContentLink, true, true);
                var hosts = siteDefinition.GetHosts(contentLanguage, true).ToList();

                var host = hosts.FirstOrDefault(h => h.Language != null && h.Language.Equals(contentLanguage));

                baseUri = siteDefinition.SiteUrl;

                if (host != null && host.Name.Equals("*") == false)
                {
                    Uri.TryCreate(siteDefinition.SiteUrl.Scheme + "://" + host.Name, UriKind.Absolute, out baseUri);

                    if (host.Language != null)
                    {
                        hostLanguage = "/" + host.Language.Name.ToLower() + "/";
                    }
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

        private string PartialRouterCheck(ContentReference contentLink, CultureInfo contentLanguage)
        {
            var pageLanguages = _contentRepository.GetLanguageBranches<PageData>(contentLink);
            var match = false;

            if (_contextAccessor.HttpContext != null)
            {
                var currentUrl = _contextAccessor.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(currentUrl.Value) && currentUrl.HasValue)
                {
                    var urlSegments = currentUrl.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (urlSegments.Length > 1)
                    {
                        foreach (var pageData in pageLanguages)
                        {
                            var pageUrl = _urlResolver.GetUrl(
                                pageData.ContentLink,
                                pageData.Language.Name,
                                new VirtualPathArguments
                                {
                                    ContextMode = ContextMode.Default,
                                    ForceCanonical = true
                                });

                            if (pageUrl.Equals(currentUrl.Value, StringComparison.CurrentCultureIgnoreCase))
                            {
                                match = true;
                                break;
                            }

                            if (!string.IsNullOrEmpty(pageData.ExternalURL) && pageData.ExternalURL.Equals(currentUrl.Value, StringComparison.CurrentCultureIgnoreCase))
                            {
                                match = true;
                                break;
                            }
                        }

                        if (!match)
                        {
                            var currentCulture = ContentLanguage.PreferredCulture;

                            var url = currentUrl.Value.Replace("/" + currentCulture.Name.ToLower() + "/", "/" + contentLanguage.Name.ToLower() + "/");
                            return url;
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}