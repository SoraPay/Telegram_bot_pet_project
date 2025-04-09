using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Bot_my_helper.Services
{
    public class MessageSender : IMessageSender
    {
        private readonly ITelegramBotClient _botClient;
        private readonly long _idChat;

        public MessageSender(ITelegramBotClient botClient, IConfiguration config)
        {
            _botClient = botClient;
            _idChat = long.Parse(config["TelegramBot:LogChatId"] ?? "0");
        }

        public async Task SendLogToTelegram(string text)
        {

            await _botClient.SendMessage(_idChat, text);

        }
    }
}
