﻿using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.Events;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using System.Globalization;

namespace DiscordMusicBot.Services.Managers
{
    internal class BotManager : IServiceBotManager
    {
        private DiscordSocketClient? _client;
        private BotData _botData;
        private BotTimer _botTimer;
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
            _client.Connected += SubscribeToEvents;
            _client.Disconnected += UnsubscribeToEvents;
            _client.ButtonExecuted += Ev_ButtonExecutedAsync;

            _botTimer = new BotTimer(60000);
            await Service.Get<IServiceAnalytics>().Initialize();
            _botData = Service.Get<IServiceDataManager>().LoadConfig();

            await _client.LoginAsync(TokenType.Bot, _botData.ApiKey);
            await _client.StartAsync();

            await UpdateBotStatus();

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
            if (_client != null) _client.SlashCommandExecuted += Ev_SlashCommandHandler;
        }

        private async Task Ev_SlashCommandHandler(SocketSlashCommand command)
        {
            _ = Task.Run(async () =>
            {
                await Service.Get<IServiceCommandManager>().ExecuteCommand(command);
            });
        }

        private async Task Ev_ButtonExecutedAsync(SocketMessageComponent component)
        {
            await component.Message.DeleteAsync();

            var user = component.User;
            var index = 0;
            switch (component.Data.CustomId)
            {
                case "press_0":
                    index = 0;
                    break;
                case "press_1":
                    index = 1;
                    break;
                case "press_2":
                    index = 2;
                    break;
                case "press_3":
                    index = 3;
                    break;
                case "press_4":
                    index = 4;
                    break;
                case "press_5":
                    index = 5;
                    break;
            }
            var results = Service.Get<IServiceYtdlp>().SearchResults;
            await component.RespondAsync($"Selected {results[index].Title}");

            Debug.Log($"<color=red>{user.Username}</color> <color=white>picked song</color> <color=cyan>{index}#</color>");
            _ = Task.Run(async () =>
            {
                await Service.Get<IServiceAudioManager>().PlaySong(results[index].Title, results[index].Url);
            });

            var msg = await component.ModifyOriginalResponseAsync((m) => m.Content = $"Adding {results[index].Title} to Queue");
            await Service.Get<IServiceAnalytics>().AddSongAnalytics(user.Username, new SongData { Title = results[index].Title, Url = results[index].Url });
            await Task.Delay(1000); //This can be a problem with gateway task blocking
            await msg.DeleteAsync();
        }

        private async Task Ev_ReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheable, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var message = await cacheable.GetOrDownloadAsync();
            if (message == null || message.Author.Id != _client.CurrentUser.Id) return;

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
                        await Service.Get<IServiceAnalytics>().AddSongAnalytics(user.Username, new SongData { Title = results[i].Title, Url = results[i].Url });
                        await Task.Delay(5000);
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

        private async Task SubscribeToEvents()
        {
            EventHub.Subscribe<EvOnTimerLoop>((a) =>
            {
                Task.Run(async () =>
                {
                    if (_client == null) return;
                    if (Service.Get<IServiceFFmpeg>().IsSongPlaying) return;
                    await UpdateBotStatus();
                });
            });

            EventHub.Subscribe<EvOnFFmpegExit>((a) =>
            {
                if (Service.Get<IServiceAudioManager>().SongCount > 0) return;
                Task.Run(async () =>
                {
                    if (_client == null) return;
                    await UpdateBotStatus();
                });
            });

            EventHub.Subscribe<EvOnPlayNextSong>((a) =>
            {
                Task.Run(async () =>
                {
                    Debug.Log("EvOnPlayNextSong! Updating status song playing");
                    if (_client == null) return;
                    await _client.SetCustomStatusAsync($"Playin '{a.Title}'");
                });
            });

            await Task.CompletedTask;
        }

        private async Task UnsubscribeToEvents(Exception exception)
        {
            EventHub.Unsubscribe<EvOnFFmpegExit>((a) => { Debug.Log("Unsubscribed from event EvOnFFmpegExit"); });
            EventHub.Unsubscribe<EvOnPlayNextSong>((a) => { Debug.Log("Unsubscribed from event EvOnFFmpegExit"); });
            await Task.CompletedTask;
        }

        private async Task UpdateBotStatus()
        {
            // var currentTime = DateTime.Now.ToString("h:mmtt");
            // currentTime = currentTime.Replace(".", "");
            // await _client.SetCustomStatusAsync($"[{currentTime}] {GetRandomMotto(_botData)}");
            await _client.SetCustomStatusAsync($"{GetRandomMotto(_botData)}");
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

            var motto = new string[botData.CustomStatus.Length + 1];
            for (var i = 0; i < motto.Length; i++)
            {
                if (i >= botData.CustomStatus.Length) break;
                motto[i] = botData.CustomStatus[i];
            }
            motto[motto.Length - 1] = specialMotto;
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
