namespace LogicApp.Models;

public record StepDefinition
{
    public string StepExecutionId { get; init; } = Guid.NewGuid().ToString();
    public required string Name { get; init; }
}

