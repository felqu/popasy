namespace APItests.Models;

public sealed record Question(string Text, string[] Options, int CorrectIndex, string Explanation);

public sealed record PersonnelTest(
    Guid Id, string Title, string Description, string Department, string Level,
    int DurationMinutes, int PassScore, DateTime CreatedAt, Question[] Questions, bool AiCreated);

public sealed record Employee(Guid Id, string Name, string Role, string Department, string Email, string Color)
{
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => p[0]));
}

public sealed record Assignment(
    Guid Id, Guid TestId, Guid EmployeeId, DateTime AssignedAt, DateTime Deadline,
    DateTime? CompletedAt = null, int? Score = null, int[]? Answers = null);
