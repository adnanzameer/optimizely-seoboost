using System.Text;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using SeoBoost.Models.Pages;

namespace SeoBoost.Controllers
{
    public class SBRobotsTxtController : PageController<SBRobotsTxt>
    {
        private readonly IContentLoader _contentLoader;

        public SBRobotsTxtController(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        [HttpGet]
        [Route("robots.txt", Name = "robots.txt")]
        public IActionResult Index(SBRobotsTxt currentPage)
        {
            var items = _contentLoader
                .GetChildren<SBRobotsTxt>(ContentReference.StartPage, new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() });

            return Content(currentPage.RobotsContent, "text/plain", Encoding.UTF8);
        }

    }
}


