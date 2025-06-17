using Discord.WebSocket;
using Discord;
using DiscordMusicBot2.Bot.Interface;
using DiscordMusicBot2.Events;
using DiscordMusicBot2.Services;
using DiscordMusicBot2.Chat.Interface;

namespace DiscordMusicBot2.Bot
{
    internal class ServiceBot : IServiceBot
    {
        DiscordSocketClient? m_client;
        SocketGuild? m_guild;

        string m_appId;
        string m_guildId;

        public SocketGuild? Guild => m_guild;

        public ServiceBot() 
        {

        }

        public async Task StartDiscordBot(string appId, string guildId)
        {
            SubscribeToEvents();

            m_appId = appId;
            m_guildId = guildId;

            m_client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildVoiceStates
            });

            m_client.Log += msg => { Console.WriteLine(msg); return Task.CompletedTask; };
            m_client.Ready += OnClientReady;

            await m_client.LoginAsync(TokenType.Bot, appId);
            await m_client.StartAsync();
        }

        private async Task OnClientReady()
        {
            if(ulong.TryParse(m_guildId, out var result))
            {
                m_guild = m_client?.GetGuild(result);
            }
            else
            {
                Debug.Log("<color=red>Guild ID (Server Id) is WRONG. Please fix it in your config and restart the bot or Slash Commands wont register/work.</color>");
                return;
            }


            //            if (CLEAR_SLASH_COMMANDS)
            //            {
            //#pragma warning disable CS0162 // Unreachable code detected
            //                await SlashCommandClear(guild);
            //#pragma warning restore CS0162 // Unreachable code detected
            //            }
            //            else
            //            {
            if (m_guild != null)
                await Service.Get<IServiceSlashCommands>().RegisterAllCommands(m_guild);
            if (m_client != null)
                m_client.SlashCommandExecuted += OnSlashCommandRecieved;
            //}
        }

        public async Task StopDiscordBot()
        {
            UnsubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            EventHub.Subscribe<OnTickEvent>(OnTickEventSubscription);
        }

        private void UnsubscribeToEvents()
        {
            EventHub.Unsubscribe<OnTickEvent>(OnTickEventSubscription);
        }

        private async Task OnTickEventSubscription(OnTickEvent @event)
        {
            Debug.Log($"OnTickEvent Executed: {@event.Tick}");
        }

        private async Task OnSlashCommandRecieved(SocketSlashCommand command)
        {
            _ = Task.Run(async () => await Service.Get<IServiceSlashCommands>().ExecuteCommand(command));
            await Task.CompletedTask;
        }
    }
}
