using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Services.Managers.Discord
{
    internal class BotManager : IServiceBotManager
    {
        private const double BOT_LOOP_TIMER_MS = 60000;
        private const int SEARCH_RESULT_MSG_DELETE_MS = 5000;
        private const bool CLEAR_SLASH_COMMANDS = false;

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
            _client.Connected += SubscribeToEvents;
            _client.Disconnected += UnsubscribeToEvents;
            _client.ButtonExecuted += Ev_ButtonExecutedAsync;

            new BotTimer(BOT_LOOP_TIMER_MS);
            await Service.Get<IServiceAnalytics>().InitializeAsync();

            var botData = Service.Get<IServiceDataManager>().LoadConfig();
            Debug.Initialize(botData.DebugMode);

            await _client.LoginAsync(TokenType.Bot, botData.ApiKey);
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

            if (CLEAR_SLASH_COMMANDS)
            {
#pragma warning disable CS0162 // Unreachable code detected
                await SlashCommandClear(guild);
#pragma warning restore CS0162 // Unreachable code detected
            }
            else
            {
                if (guild != null)
                    await Service.Get<IServiceCommandManager>().RegisterAllCommands(guild);
                if (_client != null)
                    _client.SlashCommandExecuted += Ev_SlashCommandHandler;
            }
        }

        private async Task Ev_SlashCommandHandler(SocketSlashCommand command)
        {
            _ = Task.Run(async () => await Service.Get<IServiceCommandManager>().ExecuteCommand(command));
            await Task.CompletedTask;
        }

        private async Task Ev_ButtonExecutedAsync(SocketMessageComponent component)
        {
            var user = component.User;
            var songId = component.Data.CustomId;
            var results = Service.Get<IServiceYtdlp>().SearchResultsHistory;
            var selectedSong = results.Find(x => x.Id == songId);
            await component.Message.ModifyAsync((m) => { m.Content = $"{component.User.Mention} Picked **{selectedSong.Title}**"; m.Components = null; });
            
            //Formated for debugging
            var title = selectedSong.Title;
            var formatTitle = title.Length > 50 ? title.Substring(0,42) : title;
            Debug.Log($"<color=red>{user.Username}</color> <color=white>picked song</color> <color=cyan>{formatTitle}#</color>");
            
            var songData = new SongData() { Title = selectedSong.Title, Url = selectedSong.Url, Length = selectedSong.Length };
            _ = Task.Run(async () => await Service.Get<IServiceAudioManager>().PlaySong(songData));
            await component.RespondAsync($"You've added '{selectedSong.Title}' to Queue", ephemeral: true);
            // await Task.Delay(SEARCH_RESULT_MSG_DELETE_MS);
            await component.Message.DeleteAsync();
            await component.Message.ModifyAsync((m)=> m.Content = "test");
        }

        private static Task Ev_Log(LogMessage msg)
        {
            var colorTag = msg.Severity == LogSeverity.Error || msg.Severity == LogSeverity.Critical ? "red" : "white";
            colorTag = msg.Severity == LogSeverity.Warning ? "yellow" : colorTag;
            if (colorTag == "yellow") return Task.CompletedTask;
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
                    var title = a.Title;
                    var formatTitle = title.Length > 50 ? title.Substring(0,42) : title;
                    Debug.Log($"<color=magenta>Playing</color>: <color=white>{formatTitle}</color>");
                    if (_client == null) return;
                    await _client.SetCustomStatusAsync($"Playin '{title}'");
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
            var botData = Service.Get<IServiceDataManager>().LoadConfig();
            await _client.SetCustomStatusAsync($"{GetRandomMotto(botData)}");
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
            await Task.CompletedTask;
        }
    }
}
