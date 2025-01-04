using System.Text;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.Commands.CommandArgs
{
    public class CmdSendSearchResult : ICommand
    {

        SocketSlashCommand _command;

        public CmdSendSearchResult(SocketSlashCommand command)
        {
            _command = command;
        }

        public async Task ExecuteAsync()
        {
            var buttonBuilder = new ComponentBuilder();
            var stringBuilder = new StringBuilder();
            var audioManager = Service.Get<IServiceAudioManager>();
            var dataManager = Service.Get<IServiceDataManager>();
            var arg = (string)(_command.Data.Options.First().Value);

            await _command.RespondAsync($"Searching: {arg}");
            await audioManager.CheckAndJoinVoice(_command);

            var result = await Service.Get<IServiceYtdlp>().SearchYoutube(arg);
            if (result == null)
            {
                await _command.ModifyOriginalResponseAsync((m) => m.Content = "Error failed to search youtube.");
                return;
            }

            stringBuilder.Append($"```bash\n* -- * -- Searched: {arg} -- * -- *\n\n");
            for (int i = 0; i < result.Count; i++)
            {
                //Build our selection buttons
                ButtonStyle style = ButtonStyle.Secondary;
                buttonBuilder.WithButton(
                    customId: $"{result[i].Id}", 
                    style: style,             
                    emote: new Emoji(dataManager.LoadConfig().SearchResultButtonEmojis[i]));

                //Parse our results string so its not too long
                var title = result[i].Title;
                if (title.Length > 64) title = title.Substring(0, 64);
                stringBuilder.Append($"{i}# {title}\n");
            }
            stringBuilder.Append("```");
            var messageList = await _command.ModifyOriginalResponseAsync((m) => { m.Content = stringBuilder.ToString(); m.Components = buttonBuilder.Build(); } );
        }

        public async Task Redo()
        {
            Utils.Debug.Log($"<color=red>Error: Redo unavailable in {this}");
            await Task.CompletedTask;
        }

        public async Task Undo()
        {
            Utils.Debug.Log($"<color=red>Error: Undo unavailable in {this}");
            await Task.CompletedTask;
        }
    }
}