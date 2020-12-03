using AdvertDetector;
using Discord;
using Discord.WebSocket;
using Microsoft.ML;
using SentimentAnalysisConsoleApp;
using SentimentAnalysisConsoleApp.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AdvertDetector
{
    public static class MyStringExtensions
    {
        public static string RemoveAt(this string s, int index)
        {
            return s.Remove(index, 1);
        }
    }

    class DiscordBot
    {
        private const string ownerId = "<your discord id>";
        private const string channelId = "<channel id>";
        private const string token = "<bot token>";

        private readonly DiscordSocketClient _client;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private Random rnd;
        public Detector AdvertDetector { get; }
        private Dictionary<ulong, string> Messages = new Dictionary<ulong, string>();

        public DiscordBot(Detector advertDetector)
        {
            _client = new DiscordSocketClient();
            rnd = new Random();

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.ReactionAdded += _client_ReactionAdded;
            _client.MessageReceived += MessageReceivedAsync;
            MainAsync().GetAwaiter().GetResult();
            AdvertDetector = advertDetector;
        }

        private Task _client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (arg3.UserId.ToString() == ownerId && Messages.ContainsKey(arg1.Id))
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(Detector.DataPath, true);
                string msg = Messages[arg1.Id];
                List<string> messagesMutated = new List<string>();
                messagesMutated.Add(msg);
                for (var k = 0; k < 5; k++)
                {
                    string tmpMessage = msg;
                    for (var i = 0; i < msg.Length / 5; i++)
                    {
                        tmpMessage = tmpMessage.RemoveAt(rnd.Next(0, tmpMessage.Length));
                    }
                    messagesMutated.Add(tmpMessage);
                    tmpMessage = msg;
                    for (var i = 0; i < 1; i++)
                    {
                        tmpMessage = tmpMessage.RemoveAt(rnd.Next(0, tmpMessage.Length));
                    }
                    messagesMutated.Add(tmpMessage);
                }

                if (arg3.Emote.Name == "👍")
                {
                    foreach (var item in messagesMutated.Distinct())
                    {
                        if (item.Length > 3)
                        {
                            file.WriteLine($"1\t{item}");
                        }
                    }
                }
                if (arg3.Emote.Name == "👎")
                {
                    foreach (var item in messagesMutated.Distinct())
                    {
                        if (item.Length > 3)
                        {
                            file.WriteLine($"0\t{item}");
                        }
                    }
                }

                file.Flush();
                file.Close();
            }
            return Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block the program until it is closed.
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        string StringBuilderStrings(IEnumerable<char> charSequence)
        {
            var sb = new StringBuilder();
            foreach (var c in charSequence)
            {
                sb.Append(c.ToString());
            }
            return sb.ToString();
        }

        private static readonly Encoding Utf8Encoder = Encoding.GetEncoding(
            "UTF-8",
            new EncoderReplacementFallback(string.Empty),
            new DecoderExceptionFallback()
        );

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            string messageContent = message.Content;

            messageContent = Utf8Encoder.GetString(Utf8Encoder.GetBytes(messageContent));
            if (message.Author.Id.ToString() == ownerId)
            {
                if (messageContent == "trenuj")
                {
                    await message.Channel.SendMessageAsync("Trenuje...");
                    AdvertDetector.Learn();
                    await message.Channel.SendMessageAsync("Wytrenowany");
                    AdvertDetector.Load();
                    return;
                }
            }

            if (message.Channel.Id.ToString() == channelId)
            {
                if (!string.IsNullOrWhiteSpace(messageContent) && messageContent.Length > 3)
                {
                    string msg = messageContent.Replace("\n", " ").Replace("\r", " ");

                    Messages[message.Id] = msg;
                    SentimentPrediction resultprediction = AdvertDetector.Predict(msg);
                    string text = $"{(resultprediction.Probability * 100).ToString("0.00")}%: {StringBuilderStrings(msg.Take(30))}...";
                    await message.Channel.SendMessageAsync(text);
                }
            }
        }
    }
}