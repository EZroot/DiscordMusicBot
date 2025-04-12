using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Utils
{
    using System;

    public class UrlHelper
    {
        public static string RemoveQueryString(string url)
        {
            // Check if the URL is null or empty
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            // Find the index of the question mark
            int index = url.IndexOf('?');

            // If a question mark is found, return the substring up to that index
            if (index >= 0)
            {
                return url.Substring(0, index);
            }

            // If no question mark is found, return the original URL
            return url;
        }
    }

}
