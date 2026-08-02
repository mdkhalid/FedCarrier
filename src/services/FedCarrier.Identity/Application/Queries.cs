using FedCarrier.Contracts;
using FedCarrier.Identity.Application.Commands;
using MediatR;

namespace FedCarrier.Identity.Application.Queries;

public class GetCurrentUserQuery : IRequest<ApiResponse<UserDto>>
{
}
