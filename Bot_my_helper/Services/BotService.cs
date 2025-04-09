using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Bot_my_helper.Services
{
    public class BotService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UpdateHandler _updateHandler;

        public BotService(ITelegramBotClient botClient, UpdateHandler updateHandler)
        {
            _botClient = botClient;
            _updateHandler = updateHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botClient.StartReceiving(
                _updateHandler,
                cancellationToken: stoppingToken
            );

            Console.WriteLine("Бот начал принимать обновления...");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
