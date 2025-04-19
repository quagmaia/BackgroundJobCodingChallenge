namespace LogicApp.Models;

public record ExecutionIncoming
{
    public string? ParentJobExecutionId { get; set; } = null;
    public required string JobExecutionId { get; set; }
    public required string StepExecutionId { get; init; } = Guid.NewGuid().ToString();
    public required string Name { get; init; }
    public required Scope Scope { get; init; }
    public required dynamic Input { get; init; }
}

