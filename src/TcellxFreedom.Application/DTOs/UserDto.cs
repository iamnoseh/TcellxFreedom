namespace TcellxFreedom.Application.DTOs;

public sealed record UserDto(
    string Id,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    decimal Balance
);
