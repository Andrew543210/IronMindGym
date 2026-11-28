namespace IronMindGym.Services;

public sealed class DomainValidationException(string message, string field = "") : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class DomainConflictException(string message) : Exception(message);
