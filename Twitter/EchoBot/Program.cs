﻿using System;
using Bots.Twitter;

namespace EchoBot
{
    using static Consts;

    class Program
    {
        static TwitterBot bot = new TwitterBot(80, ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);

        static void Main(string[] args)
        {
            Console.WriteLine("Starting the bot...");
            bot.OnMessage += Bot_OnMessage;
            bot.StartReceivingAsync();
            bot.WaitForShutdown();
        }

        private static async void Bot_OnMessage(MessageEventArgs e)
        {
            Console.WriteLine($"{e.Message.Sender}: {e.Message.Text}");

            if (e.Message.Sender != UserId)
            {
                QuickReply qr = null;

                if (e.Message.Text == "q")
                {
                    qr = new QuickReply();
                    qr.Options.Add(new QuickReplyOption() { Label = "1", Metadata = "#1#" });
                    qr.Options.Add(new QuickReplyOption() { Label = "2", Metadata = "#2#" });
                    qr.Options.Add(new QuickReplyOption() { Label = "3", Metadata = "#3#" });
                }

                await bot.SendMessageAsync(e.Message.Sender, e.Message.Text, qr);
            }
        }
    }
}
