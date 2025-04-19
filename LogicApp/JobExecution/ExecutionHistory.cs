using LogicApp.Models;

namespace LogicApp.JobExecution;

public record ExecutionHistory
{
    public required string Step { get; init; }
    public required DateTimeOffset? StartTime { get; set; }
    public required DateTimeOffset? EndTime { get; set; }
    public required bool IsRetry { get; set; }
    public required ResultStatus Status { get; init; }
}