namespace IronMindGym.Services;

/// <summary>
/// A request the rules refuse, such as a date outside the allowed range. The API answers 400 with
/// validation problem details that list the message under <see cref="Field"/>, like a failed model validation.
/// </summary>
public sealed class DomainValidationException(string message, string field = "") : Exception(message)
{
    public string Field { get; } = field;
}

/// <summary>A request that conflicts with what is already stored. The API answers 409 with the message as the title.</summary>
public sealed class DomainConflictException(string message) : Exception(message);
