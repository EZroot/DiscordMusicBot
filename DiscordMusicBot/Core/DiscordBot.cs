using System.Net.Http.Headers;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Core
{
    internal class DiscordBot
    {
        private const double BOT_LOOP_TIMER_MS = 60000;
        private const int SEARCH_RESULT_MSG_DELETE_MS = 5000;
        private const bool CLEAR_SLASH_COMMANDS = false;

        private DiscordSocketClient? _client;
        public DiscordSocketClient? Client => _client;

        bool _hasConnected = false;
        public DiscordBot()
        {

        }
        
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
            var tokenValid = await IsTokenValid(botData.ApiKey);
            if (tokenValid)
            {
                await _client.LoginAsync(TokenType.Bot, botData.ApiKey);
                await _client.StartAsync();

                await UpdateBotStatus();
                _hasConnected = true;
                await Service.Get<IServiceAudioPlaybackService>().SkipSongRaw();
                // Block this task
                await Task.Delay(-1);
            }

            Debug.Log($"Token invalid. Token:{botData.ApiKey}");
        }
        
        public async Task<bool> IsTokenValid(string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
            var response = await client.GetAsync("https://discord.com/api/v10/users/@me");
            return response.IsSuccessStatusCode;
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

            try
            {
                await Service.Get<IServiceAnalytics>().AddSongAnalyticsAsync(user.Username, songData);
            }
            catch (Exception e)
            {
                Debug.Log($"<color=red>Analytics Error: {e.Message}");
            }

            await Service.Get<IServiceAudioPlaybackService>().QueueSongToPlay(songData);
            
            await component.RespondAsync($"You've added '{selectedSong.Title}' to Queue", ephemeral: true);
            // await Task.Delay(SEARCH_RESULT_MSG_DELETE_MS);
            //Create some kind of queue that will queue delete the message history too, this should help with timeouts if we react too many times
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
                    if(_hasConnected == false)
                    {
                        await Initialize();
                    }
                });
            });

            EventHub.Subscribe<EvOnFFmpegExit>(async (a) =>
            {
                await Service.Get<IServiceAudioPlaybackService>().ClearSong();
                if (Service.Get<IServiceAudioPlaybackService>().SongCount > 0) return;
                _ = Task.Run(async () =>
                {
                    if (_client == null) return;
                    await UpdateBotStatus();
                });
            });

            EventHub.Subscribe<EvOnPlayNextSong>((a) =>
            {
                Task.Run(async () =>
                {
                    var song = new SongData() { Title = a.Title, Url = a.Url, Length = a.Length };
                    var title = song.Title;
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
            _hasConnected = false;
            await Service.Get<IServiceAudioPlaybackService>().ClearSong();
            await Service.Get<IServiceAudioPlaybackService>().LeaveVoiceRaw();
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
