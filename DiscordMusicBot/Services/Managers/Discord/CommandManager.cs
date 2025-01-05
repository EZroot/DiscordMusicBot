using DiscordMusicBot.Services.Interfaces;
using Discord;
using Discord.WebSocket;
using System.Reflection;
using DiscordMusicBot.Utils;
using DiscordMusicBot.SlashCommands.Interfaces;

namespace DiscordMusicBot.Services.Managers.Discord
{
    internal class CommandManager : IServiceCommandManager
    {

        private readonly Dictionary<string, IDiscordCommand> _commands = new();
        private readonly List<SlashCommandBuilder> _slashCommandBuilder = new();

        public async Task RegisterAllCommands(SocketGuild guild)
        {
            // Get all types that implement IDiscordCommand
            var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IDiscordCommand).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var commandType in commandTypes)
            {
                // Create an instance of the command
                var commandInstance = (IDiscordCommand)Activator.CreateInstance(commandType);
                // Register the command
                RegisterCommand(commandInstance);
            }

            await BuildAllCommands(guild);
        }

        public async Task ExecuteCommand(SocketSlashCommand slashCommand)
        {
            if (_commands.TryGetValue(slashCommand.Data.Name, out var command))
            {
                try
                {
                    var potentialOption = slashCommand.Data.Options.First().Value;
                    Debug.Log($"<color=red>{slashCommand.User.Username}</color>:> <color=magenta>{slashCommand.Data.Name}</color> <color=cyan>{potentialOption}</color>");
                }
                catch (Exception e)
                {
                    Debug.Log($"<color=red>{slashCommand.User.Username}</color>:> <color=magenta>{slashCommand.Data.Name}</color>");
                }
                finally
                {
                    await command.ExecuteAsync(slashCommand);
                }
            }
            else
            {
                Debug.Log("Command not found.");
            }
        }

        private async Task BuildAllCommands(SocketGuild guild)
        {
            // Clear existing commands
            _ = Task.Run(async () =>
            {
                foreach (var command in _slashCommandBuilder)
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
            });
        }

        private void RegisterCommand(IDiscordCommand command)
        {
            Debug.Log($"Registering Command: <color=yellow>{command.CommandName}</color>");
            _slashCommandBuilder.Add(command.Register());
            AddCommand(command.CommandName, command);
        }

        private void AddCommand(string name, IDiscordCommand command)
        {
            _commands[name] = command;
        }
    }
}
