using Xunit;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;

namespace UserService.IntegrationTests
{
	public class UserServiceIntegrationTests : IDisposable
	{
		private readonly IMongoDatabase _database;
		private readonly IMongoCollection<User> _userCollection;
		private readonly UserService.Services.UserService _userService;

		public UserServiceIntegrationTests()
		{
			var config = new ConfigurationBuilder()
						.AddJsonFile("appsettings.json")
						.Build();

			var client = new MongoClient(config.GetConnectionString("MongoDb"));
			_database = client.GetDatabase("UserServiceTestDb");

			// Clear the collection before each test
			_userCollection = _database.GetCollection<User>("Users");
			_userCollection.DeleteMany(FilterDefinition<User>.Empty);

			// Setup the real UserRepository and UserService
			var userRepository = new UserRepository(config, "UserServiceTestDb");
			_userService = new UserService.Services.UserService(userRepository);
		}

		[Fact]
		public void Authenticate_WithValidCredentials_ReturnsUser()
		{
			// Arrange
			var username = "testuser";
			var password = "password123";
			var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

			var user = new User { Username = username, PasswordHash = hashedPassword };
			_userCollection.InsertOne(user);

			// Act
			var result = _userService.Authenticate(username, password);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(username, result.Username);
		}

		[Fact]
		public void Authenticate_WithInvalidCredentials_ReturnsNull()
		{
			// Arrange
			var username = "testuser";
			var password = "wrongpassword";

			var user = new User { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
			_userCollection.InsertOne(user);

			// Act
			var result = _userService.Authenticate(username, password);

			// Assert
			Assert.Null(result);
		}

		public void Dispose()
		{
			// Clean up the database after each test
			_database.DropCollection("Users");
		}
	}
}
