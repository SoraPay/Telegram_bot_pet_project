using Bot_my_helper.DB.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Bot_my_helper.Services
{
    public class DbService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DbService> _logger;
        private readonly IMessageSender _messageSender;

        public DbService(AppDbContext dbContext, IMessageSender messageSender, ILogger<DbService> logger)
        {
            _dbContext = dbContext;
            _messageSender = messageSender;
            _logger = logger;
        }
        public async Task SaveMessageToDatabaseAsync(Telegram.Bot.Types.Message message)
        {
            // С помощью policy повторяем попытки сохранения при сбоях. Настраиваем количество
            var policy = Policy
                .Handle<DbUpdateException>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (exception, timeSpan, attempt, context) =>
                    {
                        _logger.LogWarning($"Попытка {attempt} не удалась, будет повтор через {timeSpan.TotalSeconds} секунд. Ошибка: {exception.Message}");
                    });

            await policy.ExecuteAsync(async () =>
            {
                var userId = await _dbContext.Users
                  .Where(u => u.IdTelegram == message.From.Id)
                  .Select(u => u.Id)
                  .FirstOrDefaultAsync();


                var newMessage = new DB.Model.Message
                {
                    MessageSend = message.Text,
                    DateSending = DateTime.UtcNow,
                    UserId = userId
                };

                _dbContext.Messages.Add(newMessage);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Данные сохранены в базу данных");
                await _messageSender.SendLogToTelegram($"Данные сохранены в базу данных");

            });
        }

        public async Task AddUserAsync(Telegram.Bot.Types.Message message)
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (exception, timeSpan, attempt, context) =>
                    {
                        _logger.LogWarning($"Попытка {attempt}: Ошибка БД ({exception.Message}). Повтор через {timeSpan.TotalSeconds} сек.");
                    });

            await policy.ExecuteAsync(async () =>
            {

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.IdTelegram == message.From.Id);

                if (user == null)
                {
                    user = new DB.Model.User
                    {
                        IdTelegram = message.From?.Id ?? 0,
                        UserName = message.From?.Username,
                        DateFirstTouch = DateTime.UtcNow
                    };

                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Данные сохранены в базу данных");
                }
            });



        }
    }
}
