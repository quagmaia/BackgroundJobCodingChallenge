namespace LogicApp.JobExecution;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ExecutionStepAttribute(string LookupName, bool AllowGlobalScope, bool AllowTenantScope) : Attribute
{
    public ExecutionStepMeta Meta { get; set; } = new ExecutionStepMeta(LookupName, AllowGlobalScope, AllowTenantScope);
    public override string ToString() => Meta.ToString();
}

public record ExecutionStepMeta(string LookupName, bool AllowGlobalScope, bool AllowTenantScope) { }
