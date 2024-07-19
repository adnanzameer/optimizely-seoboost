using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SeoBoost.Helper
{
    public static class HttpContextUtility
    {

        public static void SetItem(HttpContext? httpContext, KeyValuePair<string, string> keyValue)
        {
            if (httpContext != null)
                httpContext.Items[keyValue.Key] = keyValue.Value;
        }

        public static string GetItem(HttpContext? httpContext, string key)
        {
            if (httpContext != null)
            {
                httpContext.Items.TryGetValue(key, out var item);
                if (item != null) return item.ToString();
            }
            return "";
        }

        public static T? GetItems<T>(HttpContext? httpContext, string key) where T : class
        {
            if (httpContext != null)
            {
                httpContext.Items.TryGetValue(key, out var item);
                if (item != null) return (T)item;
            }
            return null;
        }
    }
}
