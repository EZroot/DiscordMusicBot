using System.Diagnostics;

namespace DiscordMusicBot.Utils
{
    public static class Debug
    {
        public static void Log(string input)
        {
            string timeStamp = DateTime.Now.ToString("hh:mm:ss tt");

            StackTrace stackTrace = new StackTrace();
            StackFrame frame = stackTrace.GetFrame(1); 
            var method = frame.GetMethod();
            string callerClassName = method.ReflectedType.Name; 
            string callerMethodName = method.Name;

            input = $"<color=magenta>{timeStamp}</color> <color=yellow>[{callerClassName}]</color> " + input;

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
