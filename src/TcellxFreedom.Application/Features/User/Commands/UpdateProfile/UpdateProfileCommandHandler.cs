using MediatR;
using TcellxFreedom.Application.DTOs;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.User.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("User not found");

        user.UpdateProfile(request.FirstName, request.LastName);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Balance
        );
    }
}
