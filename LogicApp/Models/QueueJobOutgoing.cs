namespace LogicApp.Models;

public record QueueJobOutgoing<TOutput>
{
    public required Scope Scope { get; init; }
    public TOutput? Outputs { get; set; }
    public ResultStatus? ResultStatus { get; set; }
}
