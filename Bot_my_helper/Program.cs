using Bot_my_helper.DB.Data;
using Bot_my_helper.DB.Model;
//using Bot_my_helper.Repositories;
using Bot_my_helper.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis;
using System.Threading.Tasks;
using Telegram.Bot;


namespace Bot_my_helper
{
    class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal(e.ExceptionObject as Exception, "Необработанное исключение в приложении.");

            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Log.Fatal(e.Exception, "Необработанное исключение в задаче.");
                e.SetObserved();
            };

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                // Настрйока хостинга и di для телеграм бота с поддержкой бд, redis и логироания.
                var builder = Host.CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
                              .AddUserSecrets<Program>() // for localHost
                              .AddEnvironmentVariables(); // for server
                    })
                    .ConfigureServices((context, services) =>
                    {
                        var config = context.Configuration;
                        string? token = config["TelegramBot:ApiToken"];
                        if (string.IsNullOrEmpty(token))
                        {
                            throw new InvalidOperationException("Ошибка: API Token бота не найден в конфигурации");
                        }

                        services.AddHostedService<BotService>();


                        services.AddLogging(loggingBuilder =>
                        {
                            loggingBuilder.ClearProviders();
                            loggingBuilder.AddSerilog();
                        });

                        services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(token));
                        services.AddSingleton<IMessageSender, MessageSender>();
                        services.AddSingleton<UpdateHandler>();

                        string? stringConnectionDB = config["ConnectionStrings:DefaultConnection"];
                        if (string.IsNullOrEmpty(stringConnectionDB))
                        {
                            throw new InvalidOperationException("Ошибка: строка подключения к базе данных не найдена в конфигурации");
                        }

                        services.AddDbContext<AppDbContext>(options =>
                            options.UseMySql(stringConnectionDB, ServerVersion.AutoDetect(stringConnectionDB)));

                        services.AddScoped<DbService>();

                        services.AddSingleton(provider =>
                        {
                            string? stringConnectionRedis = config["ConnectionStringsRedis:DefaultConnection"];
                            var logger = provider.GetRequiredService<ILogger<RedisService>>();
                            IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(stringConnectionRedis);
                            return new RedisService(redis, logger);
                        });

                    });

                var host = builder.Build();

                using (var scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    try
                    {
                        Log.Information("Применение миграций к базе данных...");
                        dbContext.Database.Migrate();
                        Log.Information("Миграции успешно применены.");
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Ошибка при применении миграций к базе данных.");
                        throw;
                    }
                }

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Ошибка при запуске приложения.");
            }
            finally
            {
                Log.CloseAndFlush();
            }


        }


    }


}


