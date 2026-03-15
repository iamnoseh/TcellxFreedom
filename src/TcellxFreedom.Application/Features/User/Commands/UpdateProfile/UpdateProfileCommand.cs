using MediatR;
using TcellxFreedom.Application.DTOs;

namespace TcellxFreedom.Application.Features.User.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(string UserId, string FirstName, string LastName) : IRequest<UserDto>;
