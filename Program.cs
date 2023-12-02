using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Threading;

namespace VoiceRecognationBot
{
    class Program
    {
        private static IConfiguration configuration;
        private static ITelegramBotClient botClient;
        private static HttpClient httpClient;
        private static string filePath;

        public static async Task Main(string[] args)
        {
            configuration = BuildConfiguration();

            Initialize();

            botClient.StartReceiving(Update, Error);
            Console.WriteLine("Bot started.");
            await Task.Delay(-1);
        }
        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .Add(new JsonConfigurationSource { Path = "appsettings.json", Optional = false, ReloadOnChange = true })
                .Build();
        }
        private static void Initialize()
        {
            string apiTelegram = configuration["TelegramApiKey"];
            string apiAssemblyAI = configuration["AssemblyAIApiKey"];
            filePath = configuration["VoiceMessageFilePath"];

            botClient = new TelegramBotClient(apiTelegram);
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiAssemblyAI);
        }
        private static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            if (update.Message.Text != null)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat, "Welcome to our Voice Recognition Bot! \nSimply send us a voice message, and we'll magically transform it into text for you. \nGive it a try now!", cancellationToken: token);
            }

            if (update.Message.Voice != null)
            {
                try
                {
                    Message message = await botClient.SendTextMessageAsync(update.Message.Chat, text: "Wait a moment, please! Transcription in progress...", replyToMessageId: update.Message.MessageId, cancellationToken: token);

                    await DownloadVoiceMessage(update.Message.Voice.FileId);

                    string uploadUrl = await UploadFileAsync(filePath, httpClient);
                    if (uploadUrl == null)
                    {
                        Console.WriteLine("Failed to upload file.");
                        return;
                    }

                    Transcript transcript = await CreateTranscriptAsync(uploadUrl, httpClient);
                    transcript = await WaitForTranscriptToProcess(transcript, httpClient);

                    await botClient.DeleteMessageAsync(update.Message.Chat, message.MessageId, cancellationToken: token);
                    await botClient.SendTextMessageAsync(update.Message.Chat, transcript.Text, cancellationToken: token);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    await botClient.SendTextMessageAsync(update.Message.Chat, "Error on the server side.", cancellationToken: token);
                }
            }
        }
        private static async Task DownloadVoiceMessage(string fileId)
        {
            Telegram.Bot.Types.File voiceMessage = await botClient.GetFileAsync(fileId);

            using FileStream fileStream = new(filePath, FileMode.Create);
            await botClient.DownloadFileAsync(voiceMessage.FilePath, fileStream);
        }
        private static async Task<string> UploadFileAsync(string filePath, HttpClient httpClient)
        {
            using FileStream fileStream = System.IO.File.OpenRead(filePath);
            using StreamContent fileContent = new(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using HttpResponseMessage response = await httpClient.PostAsync("https://api.assemblyai.com/v2/upload", fileContent);
            response.EnsureSuccessStatusCode();
            JsonDocument jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return jsonDoc.RootElement.GetProperty("upload_url").GetString();
        }
        private static async Task<Transcript> CreateTranscriptAsync(string audioUrl, HttpClient httpClient)
        {
            StringContent content = new(JsonSerializer.Serialize(new { audio_url = audioUrl }), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Transcript>();
        }
        private static async Task<Transcript> WaitForTranscriptToProcess(Transcript transcript, HttpClient httpClient)
        {
            string pollingEndpoint = $"https://api.assemblyai.com/v2/transcript/{transcript.Id}";

            while (true)
            {
                var pollingResponse = await httpClient.GetAsync(pollingEndpoint);
                transcript = await pollingResponse.Content.ReadFromJsonAsync<Transcript>();
                switch (transcript.Status)
                {
                    case "processing":
                    case "queued":
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        break;
                    case "completed":
                        return transcript;
                    case "error":
                        throw new Exception($"Transcription failed: {transcript.Error}");
                    default:
                        throw new Exception("This code shouldn't be reachable.");
                }
            }
        }
        private static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
