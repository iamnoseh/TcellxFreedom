using TcellxFreedom.Domain.Entities;

namespace TcellxFreedom.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
