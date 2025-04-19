namespace LogicApp.JobExecution;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ExecutionStepAttribute(ExecutionStepMeta Meta) : Attribute
{
    public override string ToString() => Meta.ToString();

}

public record ExecutionStepMeta(string LookupName, bool AllowGlobalScope, bool AllowTenantScope) { }
