using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Common
{
    public static class HtmlUtil
    {
        public static string Logo175Content { get; set; } = null!;

        public static string SetLogo(string html)
        {
#pragma warning disable S1075 // URIs should not be hardcoded
            const string logoUrl = "https://github.com/atias007/Planar/blob/master/src/Planar/Content/logo2.png?raw=true";
#pragma warning restore S1075 // URIs should not be hardcoded
            switch (AppSettings.Smtp.HtmlImageMode)
            {
                case ImageMode.Embedded:
                    html = Replace(html, "Logo", $"data:image/png;base64,{Logo175Content}");
                    break;

                case ImageMode.External:
                    html = Replace(html, "Logo", logoUrl);
                    break;

                case ImageMode.Internal:
                    if (Uri.TryCreate(AppSettings.Smtp.HtmlImageInternalBaseUrl, UriKind.Absolute, out var url))
                    {
                        var imageUrl = new Uri(url, "/content/email-logo.png").ToString();
                        html = Replace(html, "Logo", imageUrl);
                    }
                    else
                    {
                        html = Replace(html, "Logo", $"data:image/png;base64,{Logo175Content}");
                    }
                    break;
            }
            return html;
        }

        private static string Replace(string html, string key, string? value)
        {
            return html.Replace($"{{{{{key}}}}}", value);
        }
    }
}