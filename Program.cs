using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace VoiceRecognationBot
{
    class Program
    {
        private static IConfiguration configuration;
        private static ITelegramBotClient botClient;
        private static string apiAssemblyAI;
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
            apiAssemblyAI = configuration["AssemblyAIApiKey"];
            filePath = configuration["VoiceMessageFilePath"];

            botClient = new TelegramBotClient(apiTelegram);
        }
        private static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            if (update.Message.Text != null)
            {
                await SendMessage(update.Message.Chat, "This bot will help translate your voice into text, just send a voice message to get started.");
            }

            if (update.Message.Voice != null)
            {
                try
                {
                    await DownloadVoiceMessage(update.Message.Voice.FileId);

                    string uploadedFileUrl = await UploadFileAsync(apiAssemblyAI, filePath);
                    if (uploadedFileUrl == null)
                    {
                        Console.WriteLine("Failed to upload file.");
                        return;
                    }
                    Message message = await botClient.SendTextMessageAsync(update.Message.Chat, "Transcription in progress...", cancellationToken: token);
                    int messageId = message.MessageId;

                    string transcript = await GetTranscriptAsync(apiAssemblyAI, uploadedFileUrl);

                    await botClient.DeleteMessageAsync(update.Message.Chat, messageId, cancellationToken: token);
                    await SendMessage(update.Message.Chat, transcript);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    await SendMessage(update.Message.Chat, "Error on the server side.");
                }
            }
        }
        private static async Task DownloadVoiceMessage(string fileId)
        {
            Telegram.Bot.Types.File voiceMessage = await botClient.GetFileAsync(fileId);

            using FileStream fileStream = new(filePath, FileMode.Create);
            await botClient.DownloadFileAsync(voiceMessage.FilePath, fileStream);
        }
        private static async Task SendMessage(Chat chatId, string message)
        {
            await botClient.SendTextMessageAsync(
              chatId: chatId,
              text: message
            );
        }
        private static async Task<string> UploadFileAsync(string apiAssemblyAI, string path)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(apiAssemblyAI);

            using ByteArrayContent fileContent = new(System.IO.File.ReadAllBytes(path));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.assemblyai.com/v2/upload", fileContent);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);
                return json["upload_url"].ToString();
            }
            else
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }
        }
        public static async Task<string> GetTranscriptAsync(string apiAssemblyAI, string audioUrl)
        {
            Dictionary<string, string> data = new()
            {
                { "audio_url", audioUrl }
            };

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("authorization", apiAssemblyAI);
            StringContent content = new(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("https://api.assemblyai.com/v2/transcript", content);
            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

            string transcriptId = responseJson.id;
            string pollingEndpoint = $"https://api.assemblyai.com/v2/transcript/{transcriptId}";

            while (true)
            {
                HttpResponseMessage pollingResponse = await client.GetAsync(pollingEndpoint);
                string pollingResponseContent = await pollingResponse.Content.ReadAsStringAsync();
                dynamic pollingResponseJson = JsonConvert.DeserializeObject<dynamic>(pollingResponseContent);

                if (pollingResponseJson.status == "completed")
                {
                    return pollingResponseJson.text;
                }
                else if (pollingResponseJson.status == "error")
                {
                    throw new Exception($"Transcription failed: {pollingResponseJson.error}");
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
        private static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
