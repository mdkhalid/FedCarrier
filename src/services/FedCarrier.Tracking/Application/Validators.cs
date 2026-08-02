using FluentValidation;
using FedCarrier.Tracking.Application.Commands;

namespace FedCarrier.Tracking.Application.Validators;

public class CreateTrackingLocationCommandValidator : AbstractValidator<CreateTrackingLocationCommand>
{
    public CreateTrackingLocationCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public class UpdateTrackingStatusCommandValidator : AbstractValidator<UpdateTrackingStatusCommand>
{
    public UpdateTrackingStatusCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}


