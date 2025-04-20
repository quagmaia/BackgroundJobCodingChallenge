namespace LogicApp.Models;
public class JobRetryableException(string message, Exception? innerException = null) : Exception(message, innerException) //should requeue the job to try again later
{
    public TimeSpan RequeueInvisibilityTimeout { get; set; } = TimeSpan.FromSeconds(15);
}
