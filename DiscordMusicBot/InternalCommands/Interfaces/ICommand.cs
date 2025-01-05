namespace DiscordMusicBot.InternalCommands.Interfaces
{
    internal interface ICommand
    {
        Task ExecuteAsync();
        Task Undo();
        Task Redo();
    }
}