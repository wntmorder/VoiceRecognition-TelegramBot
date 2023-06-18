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

namespace VoiceRecognationBot
{
    class Program
    {
        private static ITelegramBotClient botClient = new TelegramBotClient("");    // Your Telegram API Token 
        private static string apiToken = "";                                        // Your Assembly-AI API Token 
        private static string filePath = @"";                                       // Path to the audio file

        public static async Task Main(string[] args)
        {
            botClient.StartReceiving(Update, Error);
            Thread.Sleep(int.MaxValue);
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

                    var uploadedFileUrl = await UploadFileAsync(apiToken, filePath);
                    if (uploadedFileUrl == null)
                    {
                        Console.WriteLine("Failed to upload file.");
                        return;
                    }
                    var message = await botClient.SendTextMessageAsync(update.Message.Chat, "Transcription in progress...");
                    var messageId = message.MessageId;

                    var transcript = await GetTranscriptAsync(apiToken, uploadedFileUrl);

                    await botClient.DeleteMessageAsync(update.Message.Chat, messageId);
                    await SendMessage(update.Message.Chat, transcript);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }
        private static async Task DownloadVoiceMessage(string fileId)
        {
            var voiceMessage = await botClient.GetFileAsync(fileId);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await botClient.DownloadFileAsync(voiceMessage.FilePath, fileStream);
            }
        }
        private static async Task SendMessage(Chat chatId, string message)
        {
            await botClient.SendTextMessageAsync(
              chatId: chatId,
              text: message
            );
        }
        private static async Task<string> UploadFileAsync(string apiToken, string path)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(apiToken);

            using var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(path));
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
                var json = JObject.Parse(responseBody);
                return json["upload_url"].ToString();
            }
            else
            {
                Console.Error.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }
        }
        public static async Task<string> GetTranscriptAsync(string apiToken, string audioUrl)
        {
            var data = new Dictionary<string, string>()
            {
                { "audio_url", audioUrl }
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("authorization", apiToken);
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("https://api.assemblyai.com/v2/transcript", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

                string transcriptId = responseJson.id;
                string pollingEndpoint = $"https://api.assemblyai.com/v2/transcript/{transcriptId}";

                while (true)
                {
                    var pollingResponse = await client.GetAsync(pollingEndpoint);
                    var pollingResponseContent = await pollingResponse.Content.ReadAsStringAsync();
                    var pollingResponseJson = JsonConvert.DeserializeObject<dynamic>(pollingResponseContent);

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
        }
        private static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}