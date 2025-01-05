namespace DiscordMusicBot.Utils
{
    public class FormatHelper
    {
        public static string FormatLengthWithDescriptor(string length)
        {
            string[] parts = length.Split(':');
            if (parts.Length == 3) 
            {
                if (int.Parse(parts[0]) > 0)
                    return length + " hours";
                else
                    return parts[1] + ":" + parts[2] + " mins"; // Show only mm:ss
            }
            else if (parts.Length == 2)
            {
                return length + " mins";
            }

            return length;
        }

    }
}