using FluentValidation;
using FedCarrier.Shipment.Application.Commands;

namespace FedCarrier.Shipment.Application.Validators;

public class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateShipmentItemDtoValidator());
    }
}

public class CreateShipmentItemDtoValidator : AbstractValidator<CreateShipmentItemDto>
{
    public CreateShipmentItemDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Weight).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class AssignDriverCommandValidator : AbstractValidator<AssignDriverCommand>
{
    public AssignDriverCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
    }
}

public class UpdateShipmentStatusCommandValidator : AbstractValidator<UpdateShipmentStatusCommand>
{
    public UpdateShipmentStatusCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
