namespace TcellxFreedom.Application.DTOs.Gemini;

public sealed record GeminiChatScheduleRequest(
    string FreeText,
    DateTime Date,
    string UserTimeZone
);
