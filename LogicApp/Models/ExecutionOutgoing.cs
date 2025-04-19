namespace LogicApp.Models;

public record ExecutionOutgoing
{
    public required StepDefinition Input { get; set; }
    public ResultStatus? ResultStatus { get; set; }
    public dynamic? Output { get; set; }
}
