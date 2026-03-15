using MediatR;
using TcellxFreedom.Application.DTOs;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Application.Features.User.Queries.GetMe;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetMeQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("User not found");

        return new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Balance
        );
    }
}
