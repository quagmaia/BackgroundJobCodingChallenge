namespace LogicApp.Models;

public record ExecutionHistory
{
    public required string Step { get; init; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public ResultStatus Result { get; set; } = ResultStatus.Unknown;
    public string? ResultMessage { get; set; }
}