using LogicApp.Models;

namespace LogicApp.JobExecution
{
    public record JobState
    {
        public string? ParentExecutionId { get; set; } = null;
        public string ExecutionId { get; init; } = Guid.NewGuid().ToString();
        public int QueueId { get; set; }
        public dynamic? ExecutionData { get; set; }
        public required Scope Scope { get; init; }
        public int CurrentStep { get; set; } = -1; //-1 means it hasn't started yet
        public List<List<ExecutionIncoming>> AllSequentialSteps { get; init; } = new();
        public List<ExecutionHistory> ExecutionHistory { get; set; } = new();
        public bool IsCompleted { get; set; }
        public bool IsCanceled { get; set; }
        public DateTimeOffset KillDate { get; set; } = DateTimeOffset.UtcNow.AddDays(7); //jobs running after this timespan will be marked as canceled
        public string LookupKey => $"{Scope.TenantId ?? "global"}_{ExecutionId}";
    }
}
