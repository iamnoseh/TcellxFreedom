using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Infrastructure.Identity;

namespace TcellxFreedom.Infrastructure.Mappers;

public static class UserMapper
{
    public static User ToDomain(this ApplicationUser appUser) =>
        User.Reconstitute(
            appUser.Id,
            appUser.PhoneNumber!,
            appUser.FirstName,
            appUser.LastName,
            appUser.Balance,
            appUser.CreatedAt,
            appUser.UpdatedAt);

    public static ApplicationUser ToInfrastructure(this User user, ApplicationUser? existing = null)
    {
        var appUser = existing ?? new ApplicationUser();
        appUser.Id = user.Id;
        appUser.PhoneNumber = user.PhoneNumber;
        appUser.UserName = user.PhoneNumber;
        appUser.FirstName = user.FirstName;
        appUser.LastName = user.LastName;
        appUser.Balance = user.Balance;
        appUser.UpdatedAt = user.UpdatedAt;
        if (existing is null)
            appUser.CreatedAt = user.CreatedAt;
        return appUser;
    }
}
