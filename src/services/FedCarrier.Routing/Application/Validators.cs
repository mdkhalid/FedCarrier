using FluentValidation;
using FedCarrier.Routing.Application.Commands;

namespace FedCarrier.Routing.Application.Validators;

public class OptimizeRouteCommandValidator : AbstractValidator<OptimizeRouteCommand>
{
    public OptimizeRouteCommandValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.OriginLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.OriginLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DestinationLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.DestinationLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.OriginAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DestinationAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Waypoints).Must(w => w is null || w.Count <= 20)
            .WithMessage("Route can contain at most 20 waypoints");
    }
}

public class UpdateRouteStatusCommandValidator : AbstractValidator<UpdateRouteStatusCommand>
{
    public UpdateRouteStatusCommandValidator()
    {
        RuleFor(x => x.RoutePlanId).NotEmpty();
    }
}
