namespace IronMindGym.Contracts;

/// <summary>A choice in a list, such as a split type or a muscle group, in the request language.</summary>
public sealed record OptionResponse(int Id, string Name);
