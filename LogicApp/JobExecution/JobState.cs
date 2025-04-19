using LogicApp.Models;

namespace LogicApp.JobExecution
{
    public record JobState
    {
        public string? ParentExecutionId { get; set; } = null;
        public string ExecutionId { get; init; } = Guid.NewGuid().ToString();
        public dynamic? ExecutionData { get; set; }
        public required Scope Scope { get; init; }
        public int CurrentStep { get; set; } = -1; //-1 means it hasn't started yet
        public OrderedDictionary<int, List<ExecutionIncoming>> AllSequentialSteps { get; init; } = new();
        public List<ExecutionHistory> ExecutionHistory { get; set; } = new();
        public bool IsCompleted { get; set; }
        public bool IsCanceled { get; set; }
        public string LookupKey => $"{Scope.TenantId ?? "global"}_{ExecutionId}";
    }
}
