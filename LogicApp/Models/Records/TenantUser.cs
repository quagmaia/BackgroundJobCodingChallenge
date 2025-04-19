namespace LogicApp.Models.Records;

public record TenantUser(string Id, string TenantId, string PreferredEmail, string PreferredEmailStatus);

public record Tenant(string Id, bool Active);