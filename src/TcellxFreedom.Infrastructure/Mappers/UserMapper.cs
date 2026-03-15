using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Infrastructure.Identity;

namespace TcellxFreedom.Infrastructure.Mappers;

public static class UserMapper
{
    public static User ToDomain(this ApplicationUser applicationUser)
    {
        var user = User.Create(applicationUser.PhoneNumber!);

        typeof(User).GetProperty(nameof(User.Id))!
            .SetValue(user, applicationUser.Id);

        if (!string.IsNullOrEmpty(applicationUser.FirstName) &&
            !string.IsNullOrEmpty(applicationUser.LastName))
        {
            user.UpdateProfile(applicationUser.FirstName, applicationUser.LastName);
        }

        if (applicationUser.Balance > 0)
        {
            user.UpdateBalance(applicationUser.Balance);
        }

        return user;
    }

    public static ApplicationUser ToInfrastructure(this User domainUser, ApplicationUser? existing = null)
    {
        var appUser = existing ?? new ApplicationUser();

        appUser.Id = domainUser.Id;
        appUser.PhoneNumber = domainUser.PhoneNumber;
        appUser.UserName = domainUser.PhoneNumber;
        appUser.FirstName = domainUser.FirstName;
        appUser.LastName = domainUser.LastName;
        appUser.Balance = domainUser.Balance;
        appUser.UpdatedAt = domainUser.UpdatedAt;

        if (existing == null)
        {
            appUser.CreatedAt = domainUser.CreatedAt;
        }

        return appUser;
    }
}
