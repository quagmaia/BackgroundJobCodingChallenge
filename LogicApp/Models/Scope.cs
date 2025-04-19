namespace LogicApp.Models;

public record Scope 
{ 
    public required string? TenantId { get; init; }
    public required string JobId { get; init; }

    public bool GlobalScope => string.IsNullOrWhiteSpace(TenantId);
    public bool TenantScope => !string.IsNullOrWhiteSpace(TenantId);

}
