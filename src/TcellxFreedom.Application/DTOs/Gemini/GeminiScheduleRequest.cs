using TcellxFreedom.Domain.Enums;

namespace TcellxFreedom.Application.DTOs.Gemini;

public sealed record GeminiScheduleRequest(
    string UserTimeZone,
    DateTime StartDate,
    DateTime EndDate,
    List<GeminiTaskInput> UserTasks
);

public sealed record GeminiTaskInput(
    string Title,
    string? Description,
    string? PreferredTimeOfDay,
    RecurrenceType Recurrence
);
