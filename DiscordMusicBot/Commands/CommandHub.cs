using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands
{
    internal static class CommandHub
    {
        private static Stack<ICommand> commandHistory = new Stack<ICommand>();
        private static Queue<ICommand> commandQueue = new Queue<ICommand>();

        public static async Task ExecuteCommand(ICommand command)
        {
            Debug.Log($"<color=#FFA500>[CommandHub] ICommand Executed</color> <b>{command.GetType()}</b>");
            await command.ExecuteAsync();
            commandHistory.Push(command);
        }

        public static async Task UndoLastCommand()
        {
            if (commandHistory.Count > 0)
            {
                ICommand lastCommand = commandHistory.Pop();
                await lastCommand.Undo();
            }
        }

        // Add a method to be called from an Update loop in a MonoBehaviour, since static classes cannot have Update methods
        public static async Task UpdateCommandQueue()
        {
            if (commandQueue.Count > 0)
            {
                ICommand nextCommand = commandQueue.Dequeue();
                await ExecuteCommand(nextCommand);
            }
        }

        public static void QueueCommand(ICommand command)
        {
            commandQueue.Enqueue(command);
        }

    }
}