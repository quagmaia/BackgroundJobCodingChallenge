namespace LogicApp.JobExecution;

public class JobRetryableException(string message, Exception? innerException = null) : Exception(message, innerException) //should requeue the job to try again later
{
}