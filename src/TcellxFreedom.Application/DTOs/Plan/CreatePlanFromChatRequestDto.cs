namespace TcellxFreedom.Application.DTOs.Plan;

public sealed record CreatePlanFromChatRequestDto(
    string Text,
    DateTime Date,
    string UserTimeZone
);
