using FedCarrier.Contracts;
using FedCarrier.Notification.Application.Commands;
using FedCarrier.Notification.Application.Handlers;
using FedCarrier.Notification.Application.Queries;
using FedCarrier.Notification.Domain;
using FedCarrier.Notification.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FedCarrier.Tests;

public class NotificationServiceTests
{
    private NotificationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new NotificationDbContext(options);
    }

    [Fact]
    public async Task SendNotificationCommandHandler_ShouldCreatePendingNotification()
    {
        var db = GetDbContext();
        var handler = new SendNotificationCommandHandler(db);
        var command = new SendNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.ShipmentStatus,
            Channel = NotificationChannel.Email,
            Subject = "Shipment Update",
            Body = "Your shipment is on its way.",
            Recipient = "customer@fedcarrier.com"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var notification = await db.Notifications.FindAsync(result.Data);
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.Subject.Should().Be("Shipment Update");
    }

    [Fact]
    public async Task MarkNotificationSentCommandHandler_ShouldMarkSent()
    {
        var db = GetDbContext();
        var sendHandler = new SendNotificationCommandHandler(db);
        var notificationId = (await sendHandler.Handle(new SendNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.System,
            Channel = NotificationChannel.Sms,
            Subject = "Alert",
            Body = "System alert"
        }, CancellationToken.None)).Data;

        var handler = new MarkNotificationSentCommandHandler(db);
        var command = new MarkNotificationSentCommand { NotificationId = notificationId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        var notification = await db.Notifications.FindAsync(notificationId);
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotificationsQueryHandler_ShouldReturnUserNotifications()
    {
        var db = GetDbContext();
        var userId = Guid.NewGuid();
        var handler = new SendNotificationCommandHandler(db);
        await handler.Handle(new SendNotificationCommand
        {
            UserId = userId,
            Type = NotificationType.Payment,
            Channel = NotificationChannel.Push,
            Subject = "Payment",
            Body = "Payment received"
        }, CancellationToken.None);
        await handler.Handle(new SendNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.System,
            Channel = NotificationChannel.Email,
            Subject = "Other",
            Body = "Other user"
        }, CancellationToken.None);

        var queryHandler = new GetNotificationsQueryHandler(db);
        var query = new GetNotificationsQuery { UserId = userId, Page = 1, PageSize = 20 };

        var result = await queryHandler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.TotalCount.Should().Be(1);
        result.Data.Items[0].Subject.Should().Be("Payment");
    }

    [Fact]
    public async Task CreateNotificationTemplateCommandHandler_ShouldCreateTemplate()
    {
        var db = GetDbContext();
        var handler = new CreateNotificationTemplateCommandHandler(db);
        var command = new CreateNotificationTemplateCommand
        {
            Type = NotificationType.ShipmentStatus,
            Name = "ShipmentDelivered",
            SubjectTemplate = "Your shipment {shipmentId} was delivered",
            BodyTemplate = "Thanks for shipping with FedCarrier."
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var template = await db.NotificationTemplates.FindAsync(result.Data);
        template.IsActive.Should().BeTrue();
    }
}
