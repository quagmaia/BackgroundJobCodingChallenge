using LogicApp.Models;

namespace HttpApp.Controllers;

public record EnqueueRequest 
{
    public int QueueId { get; init; }
    public List<List<StepDefinition>> Steps { get; init; } = new();
    public Dictionary<string, dynamic> InitialInputs { get; init; } = new();
}
