using DiscordMusicBot2.Data;

namespace DiscordMusicBot2.FileManagement
{
    internal static class FileLoader
    {

        public static ConfigData? LoadConfig(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Debug.Log($"<color=red>Config file not found:</color> {path}");
                Debug.Log($"Generating a default config.ini: {path}");
                //Generatring a default config.ini
                System.IO.File.WriteAllText(path, "##bot token is for api key, guild id is for registering slash commands\n" +
                    "bottoken:paste_token_here\n" +
                    "guildid:paste_server_id_here");
                return null;
            }

            try
            {
                var configData = System.IO.File.ReadAllText(path);
                var botToken = configData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("bottoken:"))?.Substring("bottoken:".Length).Trim();
                var guildId = configData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("guildid:"))?.Substring("guildid:".Length).Trim();

                if (!string.IsNullOrEmpty(botToken) && !string.IsNullOrEmpty(guildId))
                    return new ConfigData(botToken,guildId);
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading config file: {ex.Message}", ex);
            }
        }
    }
}
