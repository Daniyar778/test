# Telegram FAQ Bot

This repository contains a simple Telegram bot that answers frequently asked questions about ОСАГО.

The bot is located in the `TelegramBotFAQ` directory and targets .NET 8.0.

## Running

1. Install the [.NET SDK](https://dotnet.microsoft.com/) (version 8.0 or later).
2. Set the `TG_BOT_TOKEN` environment variable with your bot token (do **not** commit tokens to source control).
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. From the `TelegramBotFAQ` directory run:

```bash
dotnet run
```

The bot listens for messages and answers common questions about ОСАГО.
