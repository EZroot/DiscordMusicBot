#  <img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/main/DiscordMusicBot/Imgs/2b1b1cb5-2446-46d7-848e-e9c418b5de91.webp" alt="drawing" width="64"/>  Discord Music Bot   <img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/main/DiscordMusicBot/Imgs/2b1b1cb5-2446-46d7-848e-e9c418b5de91.webp" alt="drawing" width="64"/>

- A non-intrusive Discord Music Bot for simple self-hosting.
- Very easy to setup!

## ✨ Latest Updates
- **Release v0.3**
  
## 🚧 Prerequisites (Included in Releases (Exception: Ffmpeg))
- **Yt-dlp**
- **Ffmpeg**
- **libsodium.dll & opus.dll**

> **Note:** Linux/Window Builds - Ensure to include libsodium, opus, and yt-dlp program in your build folder. (Install ffmpeg normally, so you can use it in the terminal from anywhere)

## 🛠 Installation
1. **Extract content in publish.zip**
2. **Download libsodium.dll, opus.dll, and yt-dlp from github releases, drag those in the extracted publish folder**
3. **Run DiscordModBot once to generate** the `config.json`.
4. **Set your Discord API key** in `config.json`.
5. **Set your Guild (Server) ID** in `config.json` to register slash commands.
6. **Run the bot!**

## 🌐 Bot Commands Workflow
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

## 📝 Support 

If you encounter any issues with the bot, please consult the documentation or search online for solutions.

## 📸 Screenshots 
<img src="https://raw.githubusercontent.com/EZroot/DiscordMusicBot/refs/heads/main/DiscordMusicBot/Imgs/screenshot_01.png" alt="drawing"/> 
