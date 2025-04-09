using Bot_my_helper.DB.Data;
using Bot_my_helper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Bot_my_helper.DB.Model;

namespace TelegramBot.Tests
{
    // Для проверки сохранения сообщений и добавление пользователей 
    public class DbServiceTests
    {
        private readonly DbService _dbService;
        private readonly AppDbContext _dbContext;
        private readonly Mock<ILogger<DbService>> _loggerMock;
        private readonly Mock<IMessageSender> _messageSenderMock;

        public DbServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _dbContext = new AppDbContext(options);

            _loggerMock = new Mock<ILogger<DbService>>();
            _messageSenderMock = new Mock<IMessageSender>();

            _dbService = new DbService(_dbContext, _messageSenderMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task SaveMessageToDatabaseAsync_UserExists_MessageSavedSuccessfully()
        {
            var user = new Bot_my_helper.DB.Model.User
            {
                IdTelegram = 12345,
                UserName = "TestUser",
                DateFirstTouch = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();

            var message = new Telegram.Bot.Types.Message
            {
                Text = "Hello, world!",
                From = new Telegram.Bot.Types.User
                {
                    Id = 12345,
                    Username = "TestUser"
                }
            };

           
            await _dbService.SaveMessageToDatabaseAsync(message);

           
            var savedMessage = await _dbContext.Messages.FirstOrDefaultAsync(m => m.MessageSend == "Hello, world!");
            Assert.NotNull(savedMessage);
            Assert.Equal(user.Id, savedMessage.UserId);
            Assert.Equal(message.Text, savedMessage.MessageSend);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
            _messageSenderMock.Verify(x => x.SendLogToTelegram("Данные сохранены в базу данных"), Times.Once());
        }

        [Fact]
        public async Task AddUserAsync_UserDoesNotExist_AddsNewUser()
        {
            
            var message = new Telegram.Bot.Types.Message
            {
                From = new Telegram.Bot.Types.User 
                {
                    Id = 67890, Username = "NewUser" 
                }
            };

           
            await _dbService.AddUserAsync(message);

           
            var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdTelegram == 67890);
            Assert.NotNull(savedUser);
            Assert.Equal("NewUser", savedUser.UserName);
            Assert.Equal(67890, savedUser.IdTelegram);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [Fact]
        public async Task AddUserAsync_UserAlreadyExists_DoesNotAddDuplicate()
        {
        
            var user = new Bot_my_helper.DB.Model.User
            {
                IdTelegram = 11111,
                UserName = "ExistingUser",
                DateFirstTouch = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var message = new Telegram.Bot.Types.Message
            {
                From = new Telegram.Bot.Types.User 
                { 
                    Id = 11111,
                    Username = "ExistingUser"
                }
            };

           
            await _dbService.AddUserAsync(message);

            
            var users = await _dbContext.Users.Where(u => u.IdTelegram == 11111).ToListAsync();
            Assert.Single(users);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never());
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
