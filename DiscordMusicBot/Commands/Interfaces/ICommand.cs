namespace DiscordMusicBot.Commands.Interfaces
{
    internal interface ICommand
    {
        Task ExecuteAsync();
        Task Undo();
        Task Redo();
    }
}