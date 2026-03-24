namespace TcellxFreedom.Domain.Entities;

public sealed class User
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private User() { }

    public static User Reconstitute(
        string id, string phoneNumber, string? firstName, string? lastName,
        decimal balance, DateTime createdAt, DateTime? updatedAt)
    {
        return new User
        {
            Id = id,
            PhoneNumber = phoneNumber,
            FirstName = firstName,
            LastName = lastName,
            Balance = balance,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public static User Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        return new User
        {
            PhoneNumber = phoneNumber
        };
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBalance(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Balance cannot be negative", nameof(amount));

        Balance = amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddBalance(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeductBalance(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}
