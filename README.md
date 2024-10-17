#  <img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/main/DiscordMusicBot/Imgs/2b1b1cb5-2446-46d7-848e-e9c418b5de91.webp" alt="drawing" width="64"/>  Discord Music Bot   <img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/main/DiscordMusicBot/Imgs/2b1b1cb5-2446-46d7-848e-e9c418b5de91.webp" alt="drawing" width="64"/>

- A non-intrusive Discord music bot for self-hosting.
- Very easy to setup!

## Table of Contents

- [Latest Updates ✨](#latest-updates-)
- [Prerequisites 🚧](#prerequisites-)
- [Installation 📝](#installation-)
  - [Docker 🐳](#docker-)
  - [Non-Docker 💪](#non-docker-)
- [Support 📝](#support-)
- [Screenshots 📸](#screenshots-)
  
## Latest Updates

- **Release v0.2**
- **Slash Commands:**
  - `/play [url]` - Play an audio stream from YouTube by URL
  - `/search [keyword]` - Return and display search results from YouTube by keywords
  - `/volume [number]` - Change the volume from 0 - 100
  - `/skip` - Skip the current song
  - `/queue` - Display current song queue
  - `/history` - Display recent song history
  - `/mostplayed` - Display most played songs
  - `/leave` - Leave voice

## Prerequisites (Included in Releases)
- **YTDLP**
- **FFMPEG**
- **libsodium.dll & opus.dll**

> **Note:** If building for Linux, make sure to get the proper ytdlp to include in your build folder.

## Installation 📝
1. **Extract content in Release.zip**
2. **Run DiscordModBot once to generate** the `config.json`.
3. **Set your Discord API key** in `config.json`.
4. **Set your Guild (Server) ID** in `config.json` to register slash commands.
5. **Run the bot!**

## Bot Commands Workflow 🌐
```mermaid
flowchart TB
    subgraph Session Commands
        M["/leave"] --> N[Leave voice channel]
    end
    subgraph Info Commands
        G["/queue"] --> H[Display the songs queued]
        I["/mostplayed"] --> J[Display most played songs]
        K["/history"] --> L[Display recent song history]
    end
		subgraph Playback Commands
        A["/play <url>"] --> B[Play song based on YouTube URL]
        C["/search <keyword>"] --> D[Search and display top YouTube results]
        E["/skip"] --> F[Skip the current song]
    end
```

Support 📝

If you encounter any issues with the bot, please consult the documentation or search online for solutions.
Screenshots 📸
<img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/refs/heads/main/DiscordMusicBot/Imgs/screenshot_01.png" alt="drawing"/> 
