using Discord;
using Discord.WebSocket;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Chat.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Chat
{
    internal class ServiceSlashCommands : IServiceSlashCommands
    {
        private readonly Dictionary<string, IBotCommand> _commands = new();
        private readonly List<SlashCommandBuilder> _slashCommandBuilder = new();

        public async Task RegisterAllCommands(SocketGuild guild)
        {
            // Get all types that implement IDiscordCommand
            var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IBotCommand).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var commandType in commandTypes)
            {
                // Create an instance of the command
                var commandInstance = (IBotCommand)Activator.CreateInstance(commandType);
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

        private Task BuildAllCommands(SocketGuild guild)
        {
            // Clear existing commands
            _ = Task.Run(async () =>
            {
                foreach (var command in _slashCommandBuilder)
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
            });
            return Task.CompletedTask;
        }

        private void RegisterCommand(IBotCommand command)
        {
            Debug.Log($"Registering Command: <color=yellow>{command.CommandName}</color>");
            _slashCommandBuilder.Add(command.Register());
            AddCommand(command.CommandName, command);
        }

        private void AddCommand(string name, IBotCommand command)
        {
            _commands[name] = command;
        }
    }
}
