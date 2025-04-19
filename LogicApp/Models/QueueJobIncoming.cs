namespace LogicApp.Models;

public record QueueJobIncoming<TInput> 
{
    public required Scope Scope { get; init; }
    public required TInput Input { get; init; }
    public bool Parallel { get; init; } = false;
}
