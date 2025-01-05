using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.CommandArgs.DiscordChat
{
    public class CmdSendSearchResult : ICommand
    {

        private SocketSlashCommand _command;
        private bool _showEmbed;
        private bool _shortButtonMode;

        public CmdSendSearchResult(SocketSlashCommand command)
        {
            _command = command;
        }

        public async Task ExecuteAsync()
        {
            var audioManager = Service.Get<IServiceAudioManager>();
            var dataManager = Service.Get<IServiceDataManager>();

            var arg = (string)(_command.Data.Options.First().Value);
            var user = _command.User;

            await _command.RespondAsync($"Thinking ...", ephemeral: false);
            await audioManager.CheckAndJoinVoice(_command);

            var result = await Service.Get<IServiceYtdlp>().SearchYoutube(arg);
            if (result == null)
            {
                await _command.ModifyOriginalResponseAsync((m) => m.Content = "Error failed to search youtube.");
                return;
            }

            var replyItems = BuildReply(user, arg, result);
            
            if (_showEmbed)
                await _command.ModifyOriginalResponseAsync((m) => { m.Content = ""; m.Embed = replyItems.Item2.Build(); m.Components = replyItems.Item1.Build(); });
            else
                await _command.ModifyOriginalResponseAsync((m) => { m.Content = $"**{user.GlobalName}**'s searchin' for **'{char.ToUpper(arg[0]) + arg.Substring(1).ToLower()}'**"; m.Components = replyItems.Item1.Build(); });
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

        private (ComponentBuilder, EmbedBuilder) BuildReply(IUser user, string arg, List<SongData>? result)
        {
            var dataManager = Service.Get<IServiceDataManager>();
            var buttonBuilder = new ComponentBuilder();
            var embedTitle = $"{char.ToUpper(user.Username[0]) + user.Username.Substring(1).ToLower()} searched for '{char.ToUpper(arg[0]) + arg.Substring(1).ToLower()}'";
            // Build an embed with a more structured and cleaner look
            var embed = new EmbedBuilder()
                .WithTitle(embedTitle)
                .WithColor(new Color(255, 70, 0)); // Sets the embed to dark orange

            for (int i = 0; i < result.Count; i++)
            {
                var title = result[i].Title;
                var trimmedUrl = result[i].Url.Replace("https://", " ");
                var songLength = result[i].Length;

                var buttonLabel = $"[{songLength}] \t {title}";
                if (buttonLabel.Length > 77) buttonLabel = buttonLabel.Substring(0, 77) + "..."; // Add ellipsis to indicate truncation
                
                embed.AddField($"#{i + 1}  {buttonLabel}", $" [{trimmedUrl}]({result[i].Url})", false);

                // Build selection buttons
                ButtonStyle style = ButtonStyle.Secondary;
                var emojiIndex = i + 1 < dataManager.LoadConfig().SearchResultButtonEmojis.Count ? i + 1 : 0;
                try
                {
                    if (_shortButtonMode)
                    {
                         buttonBuilder.WithButton(
                            customId: $"{result[i].Id}",
                            style: style,
                            emote: new Emoji(dataManager.LoadConfig().SearchResultButtonEmojis[emojiIndex]));
                    }
                    else
                    {
                        buttonBuilder.WithButton(
                            label: buttonLabel,
                            customId: $"{result[i].Id}",
                            style: style,
                            emote: new Emoji(dataManager.LoadConfig().SearchResultButtonEmojis[emojiIndex]),
                            row: i);
                    }
                }
                catch (Exception e)
                {
                    Utils.Debug.Log(e.Message);
                }
            }

            return (buttonBuilder,embed);
        }
    }
}