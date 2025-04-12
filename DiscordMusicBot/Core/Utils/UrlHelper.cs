using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Utils
{
    using System;
    using System.Collections.Specialized;
    using System.Web;

    public class UrlHelper
    {
        public static string RemoveQueryString(string url)
        {
            // Return the original URL if it is null, empty or only whitespace.
            if (string.IsNullOrWhiteSpace(url))
                return url;

            try
            {
                var uri = new Uri(url);

                // If the URL is from youtube.com
                if (uri.Host.IndexOf("youtube.com", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // If it is the /watch page, we want to keep the "v" parameter.
                    if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
                    {
                        // Parse the query parameters to extract "v"
                        NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);
                        string videoId = queryParams["v"];

                        if (!string.IsNullOrEmpty(videoId))
                        {
                            // Return URL reconstructed with only the video id parameter.
                            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?v={videoId}";
                        }
                        else
                        {
                            // No video id found; return the URL without any query parameters.
                            return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                        }
                    }
                    else
                    {
                        // For other youtube.com paths (e.g. embeds or attribution links), return the base URL (without query).
                        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                    }
                }
                // If it is a youtu.be link, the video id is in the path.
                else if (uri.Host.IndexOf("youtu.be", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Return the base URL, which is the short URL with the video id.
                    return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                }
                else
                {
                    // For non-YouTube URLs, simply remove the query (if any).
                    return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                }
            }
            catch (UriFormatException)
            {
                // If the URL is not valid, return the original.
                return url;
            }
        }
    }

}
