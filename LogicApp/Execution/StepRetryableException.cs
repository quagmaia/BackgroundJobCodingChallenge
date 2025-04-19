namespace LogicApp.JobExecution;

public class StepRetryableException(string message, Exception? innerException = null) : Exception(message, innerException) //should retry without time deplay
{
    public bool ShouldAllowJobRetries { get; set; } = true;
}
