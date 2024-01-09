using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Globalization;
using EPiServer.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeoBoost.Extensions;
using SeoBoost.Models.Pages;

namespace SeoBoost.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SBRobotsTxtController : Controller
    {
        private readonly IContentLoader _contentLoader;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(SBRobotsTxtController));
        public SBRobotsTxtController(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        [HttpGet]
        [Route("robots.txt")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                var items = _contentLoader.GetChildren<SBRobotsTxt>(ContentReference.StartPage, 
                    new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(ContentLanguage.PreferredCulture) });

                var content = SeoBoostExtensions.GetDefaultRobotsContent();
                if (items != null)
                {
                    var robotTxtPages = items.ToList();

                    if (robotTxtPages.Any())
                    {
                        content = robotTxtPages.First().RobotsContent;
                    }
                }

                Response.Headers.CacheControl = "public, max-age=300";

                return new ContentResult
                {
                    Content = content,
                    ContentType = "text/plain",
                    StatusCode = 200
                };
            }
            catch (Exception exception)
            {
                _logger.Error("The robots.txt for the current site could not be loaded.", exception);
                throw;
            }
        }
    }
}