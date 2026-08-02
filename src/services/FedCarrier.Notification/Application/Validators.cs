using FluentValidation;
using FedCarrier.Notification.Application.Commands;

namespace FedCarrier.Notification.Application.Validators;

public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x.Recipient).MaximumLength(256);
    }
}

public class MarkNotificationSentCommandValidator : AbstractValidator<MarkNotificationSentCommand>
{
    public MarkNotificationSentCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}

public class CreateNotificationTemplateCommandValidator : AbstractValidator<CreateNotificationTemplateCommand>
{
    public CreateNotificationTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubjectTemplate).NotEmpty();
        RuleFor(x => x.BodyTemplate).NotEmpty();
    }
}
