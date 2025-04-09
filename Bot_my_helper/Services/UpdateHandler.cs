using Bot_my_helper.DB.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot_my_helper.Services
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly AppDbContext _dbContext;
        private readonly RedisService _redisService;
        private readonly IMessageSender _messageSender;
        private readonly DbService _dbService;
        private readonly ILogger<UpdateHandler> _logger;


        public UpdateHandler(ITelegramBotClient botClient,AppDbContext dbContext, RedisService redisService, IMessageSender messageSender, DbService dbService, ILogger<UpdateHandler> logger)
        {
            _botClient = botClient;
            _dbContext = dbContext;
            _redisService = redisService;
            _messageSender = messageSender;
            _dbService = dbService;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandleMessageAsync(update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery);
            }

            await Task.CompletedTask;
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            _logger.LogError($"Ошибка: {exception.Message}");

            await _messageSender.SendLogToTelegram($"Ошибка: {exception.Message}");
            

            await Task.CompletedTask;
        }

        // Обрабатываем: входящие сообщение, кэш, бд, и ответ пользователю 
        private async Task HandleMessageAsync(Message message)
        {

            // test logger 
            await LogMessageAsync(message);

            // test Redis get date
            await GetLastMessageFromRedisAsync(message);

            // test Redis set date
            await SaveMessageToRedisAsync(message);

            // test Db
            await AddAndSaveMessageToDatabaseAsync(message);

            //test message

            try
            {
                if (message.Text == "/start")
                {
                    await _botClient.SendMessage(message.Chat.Id, "Text test");
                }
                else if (message.Text == "/testError")
                {
                    int[] ints = [1];
                    await _botClient.SendMessage(message.Chat.Id, "Text test " + ints[10]);
                }
                else
                {
                    InlineKeyboardButton button = new InlineKeyboardButton("Button1", "button_click");
                    await _botClient.SendMessage(message.Chat.Id, "Text test 2", replyMarkup: button);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Ошибка при обработки сообщения: {ex.Message}");
                await _messageSender.SendLogToTelegram($"Ошибка при обработки сообщения: {ex.Message}");
            }
        }

        private async Task LogMessageAsync(Message message)
        {
            try
            {
                string id = $"{message.Chat.Id}";
                string userName = $"{message.Chat.Username}";
                string textForLog = $"{userName}/{id}: {message.Text}";
                _logger.LogInformation(textForLog);
                await _messageSender.SendLogToTelegram(textForLog);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при логировании сообщения: {ex.Message}");
                await _messageSender.SendLogToTelegram($"Ошибка логирования");
            }
        }
        private async Task GetLastMessageFromRedisAsync(Message message)
        {
            try
            {
                string userKey = $"user:{message.Chat.Id}";
                string? lastMessage = await _redisService.GetAsync(userKey);
                if (lastMessage != null)
                {
                    await _botClient.SendMessage(message.Chat.Id, $"Ваше предыдущее сообщение: {lastMessage}");
                }
                else
                {
                    await _botClient.SendMessage(message.Chat.Id, "В Redis нет сохраненных сообщений.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при получении данных из Redis: {ex.Message}");
                await _messageSender.SendLogToTelegram($"Не удалось получить данные из Redis.");
            }
        }
        private async Task SaveMessageToRedisAsync(Message message)
        {
            try
            {
                string userKey = $"user:{message.Chat.Id}";
                await _redisService.SetAsync(userKey, message.Text ?? "");
                await _botClient.SendMessage(message.Chat.Id, "Сообщение сохранено в Redis!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при сохранении в Redis: {ex.Message}");
                await _messageSender.SendLogToTelegram($"Не удалось сохранить данные из Redis.");
            }
        }
        private async Task AddAndSaveMessageToDatabaseAsync(Message message)
        {
            try
            {
               await _dbService.AddUserAsync(message);

               await _dbService.SaveMessageToDatabaseAsync(message);
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при сохранении в базу данных: {ex.Message}");
                await _messageSender.SendLogToTelegram($"Не удалось сохранить данные из базе данных.");
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Data == "button_click")
            {
                await _botClient.SendMessage(callbackQuery.Message.Chat.Id, "Ты нажал на кнопку!");
            }
        }
    }
}
