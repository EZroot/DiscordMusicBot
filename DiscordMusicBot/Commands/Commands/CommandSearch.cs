using Discord.WebSocket;
using Discord;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.Commands.Commands
{
    internal class CommandSearch : IDiscordCommand
    {
        private string _commandName = "search";
        private string[] _numberEmojis = new string[]
        {
                "\u0030\uFE0F\u20E3",
                "\u0031\uFE0F\u20E3",
                "\u0032\uFE0F\u20E3",
                "\u0033\uFE0F\u20E3",
                "\u0034\uFE0F\u20E3",
                "\u0035\uFE0F\u20E3",
                "\u0036\uFE0F\u20E3",
                "\u0037\uFE0F\u20E3",
                "\u0038\uFE0F\u20E3",
                "\u0039\uFE0F\u20E3",
                "\u0031\uFE0F\u20E3\u0030\uFE0F\u20E3"
        };
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Searches youtube based on the keyword")
            .AddOption("key", ApplicationCommandOptionType.String, "Search...", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var arg = (string)(command.Data.Options.First().Value);

            await command.RespondAsync($"Searching: {arg}");
            var audioManager = Service.Get<IServiceAudioManager>();
            await audioManager.CheckAndJoinVoice(command);
            var result = await Service.Get<IServiceYtdlp>().SearchYoutube(arg);
            if (result == null)
            {
                await command.ModifyOriginalResponseAsync((m) => m.Content = "Error failed to search youtube.");
                return;
            }
            var message = $"```bash\n* -- * -- Searched: {arg} -- * -- *\n\n";
            for (int i = 0; i < result.Count; i++)
            {
                var r = result[i];
                var title = r.Title;
                if (title.Length > 64) title = title.Substring(0, 64); 
                message += $"{i}# {title}\n";
            }
            message += "```";
            var messageList = await command.ModifyOriginalResponseAsync((m) => m.Content = message);

            //Use this for custom emojis
            //var emojiIds = new ulong[] { 429753831199342592, 466478794367041557, 466477774455177247, 582418378178822144 };
            for (int i = 0; i < result.Count; i++)
            {
                //var emote = Emote.Parse($"<:warrior{i}:{emojiIds[i]}>");
                await messageList.AddReactionAsync(new Emoji(_numberEmojis[i]));
            }
        }
    }
}
