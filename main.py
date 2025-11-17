import discord
from discord import app_commands
from discord.ext import commands
import yt_dlp
import asyncio
import signal, sys
import os
import configparser

# =============================
# CONFIG AUTO-GENERATION
# =============================

CONFIG_PATH = "config.ini"
config = configparser.ConfigParser()

# Create config.ini if it doesn't exist
if not os.path.exists(CONFIG_PATH):
    print("[INFO] config.ini not found. Generating default config...")
    config["DISCORD"] = {
        "TOKEN": "PUT_YOUR_DISCORD_BOT_TOKEN_HERE",
        "DEFAULT_VOLUME": "10",
        "UseCookies": "False",
        "CookiesFile": "PUT_YOUR_COOKIES_TXT_PATH_HERE"
    }
    with open(CONFIG_PATH, "w") as f:
        config.write(f)
    print("[INFO] Default config.ini created. Please edit it with your bot token and cookies settings.")
    sys.exit(0)

# Load existing config
config.read(CONFIG_PATH)
try:
    TOKEN = config["DISCORD"]["TOKEN"]
    DEFAULT_VOLUME = int(config["DISCORD"].get("DEFAULT_VOLUME", 10))
    USE_COOKIES = config["DISCORD"].getboolean("UseCookies", False)
    COOKIES_FILE = config["DISCORD"].get("CookiesFile", "")
except KeyError as e:
    print(f"[ERROR] Missing key in config.ini: {e}")
    sys.exit(1)

if TOKEN == "PUT_YOUR_DISCORD_BOT_TOKEN_HERE" or not TOKEN.strip():
    print("[ERROR] You must set your Discord bot token in config.ini before running.")
    sys.exit(1)

print(f"[INFO] Loaded config.ini (volume={DEFAULT_VOLUME}%, cookies={USE_COOKIES})")

# =============================
# DISCORD BOT SETUP
# =============================
DEBUG_MODE = True
MAX_LABEL_LEN = 78  # leave room for joiner + emoji
EM = "\u2003"
WJ = "\u2060"
MUSIC = "🎵"
intents = discord.Intents.default()
intents.message_content = True
intents.voice_states = True
intents.guilds = True

bot = commands.Bot(command_prefix="!", intents=intents)
tree = bot.tree

# ---- yt-dlp Options ----
YDL_OPTS = {
    "format": "bestaudio/best",
    "noplaylist": True,
    "quiet": True,
    "default_search": "ytsearch",
}

# Apply cookie support if enabled
if USE_COOKIES and os.path.exists(COOKIES_FILE):
    YDL_OPTS["cookiefile"] = COOKIES_FILE
    print(f"[INFO] Using cookies from: {COOKIES_FILE}")
elif USE_COOKIES:
    print(f"[WARN] Cookies enabled but file not found: {COOKIES_FILE}")


FFMPEG_OPTS = {
    "before_options": "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5",
    "options": "-vn -af loudnorm=I=-16:TP=-1.5:LRA=11"
}

guild_queues = {}      # {guild_id: [song_dict, ...]}
guild_volumes = {}     # {guild_id: int}
guild_now_playing = {} # {guild_id: str}

# =============================
# EVENTS
# =============================
@bot.event
async def on_ready():
    if DEBUG_MODE:
        bot.loop.create_task(monitor_system_usage())

    print(f"Bot logged in as {bot.user}")
    try:
        synced = await tree.sync()
        print(f"Synced {len(synced)} slash commands.")
    except Exception as e:
        print(f"Error syncing commands: {e}")

# =============================
# HELPER FUNCTIONS
# =============================
def log_system_usage():
    if not DEBUG_MODE:
        return
    process = psutil.Process(os.getpid())
    mem = process.memory_info().rss / 1024**2
    cpu = process.cpu_percent(interval=0.1)
    print(f"[DEBUG] CPU: {cpu:.1f}% | RAM: {mem:.2f} MB")

async def monitor_system_usage():
    if not DEBUG_MODE:
        return
    import psutil, os
    process = psutil.Process(os.getpid())
    while True:
        mem = process.memory_info().rss / 1024**2
        cpu = process.cpu_percent(interval=None)
        print(f"[DEBUG] CPU: {cpu:.1f}% | RAM: {mem:.2f} MB", end="\r", flush=True)
        await asyncio.sleep(5)  # adjust interval to taste

def add_to_queue(guild_id, info):
    if guild_id not in guild_queues:
        guild_queues[guild_id] = []
    guild_queues[guild_id].append(info)


def get_next_song(guild_id):
    if guild_id in guild_queues and guild_queues[guild_id]:
        return guild_queues[guild_id].pop(0)
    return None


async def play_next(interaction, vc):
    """Automatically play the next song in the queue."""
    guild_id = interaction.guild.id
    next_song = get_next_song(guild_id)
    if not next_song:
        guild_now_playing.pop(guild_id, None)
        await bot.change_presence(activity=None)
        if vc.is_connected():
            await asyncio.sleep(5)
            if not vc.is_playing() and not get_next_song(guild_id):
                await vc.disconnect()
                print(f"[INFO] Disconnected from empty voice channel in guild {guild_id}")
        return

    title = next_song["title"]
    stream_url = next_song["url"]

    if guild_id not in guild_volumes:
        guild_volumes[guild_id] = DEFAULT_VOLUME
    volume = guild_volumes[guild_id] / 100.0

    print(f"[INFO] Now playing: {title} at {guild_volumes[guild_id]}% volume")
    source = discord.FFmpegPCMAudio(stream_url, **FFMPEG_OPTS)
    source = discord.PCMVolumeTransformer(source, volume)

    async def play_next_async():
        try:
            await play_next(interaction, vc)
        except Exception as e:
            print(f"[ERROR] Failed to continue playback: {e}")

    def after_playback(err):
        if err:
            print(f"[ERROR] Playback error: {err}")
        fut = asyncio.run_coroutine_threadsafe(play_next_async(), bot.loop)
        try:
            fut.result()
        except Exception as e:
            print(f"[ERROR] after_playback future: {e}")

    vc.play(source, after=after_playback)
    guild_now_playing[guild_id] = title
    await bot.change_presence(activity=discord.Game(name=f"{title}"))

    try:
        await interaction.followup.send(f"🎶 Now playing: **{title}**", ephemeral=True)
    except discord.InteractionResponded:
        # Safe silent skip — we might be auto-playing from queue
        pass
    except Exception as e:
        print(f"[WARN] Could not send now-playing message: {e}")

# =============================
# SLASH COMMANDS
# =============================
@tree.command(name="search", description="Search YouTube and choose a song to play")
@app_commands.describe(query="Search term")
async def search(interaction: discord.Interaction, query: str):
    print(f"[DEBUG] /search called by {interaction.user} with query='{query}'")

    try:
        await interaction.response.defer(thinking=True, ephemeral=True)
        print("[DEBUG] Deferred interaction response (ephemeral)")

        loop = asyncio.get_event_loop()

        def fetch_results():
            print("[DEBUG] Fetching YouTube results with yt_dlp")
            with yt_dlp.YoutubeDL({
                "quiet": True,
                "extract_flat": "in_playlist",
                "default_search": "ytsearch5"
            }) as ydl:
                return ydl.extract_info(query, download=False)

        info = await loop.run_in_executor(None, fetch_results)
        print(f"[DEBUG] yt_dlp returned: {len(info.get('entries', []))} entries")

        entries = info.get("entries", [])[:5]
        if not entries:
            print("[DEBUG] No entries found")
            await interaction.followup.send("No results found.", ephemeral=True)
            return

        class SearchView(discord.ui.View):
            def __init__(self, results):
                super().__init__(timeout=60)
                self.results = results
                print("[DEBUG] Creating horizontal SearchView buttons")
                for i, e in enumerate(results, start=1):
                    stream_link = e.get("url")
                    title = e.get("title", "Unknown title")
                    button = self.ResultButton(i, title, stream_link, self)
                    self.add_item(button)

            class ResultButton(discord.ui.Button):
                def __init__(self, index, title, stream_link, view_ref):
                    super().__init__(
                        label=f"{index} 🎵",
                        style=discord.ButtonStyle.danger
                    )
                    self.title = title
                    self.stream_link = stream_link
                    self.view_ref = view_ref
                    print(f"[DEBUG] Created button {index} for '{title}'")

                async def callback(self, interaction_btn: discord.Interaction):
                    print(f"[DEBUG] Button clicked: {self.title}")
                    try:
                        await interaction_btn.response.defer(thinking=True, ephemeral=True)
                        user = interaction_btn.user
                        if not user.voice or not user.voice.channel:
                            await interaction_btn.followup.send("Join a voice channel first.", ephemeral=True)
                            return

                        vc = interaction_btn.guild.voice_client
                        if not vc:
                            vc = await user.voice.channel.connect()

                        # Extract actual stream
                        loop = asyncio.get_event_loop()
                        def extract_stream():
                            with yt_dlp.YoutubeDL(YDL_OPTS) as ydl:
                                return ydl.extract_info(self.stream_link, download=False)

                        info = await loop.run_in_executor(None, extract_stream)
                        if "entries" in info:
                            info = info["entries"][0]
                        stream_url = info.get("url")
                        song_info = {"url": stream_url, "title": info.get("title", self.title)}

                        if vc.is_playing() or vc.is_paused() or interaction_btn.guild.id in guild_now_playing:
                            add_to_queue(interaction_btn.guild.id, song_info)
                        else:
                            add_to_queue(interaction_btn.guild.id, song_info)
                            await play_next(interaction_btn, vc)

                        # Disable all buttons for everyone (still visible to user)
                        for child in self.view_ref.children:
                            child.disabled = True
                        await interaction_btn.message.edit(view=self.view_ref)

                    except Exception as e:
                        print(f"[ERROR] Button callback failed: {e}")

        try:
            view = SearchView(entries)
            print("[DEBUG] View created successfully")

            # Build a clean, dark-red embed for results
            embed = discord.Embed(
                title=f"🎵 searched for: {query.lower()}",
                color=discord.Color.dark_red()
            )

            # Format results for embed
            desc_lines = []
            for i, e in enumerate(entries, start=1):
                title = e.get("title", "Unknown title")
                if len(title) > 47:
                    title = title[:47] + "..."
                desc_lines.append(f"{i}) {title.lower()}")

            embed.description = "```json\n" + "\n".join(desc_lines) + "\n```"

            # Send embed + horizontal buttons (ephemeral)
            await interaction.followup.send(embed=embed, view=view, ephemeral=True)
            print("[DEBUG] Sent ephemeral search results embed with buttons")

        except Exception as e:
            print(f"[ERROR] Failed to send search embed or view: {e}")
            await interaction.followup.send(f"Error building buttons: {e}", ephemeral=True)

    except Exception as e:
        print(f"[ERROR] /search failed: {e}")
        try:
            await interaction.followup.send(f"Error: {e}", ephemeral=True)
        except Exception:
            pass

@tree.command(name="play", description="Play or queue a YouTube song by URL or search term")
@app_commands.describe(url="YouTube video URL or search term")
async def play(interaction: discord.Interaction, url: str):
    """Play or queue YouTube audio."""
    user = interaction.user
    if not user.voice or not user.voice.channel:
        await interaction.response.send_message("Join a voice channel first.", ephemeral=True)
        return

    vc = interaction.guild.voice_client
    if not vc:
        vc = await user.voice.channel.connect()

    await interaction.response.defer(thinking=True, ephemeral=True)

    loop = asyncio.get_event_loop()
    def extract_info():
        with yt_dlp.YoutubeDL(YDL_OPTS) as ydl:
            return ydl.extract_info(url, download=False)

    info = await loop.run_in_executor(None, extract_info)
    # info = ydl.extract_info(url, download=False)
    if "entries" in info:
        info = info["entries"][0]
    song_info = {"url": info["url"], "title": info.get("title", "Unknown")}

    # If a song is currently playing, queue the next
    if vc.is_playing() or vc.is_paused() or interaction.guild.id in guild_now_playing:
        add_to_queue(interaction.guild.id, song_info)
        await interaction.followup.send(
            f"➕ Queued: **{song_info['title']}**",
            ephemeral=True,
        )
    else:
        add_to_queue(interaction.guild.id, song_info)
        await play_next(interaction, vc)


@tree.command(name="skip", description="Skip the current song and play the next one in queue")
async def skip(interaction: discord.Interaction):
    vc = interaction.guild.voice_client
    if not vc or not vc.is_playing():
        await interaction.response.send_message("Nothing is playing.", ephemeral=True)
        return

    vc.stop()
    await interaction.response.send_message("⏭️ Skipping...", ephemeral=True)


@tree.command(name="queue", description="Show the current music queue")
async def queue(interaction: discord.Interaction):
    """Display upcoming songs."""
    guild_id = interaction.guild.id
    queue_list = guild_queues.get(guild_id, [])
    now_playing = guild_now_playing.get(guild_id, None)

    if not now_playing and not queue_list:
        await interaction.response.send_message("📭 The queue is empty.", ephemeral=True)
        return

    msg = ""
    if now_playing:
        msg += f"🎵 **Now playing:** {now_playing}\n"
    if queue_list:
        msg += "\n📜 **Up next:**\n"
        for i, song in enumerate(queue_list[:10], start=1):
            msg += f"{i}. {song['title']}\n"
        if len(queue_list) > 10:
            msg += f"...and {len(queue_list) - 10} more.\n"

    await interaction.response.send_message(msg.strip(), ephemeral=True)


@tree.command(name="volume", description="Set playback volume (1–100)")
@app_commands.describe(vol="Volume percent between 1 and 100")
async def volume(interaction: discord.Interaction, vol: int):
    if not 1 <= vol <= 100:
        await interaction.response.send_message("Volume must be between 1 and 100.", ephemeral=True)
        return

    vc = interaction.guild.voice_client
    if not vc or not vc.source:
        await interaction.response.send_message("Nothing is playing.", ephemeral=True)
        return

    vc.source.volume = vol / 100.0
    guild_volumes[interaction.guild.id] = vol
    await interaction.response.send_message(f"🔊 Volume set to **{vol}%**", ephemeral=True)

# ===================================
# GRACEFUL SHUTDOWN (YT-DLP / FFMPEG)
# ===================================
def shutdown(sig, frame):
    for vc in bot.voice_clients:
        vc.stop()
        asyncio.run_coroutine_threadsafe(vc.disconnect(), bot.loop)
    print("\n[INFO] Clean shutdown.")
    sys.exit(0)

signal.signal(signal.SIGINT, shutdown)

# =============================
# RUN
# =============================
bot.run(TOKEN)
