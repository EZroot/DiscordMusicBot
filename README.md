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

## Bot Workflow 🌐

```mermaid
flowchart LR
    A[User Sends Command] --> B{Bot Receives Command}
    B --> C[Play an audio stream from YouTube using /play]
    B --> D[Return search results from YouTube using /search]
    B --> E[Change volume with /volume]
    B --> F[Skip the current song with /skip]
    B --> G[Display the current song queue with /queue]
    B --> H[Display recent song history with /history]
    B --> I[Display most played songs with /mostplayed]
    B --> J[Leave the voice channel with /leave]
```
Support 📝

If you encounter any issues with the bot, please consult the documentation or search online for solutions.
Screenshots 📸
<img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/refs/heads/main/DiscordMusicBot/Imgs/screenshot_01.png" alt="drawing"/> 
