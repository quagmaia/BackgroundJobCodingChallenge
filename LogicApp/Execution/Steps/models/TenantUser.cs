namespace LogicApp.Execution.Steps.models;

public record TenantUser(string Id, string TenantId, string PreferredEmail, string PreferredEmailStatus);

public record Tenant(string Id, bool Active);

