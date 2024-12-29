using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.Events;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using System.Globalization;

namespace DiscordMusicBot.Services.Services
{
    internal class BotManager : IServiceBotManager
    {
        private DiscordSocketClient? _client;
        public DiscordSocketClient? Client => _client;
        public async Task Initialize()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildVoiceStates
            });

            _client.Log += Ev_Log;
            _client.Ready += Ev_ClientReady;
            _client.ReactionAdded += Ev_ReactionAddedAsync;

            await Service.Get<IServiceAnalytics>().Initialize();
            var botData = Service.Get<IServiceDataManager>().LoadConfig();

            SubscribeToEvents(botData);

            await _client.LoginAsync(TokenType.Bot, botData.ApiKey);
            await _client.StartAsync();
            await _client.SetCustomStatusAsync(GetRandomMotto(botData));
            // Block this task
            await Task.Delay(-1);
        }
         
        private async Task Ev_ClientReady()
        {
            // Ensure you have the correct guild ID (Replace it with your server id)
            var botData = Service.Get<IServiceDataManager>().LoadConfig();
            ulong guildId = ulong.Parse(botData.GuildId);
            if (guildId == 0) Debug.Log("<color=red>Invalid guild id. Bot may not work correctly. (Registering commands)</color>");
            var guild = _client?.GetGuild(guildId);

            // - Clear all server slash commands ---
            // await SlashCommandClear(guild); 
            // -------------------------------------------------

            if (guild != null) await Service.Get<IServiceCommandManager>().RegisterAllCommands(guild);
            if(_client != null) _client.SlashCommandExecuted += Ev_SlashCommandHandler;
        }

        private async Task Ev_SlashCommandHandler(SocketSlashCommand command)
        {
            _ = Task.Run(async () =>
            {
                await Service.Get<IServiceCommandManager>().ExecuteCommand(command);
            });
        }

        private async Task Ev_ReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheable, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var message = await cacheable.GetOrDownloadAsync();
            if (message == null) return;
            _ = Task.Run(async () =>
            {
                string[] _numberEmojis = new string[]
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
                // Replace with your custom emoji ID and name
                //var emojiIds = new ulong[] { 429753831199342592, 466478794367041557, 466477774455177247, 582418378178822144 };
                for (int i = 0; i < Service.Get<IServiceYtdlp>().SearchResults.Count; i++)
                {
                    //For custom emotes
                    //var emote = Emote.Parse($"<:warrior{i}:{emojiIds[i]}>");
                    // if (reaction.Emote is Emote e && e.Id == emojiIds[i])
                    if (GetUnicodeCodePoints(reaction.Emote.Name) == GetUnicodeCodePoints(_numberEmojis[i]))
                    {
                        var user = reaction.User.IsSpecified ? reaction.User.Value : null;
                        if (user == null) return;
                        if (user.IsBot) return;

                        Debug.Log($"<color=red>{user.Username}</color> <color=white>picked song</color> <color=cyan>{i}#</color>");
                        var results = Service.Get<IServiceYtdlp>().SearchResults;
                        _ = Task.Run(async () =>
                        {
                            await Service.Get<IServiceAudioManager>().PlaySong(results[i].Title, results[i].Url);
                        });
                        await message.ModifyAsync((m) => m.Content = $"Adding {results[i].Title} to Queue");
                        // await Service.Get<IServiceAnalytics>().AddSongAnalytics(user.Username, new SongData { Title = results[i].Title, Url = results[i].Url });
                        // Debug.Log("Deleting message... (5000ms)");
                        await Task.Delay(5000);
                        // Debug.Log("DEBUG: should now delete message!");
                        await message.DeleteAsync();
                    }
                }
            });
        }

        private static Task Ev_Log(LogMessage msg)
        {
            var colorTag = msg.Severity == LogSeverity.Error || msg.Severity == LogSeverity.Critical ? "red" : "white";
            colorTag = msg.Severity == LogSeverity.Warning ? "yellow" : colorTag;
            Debug.Log($"<color={colorTag}>{msg.ToString()}</color>");
            return Task.CompletedTask;
        }
        
        private void SubscribeToEvents(BotData data)
        {
            EventHub.Subscribe<EvOnFFmpegExit>((a) =>
            {
                if (Service.Get<IServiceAudioManager>().SongCount > 0) return;
                Task.Run(async () =>
                {
                    if (_client == null) return;
                    await _client.SetCustomStatusAsync(GetRandomMotto(data));
                });
            });

            EventHub.Subscribe<EvOnPlayNextSong>((a) =>
            {
                Debug.Log("Event played EvOnPlayNextSong!");

                Task.Run(async () =>
                {
                    Debug.Log("EvOnPlayNextSong! Tryin to show song playing");
                    if (_client == null) return;
                    await _client.SetCustomStatusAsync($"Playin '{a.Title}'");
                });
            });
        }

        private void UnsubscribeToEvents()
        {
            EventHub.Unsubscribe<EvOnFFmpegExit>((a)=>{ Debug.Log("Unsubscribed from event EvOnFFmpegExit"); });
            EventHub.Unsubscribe<EvOnPlayNextSong>((a)=>{ Debug.Log("Unsubscribed from event EvOnFFmpegExit"); });
        }

        private string GetUnicodeCodePoints(string input)
        {
            StringInfo stringInfo = new StringInfo(input);
            string result = "";

            for (int i = 0; i < stringInfo.LengthInTextElements; i++)
            {
                string textElement = stringInfo.SubstringByTextElements(i, 1);
                foreach (char c in textElement)
                {
                    result += $"\\u{((int)c):X4}";
                }
            }

            return result;
        }

        private string GetRandomMotto(BotData botData)
        {
            var specialMotto = "";
            if (DateTime.Now.Month == 12) specialMotto = "Merry Christmas!"; //december
            if (DateTime.Now.Month == 1) specialMotto = "Happy new year!"; //january
            if (DateTime.Now.Month == 10) specialMotto = "Spooky scary skeletons!";  //october

            var motto = new string[botData.CustomStatus.Length+1];
            for(var i = 0; i < motto.Length; i++)
            {
                if(i >= botData.CustomStatus.Length) break;
                motto[i] = botData.CustomStatus[i];
            }
            motto[motto.Length-1] = specialMotto;
            return motto[Random.Shared.Next(motto.Length)];
        }

        private async Task SlashCommandClear(SocketGuild guild)
        {
            // Clear existing commands
            _ = Task.Run(async () =>
            {
                var commands = await guild.GetApplicationCommandsAsync();
                foreach (var command in commands)
                {
                    await command.DeleteAsync();
                }
            });
        }
    }
}
