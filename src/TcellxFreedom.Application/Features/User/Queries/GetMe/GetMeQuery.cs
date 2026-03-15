using MediatR;
using TcellxFreedom.Application.DTOs;

namespace TcellxFreedom.Application.Features.User.Queries.GetMe;

public sealed record GetMeQuery(string UserId) : IRequest<UserDto>;
