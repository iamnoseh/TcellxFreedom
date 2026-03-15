namespace TcellxFreedom.Infrastructure.Configuration;

public sealed class OsonSmsSettings
{
    public const string SectionName = "OsonSmsSettings";

    public string Login { get; init; } = string.Empty;
    public string PassHash { get; init; } = string.Empty;
    public string Sender { get; init; } = string.Empty;
    public string Dlm { get; init; } = ";";
    public string T { get; init; } = "23";
    public string SendSmsUrl { get; init; } = string.Empty;
    public string CheckSmsStatusUrl { get; init; } = string.Empty;
    public string CheckBalanceUrl { get; init; } = string.Empty;
}
