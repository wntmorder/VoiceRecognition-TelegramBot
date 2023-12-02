# Voice Recognition Telegram Bot

This Telegram Bot leverages the power of [Assembly AI](https://www.assemblyai.com/) to convert your voice messages into text. Say goodbye to manual transcription; this bot does it for you, making it a valuable tool for anyone using Telegram.
**Documentation:**
- [Assembly AI Documentation](https://www.assemblyai.com/docs/)
- [Telegram Bot API Documentation](https://telegrambots.github.io/book/index.html)


## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Free Voice Transcription**: This bot offers voice-to-text transcription for free, even if you're not using Telegram Pro.
- **High Accuracy**: Powered by Assembly AI, the bot provides accurate voice recognition results.
- **Easy Integration**: Seamlessly add this bot to your Telegram contacts and start transcribing voice messages instantly.

## Installation

To use the Voice Recognition Telegram Bot, follow these steps:

1. Get Assembly AI and Telegram Bot API Keys:
   - Visit [Assembly AI](https://www.assemblyai.com/) and sign up for an account.
   - Get your Assembly AI API key.
   - Create a new bot on [Telegram](https://t.me/BotFather) and get the API token.

2. Clone the repository to your local machine:
   ```shell
   git clone https://github.com/wntmorder/VoiceRecognition-TelegramBot.git
   ```

3. Navigate to the project directory:
   ```shell
   cd VoiceRecognition-TelegramBot
   ```

4. Create an appsettings.json file and add your Telegram Bot API Token, AssemblyAI API Key and VoiceMessage file path:
    ```shell
    {
      "AssemblyAIApiKey": "YOUR_ASSEMBLYAI_API_KEY",
      "TelegramApiKey": "YOUR_TELEGRAM_API_KEY",
      "VoiceMessageFilePath": "YOUR_FILE_PATH"
    }
    ```

5. Add the necessary dependencies:
   - Ensure you have .NET Core installed on your system.
   - Restore the project dependencies using the following command:
    ```shell
    dotnet restore
    ```

6. Run the bot:
   ```shell
   dotnet run
   ```

## Usage

1. Start a chat with the Voice Recognition Telegram Bot on Telegram.
2. Send a voice message to the bot.
3. Wait for the bot to transcribe the voice message.
4. Receive the transcribed text from the bot as a reply.

## Contributing

Contributions to this project are welcome. If you have any improvements or feature suggestions, please feel free to open an issue or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE). You are free to use, modify, and distribute this software as per the terms of the license.

---

Give it a try and make voice message transcription a breeze with the Voice Recognition Telegram Bot powered by Assembly AI!
