namespace LogicApp.Models;

public class StepRetryableException(string message, Exception? innerException = null) : Exception(message, innerException) //should retry without time deplay
{
    public bool AllowRequeues { get; set; } = true;
    public TimeSpan RequeueInvisibilityTimeout { get; set; } = TimeSpan.FromSeconds(15);
}
