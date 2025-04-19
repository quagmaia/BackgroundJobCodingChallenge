namespace HttpApp.Controllers;

public record ApiResponse
{
    public List<dynamic> Errors { get; set; } = new();
    public dynamic? Data { get; set; }
}
