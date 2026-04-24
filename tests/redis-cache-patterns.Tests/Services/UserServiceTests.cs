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

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<UserService>> _mockLogger = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_mockRepo.Object, _mockCache.Object, _mockLogger.Object);
    }

    private static User MakeUser(int id = 1, string username = "testuser", string email = "test@example.com") => new()
    {
        Id = id,
        Username = username,
        Email = email,
        FullName = "Test User",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

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

    [Fact]
    public async Task CreateUserAsync_WithInvalidEmail_ThrowsValidationException()
    {
        var userWithBadEmail = MakeUser(email: "not-an-email");

        Func<Task> act = () => _sut.CreateUserAsync(userWithBadEmail);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*email*");
    }

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
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateUserAsync(user);

        result.Should().BeEquivalentTo(user);
        _mockCache.Verify(c => c.SetAsync("user:1", user, It.IsAny<TimeSpan?>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("users:all"), Times.Once);
    }

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
