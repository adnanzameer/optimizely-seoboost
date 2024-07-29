using System.Text;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace SeoBoost.Extensions
{
    public static class SeoBoostExtensions
    {
        public static bool IsInEditMode()
        {
            var contextModeResolver = ServiceLocator.Current.GetInstance<IContextModeResolver>();
            var mode = contextModeResolver.CurrentMode;
            return mode is ContextMode.Edit or ContextMode.Preview;
        }


        public static string GetDefaultRobotsContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Disallow: /episerver/");
            sb.AppendLine("Disallow: /utils/");

            return sb.ToString();
        }
    }
}