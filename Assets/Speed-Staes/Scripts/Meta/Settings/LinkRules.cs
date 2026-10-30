using System;

namespace SpeedStars.Meta.Settings
{
    /// <summary>Which addresses the settings window may open: only absolute http and https pages.</summary>
    public static class LinkRules
    {
        public static bool IsOpenable(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
