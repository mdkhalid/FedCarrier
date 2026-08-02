using FedCarrier.Contracts;
using FedCarrier.Identity.Application.Commands;
using FedCarrier.Identity.Application.Handlers;
using FedCarrier.Identity.Domain;
using FedCarrier.Identity.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FedCarrier.Tests;

public class IdentityServiceTests
{
    private IdentityDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static IConfiguration GetConfig()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Jwt:Key"]).Returns("a-super-secret-key-at-least-32-characters-long");
        config.Setup(c => c["Jwt:Issuer"]).Returns("FedCarrier");
        config.Setup(c => c["Jwt:Audience"]).Returns("FedCarrier.Client");
        return config.Object;
    }

    [Fact]
    public async Task RegisterCommandHandler_ShouldCreateUser()
    {
        var db = GetDbContext();
        var handler = new RegisterCommandHandler(db);
        var command = new RegisterCommand
        {
            Email = "test@fedcarrier.com",
            Password = "Passw0rd!",
            FirstName = "John",
            LastName = "Doe",
            Role = "Customer"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var user = await db.Users.FindAsync(result.Data);
        user.Should().NotBeNull();
        user.Email.Should().Be("test@fedcarrier.com");
        user.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterCommandHandler_ShouldRejectDuplicateEmail()
    {
        var db = GetDbContext();
        var handler = new RegisterCommandHandler(db);
        var command = new RegisterCommand
        {
            Email = "dup@fedcarrier.com",
            Password = "Passw0rd!",
            FirstName = "Jane",
            LastName = "Doe"
        };

        await handler.Handle(command, CancellationToken.None);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Email already registered");
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldReturnTokens()
    {
        var db = GetDbContext();
        var registerHandler = new RegisterCommandHandler(db);
        await registerHandler.Handle(new RegisterCommand
        {
            Email = "login@fedcarrier.com",
            Password = "Passw0rd!",
            FirstName = "John",
            LastName = "Doe"
        }, CancellationToken.None);

        var handler = new LoginCommandHandler(db, GetConfig());
        var command = new LoginCommand { Email = "login@fedcarrier.com", Password = "Passw0rd!" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldRejectInvalidCredentials()
    {
        var db = GetDbContext();
        var handler = new LoginCommandHandler(db, GetConfig());
        var command = new LoginCommand { Email = "nobody@fedcarrier.com", Password = "wrong" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task RefreshTokenCommandHandler_ShouldRotateToken()
    {
        var db = GetDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "refresh@fedcarrier.com",
            PasswordHash = "hash",
            Role = "Driver",
            CreatedAt = DateTime.UtcNow
        };
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenCommandHandler(db, GetConfig());
        var command = new RefreshTokenCommand { Token = "valid-refresh-token" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBe("valid-refresh-token");

        var oldToken = await db.RefreshTokens.FindAsync(refreshToken.Id);
        oldToken.IsRevoked.Should().BeTrue();
    }
}
