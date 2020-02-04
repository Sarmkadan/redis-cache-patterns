#nullable enable

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Unit tests for <see cref="UserService"/> that verify caching behavior, validation, and repository interactions.
/// Tests cover scenarios for user retrieval, creation, update, deactivation, and deletion with proper cache invalidation.
/// </summary>
public class UserServiceTests
{
	/// <summary>
	/// Mock repository for testing user data access.
	/// </summary>
	private readonly Mock<IUserRepository> _mockRepo = new();

	/// <summary>
	/// Mock cache service for testing caching behavior and invalidation.
	/// </summary>
	private readonly Mock<ICacheService> _mockCache = new();

	/// <summary>
	/// Mock logger for verifying logging behavior.
	/// </summary>
	private readonly Mock<ILogger<UserService>> _mockLogger = new();

	/// <summary>
	/// System under test - the UserService instance being tested.
	/// </summary>
	private readonly UserService _sut;

	/// <summary>
	/// Initializes a new instance of the <see cref="UserServiceTests"/> class.
	/// Sets up mock dependencies and creates the system under test.
	/// </summary>
	public UserServiceTests()
	{
		_sut = new UserService(_mockRepo.Object, _mockCache.Object, _mockLogger.Object);
	}

	/// <summary>
	/// Creates a test user with the specified parameters.
	/// </summary>
	/// <param name="id">The user ID. Defaults to 1.</param>
	/// <param name="username">The username. Defaults to "testuser".</param>
	/// <param name="email">The email address. Defaults to "test@example.com".</param>
	/// <param name="fullName">The full name. Defaults to "Test User".</param>
	/// <returns>A new User instance configured with the specified values.</returns>
	private static User MakeUser(int id = 1, string username = "testuser", string email = "test@example.com", string fullName = "Test User") => new()
	{
		Id = id,
		Username = username,
		Email = email,
		FullName = fullName,
		IsActive = true,
		CreatedAt = DateTime.UtcNow
	};

	/// <summary>
	/// Tests that GetUserByIdAsync returns user from cache without calling repository when cache hit occurs.
	/// Verifies that cached user data is properly retrieved and repository is not invoked.
	/// </summary>
	[Fact]
	public async Task GetUserByIdAsync_WhenCacheHit_ReturnsUserWithoutRepositoryCall()
	{
		var user = MakeUser(id: 1);
		_mockCache
			.Setup(c => c.GetOrLoadAsync<User>(
				"user:1",
				It.IsAny<Func<Task<User>>>(),
				It.IsAny<TimeSpan?>()))
			.ReturnsAsync(user);

		var result = await _sut.GetUserByIdAsync(1);

		result.Should().BeEquivalentTo(user);
		_mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
	}

	/// <summary>
	/// Tests that GetUserByIdAsync uses the correct cache key format when retrieving users by ID.
	/// Verifies that the cache key follows the pattern "user:{id}".
	/// </summary>
	[Fact]
	public async Task GetUserByIdAsync_UsesCorrectCacheKey()
	{
		_mockCache
			.Setup(c => c.GetOrLoadAsync<User>(
				It.IsAny<string>(),
				It.IsAny<Func<Task<User>>>(),
				It.IsAny<TimeSpan?>()))
			.ReturnsAsync((User?)null);

		await _sut.GetUserByIdAsync(99);

		_mockCache.Verify(c => c.GetOrLoadAsync<User>(
			"user:99",
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()), Times.Once);
	}

	/// <summary>
	/// Tests that GetUserByUsernameAsync retrieves user by username using the correct cache key.
	/// Verifies that the cache key follows the pattern "user:username:{username}".
	/// </summary>
	[Fact]
	public async Task GetUserByUsernameAsync_RetrievesUserByUsername()
	{
		var user = MakeUser(username: "john_doe");
		_mockCache
			.Setup(c => c.GetOrLoadAsync<User>(
				"user:username:john_doe",
				It.IsAny<Func<Task<User>>>(),
				It.IsAny<TimeSpan?>()))
			.ReturnsAsync(user);

		var result = await _sut.GetUserByUsernameAsync("john_doe");

		result.Should().BeEquivalentTo(user);
	}

	/// <summary>
	/// Tests that CreateUserAsync persists and caches a new user when validation passes.
	/// Verifies that the user is created, cached, and the repository is called appropriately.
	/// </summary>
	[Fact]
	public async Task CreateUserAsync_WithValidUser_PersistsAndCaches()
	{
		var newUser = MakeUser(id: 0, username: "newuser", email: "new@example.com");
		var createdUser = MakeUser(id: 10, username: "newuser", email: "new@example.com");

		_mockRepo.Setup(r => r.GetByUsernameAsync("newuser"))
			.ReturnsAsync((User?)null);
		_mockCache.Setup(c => c.WriteAsync(
			"user:0",
			It.IsAny<User>(),
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(createdUser);
		_mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var result = await _sut.CreateUserAsync(newUser);

		result.Id.Should().Be(10);
		result.Username.Should().Be("newuser");
	}

	/// <summary>
	/// Tests that CreateUserAsync throws ValidationException when email is invalid.
	/// Verifies that email validation occurs before user creation.
	/// </summary>
	[Fact]
	public async Task CreateUserAsync_WithInvalidEmail_ThrowsValidationException()
	{
		var userWithBadEmail = MakeUser(email: "not-an-email");

		Func<Task> act = () => _sut.CreateUserAsync(userWithBadEmail);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*email*");
	}

	/// <summary>
	/// Tests that CreateUserAsync throws ValidationException when username already exists.
	/// Verifies that username uniqueness is enforced before user creation.
	/// </summary>
	[Fact]
	public async Task CreateUserAsync_WhenUsernameExists_ThrowsValidationException()
	{
		var existingUser = MakeUser(username: "existing");
		var newUserWithSameUsername = MakeUser(id: 0, username: "existing");

		_mockRepo.Setup(r => r.GetByUsernameAsync("existing"))
			.ReturnsAsync(existingUser);

		Func<Task> act = () => _sut.CreateUserAsync(newUserWithSameUsername);

		await act.Should().ThrowAsync<ValidationException>()
			.WithMessage("*exists*");
		_mockRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
	}

	/// <summary>
	/// Tests that CreateUserAsync invalidates user list caches when a new user is created.
	/// Verifies that both "users:all" and "users:active" cache entries are removed.
	/// </summary>
	[Fact]
	public async Task CreateUserAsync_InvalidatesUserListCache()
	{
		var user = MakeUser(id: 0);
		var created = MakeUser(id: 5);

		_mockRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
			.ReturnsAsync((User?)null);
		_mockCache.Setup(c => c.WriteAsync(
			It.IsAny<string>(),
			It.IsAny<User>(),
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(created);
		_mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		await _sut.CreateUserAsync(user);

		_mockCache.Verify(c => c.RemoveAsync("users:all"), Times.Once);
		_mockCache.Verify(c => c.RemoveAsync("users:active"), Times.Once);
	}

	/// <summary>
	/// Tests that UpdateUserAsync updates user and invalidates cache when user exists.
	/// Verifies that the user is updated, cached, and cache invalidation occurs for user-specific and list caches.
	/// </summary>
	[Fact]
	public async Task UpdateUserAsync_WhenUserExists_UpdatesAndInvalidatesCache()
	{
		var user = MakeUser(id: 1, fullName: "Jane Doe");
		user.FullName = "Jane Smith";

		_mockCache.Setup(c => c.GetOrLoadAsync<User>(
			"user:1",
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(user);
		_mockRepo.Setup(r => r.UpdateAsync(user))
			.ReturnsAsync(user);
		_mockCache.Setup(c => c.WriteAsync(
			It.IsAny<string>(),
			It.IsAny<User>(),
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(user);
		_mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var result = await _sut.UpdateUserAsync(user);

		result.Should().BeEquivalentTo(user);
		_mockCache.Verify(c => c.WriteAsync(
			"user:1",
			user,
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()), Times.Once);
		_mockCache.Verify(c => c.RemoveAsync("users:all"), Times.Once);
	}

	/// <summary>
	/// Tests that UpdateUserAsync throws NotFoundException when user does not exist.
	/// Verifies that attempting to update a non-existent user throws the appropriate exception.
	/// </summary>
	[Fact]
	public async Task UpdateUserAsync_WhenUserNotFound_ThrowsNotFoundException()
	{
		var user = MakeUser(id: 999);

		_mockCache.Setup(c => c.GetOrLoadAsync<User>(
			"user:999",
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync((User?)null);

		Func<Task> act = () => _sut.UpdateUserAsync(user);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	/// <summary>
	/// Tests that DeactivateUserAsync deactivates user and invalidates cache when user exists.
	/// Verifies that the user is deactivated, cached, and active user cache is invalidated.
	/// </summary>
	[Fact]
	public async Task DeactivateUserAsync_WhenUserExists_DeactivatesAndInvalidatesCache()
	{
		var user = MakeUser(id: 1);

		_mockCache.Setup(c => c.GetOrLoadAsync<User>(
			"user:1",
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(user);
		_mockRepo.Setup(r => r.UpdateAsync(user))
			.ReturnsAsync(user);
		_mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<TimeSpan?>()))
			.Returns(Task.CompletedTask);
		_mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var result = await _sut.DeactivateUserAsync(1);

		result.Should().BeTrue();
		_mockCache.Verify(c => c.RemoveAsync("users:active"), Times.Once);
	}

	/// <summary>
	/// Tests that GetActiveUsersAsync returns active users from cache.
	/// Verifies that active users are retrieved using the correct cache key.
	/// </summary>
	[Fact]
	public async Task GetActiveUsersAsync_ReturnsActiveUsersFromCache()
	{
		var activeUsers = new List<User>
		{
			MakeUser(id: 1),
			MakeUser(id: 2)
		};

		_mockCache.Setup(c => c.GetOrLoadAsync<IEnumerable<User>>(
			"users:active",
			It.IsAny<Func<Task<IEnumerable<User>>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(activeUsers);

		var result = await _sut.GetActiveUsersAsync();

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
	}

	/// <summary>
	/// Tests that GetAllUsersAsync returns all users from cache.
	/// Verifies that all users are retrieved using the correct cache key.
	/// </summary>
	[Fact]
	public async Task GetAllUsersAsync_ReturnsAllUsersFromCache()
	{
		var allUsers = new List<User>
		{
			MakeUser(id: 1),
			MakeUser(id: 2),
			MakeUser(id: 3)
		};

		_mockCache.Setup(c => c.GetOrLoadAsync<IEnumerable<User>>(
			"users:all",
			It.IsAny<Func<Task<IEnumerable<User>>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(allUsers);

		var result = await _sut.GetAllUsersAsync();

		result.Should().HaveCount(3);
	}

	/// <summary>
	/// Tests that DeleteUserAsync deletes user and invalidates cache when user exists.
	/// Verifies that the user is deleted, cached entries are invalidated, and returns true.
	/// </summary>
	[Fact]
	public async Task DeleteUserAsync_WhenUserExists_DeletesAndInvalidatesCache()
	{
		var user = MakeUser(id: 1);

		_mockCache.Setup(c => c.GetOrLoadAsync<User>(
			"user:1",
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync(user);
		_mockRepo.Setup(r => r.DeleteAsync(1))
			.ReturnsAsync(true);
		_mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
			.Returns(Task.CompletedTask);

		var result = await _sut.DeleteUserAsync(1);

		result.Should().BeTrue();
		_mockCache.Verify(c => c.RemoveAsync("users:all"), Times.Once);
		_mockCache.Verify(c => c.RemoveAsync("users:active"), Times.Once);
	}

	/// <summary>
	/// Tests that DeleteUserAsync returns false when user does not exist.
	/// Verifies that attempting to delete a non-existent user returns false and does not call repository.
	/// </summary>
	[Fact]
	public async Task DeleteUserAsync_WhenUserNotFound_ReturnsFalse()
	{
		_mockCache.Setup(c => c.GetOrLoadAsync<User>(
			"user:1",
			It.IsAny<Func<Task<User>>>(),
			It.IsAny<TimeSpan?>()))
			.ReturnsAsync((User?)null);

		var result = await _sut.DeleteUserAsync(1);

		result.Should().BeFalse();
		_mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
	}
}