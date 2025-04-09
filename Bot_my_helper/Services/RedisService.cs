using Microsoft.Extensions.Logging;
using Polly;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot_my_helper.Services
{
    // Кэш для быстрого доступа к сообщениям 
    public class RedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _logger = logger;

            _logger.LogInformation("Подключение к Redis установлено!");
        }

        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var policy = Policy
           .Handle<RedisConnectionException>()
           .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
             (exception, timeSpan, attempt, context) =>
             {
                 _logger.LogWarning($"Попытка {attempt} не удалась, будет повтор через {timeSpan.TotalSeconds} секунд. Ошибка: {exception.Message}");
             });

            await policy.ExecuteAsync(async () =>
            {
                await _db.StringSetAsync(key, value);
                _logger.LogInformation($"Данные сохранены в Redis: {key}");
            });

        }
       
        public async Task<string?> GetAsync(string key)
        {
            var policy = Policy
            .Handle<RedisConnectionException>() 
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), 
                (exception, timeSpan, attempt, context) =>
                {
                    _logger.LogWarning($"Попытка {attempt} не удалась, будет повтор через {timeSpan.TotalSeconds} секунд. Ошибка: {exception.Message}");
                });

            return await policy.ExecuteAsync(async () =>
            {
                return await _db.StringGetAsync(key);
            });
        }

        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
            _logger.LogInformation($"Данные удалены из Redis: {key}");
        }
    }
}
