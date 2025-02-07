using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DiscordMusicBot.Utils
{
    public static class Debug
    {
        private static bool _isDebugMode = false;

        public static void Initialize(bool isDebugMode)
        {
            _isDebugMode = true;// isDebugMode;
        }
        private static string CleanGeneratedNames(string name)
        {
            // First, handle nested types and generics by removing generic indicators and replacing '+' with '.'
            int genericIndex = name.IndexOf('`');
            if (genericIndex != -1)
            {
                name = name.Substring(0, genericIndex);
            }
            name = name.Replace('+', '.');

            // Use a regex to extract meaningful parts from compiler-generated names like "<SearchYoutube>d__6"
            Match meaningfulPart = Regex.Match(name, @"\<(.+?)\>");
            if (meaningfulPart.Success)
            {
                // If a meaningful part is found within <>, use it directly
                name = meaningfulPart.Groups[1].Value;
            }
            else
            {
                // If no <> patterns are found, remove any trailing ".MoveNext" if present
                name = Regex.Replace(name, @"\.MoveNext$", "");
            }

            return name;
        }


        public static void Log(string input)
        {
            // Format the timestamp without spaces
            var timeStamp = DateTime.Now.ToString("h:mm tt").Replace(" ", "");

            // Get the calling method details
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1); // Adjust the frame index if necessary
            var method = frame.GetMethod();

            // Clean up the class name to handle generics and nested classes
            var callerClassName = CleanGeneratedNames(method.ReflectedType.Name);
            var callerMethodName = CleanGeneratedNames(method.Name);

            if (_isDebugMode)
                input = $"<color=magenta>{timeStamp}</color> <color=yellow>[{callerClassName}]</color> " + input;
            else
                input = $"<color=magenta>{timeStamp}</color> " + input;

            int currentIndex = 0;

            while (currentIndex < input.Length)
            {
                int openTagStart = input.IndexOf("<color=", currentIndex);
                if (openTagStart == -1)
                {
                    Console.Write(input.Substring(currentIndex));
                    break;
                }
                Console.Write(input.Substring(currentIndex, openTagStart - currentIndex));

                int openTagEnd = input.IndexOf(">", openTagStart);
                if (openTagEnd == -1)
                {
                    Console.Write(input.Substring(currentIndex));
                    break;
                }
                string colorName = input.Substring(openTagStart + 7, openTagEnd - (openTagStart + 7));
                ConsoleColor color;
                if (Enum.TryParse(colorName, true, out color))
                {
                    Console.ForegroundColor = color;
                }

                int closeTagStart = input.IndexOf("</color>", openTagEnd);
                if (closeTagStart == -1)
                {
                    Console.Write(input.Substring(openTagEnd + 1));
                    break;
                }

                Console.Write(input.Substring(openTagEnd + 1, closeTagStart - (openTagEnd + 1)));
                Console.ResetColor();

                currentIndex = closeTagStart + 8;
            }
            Console.WriteLine();
        }
    }
}
