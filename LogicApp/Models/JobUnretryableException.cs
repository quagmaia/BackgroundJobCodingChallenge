namespace LogicApp.Models;

public class JobUnretryableException(string message, Exception? innerException = null) : Exception(message, innerException);
