namespace LogicApp.JobExecution;

public class JobUnretryableException(string message, Exception? innerException = null) : Exception(message, innerException) 
{
}
