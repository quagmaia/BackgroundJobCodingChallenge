namespace LogicApp.Models;

public enum ResultStatus
{
    Unknown = 'z',
    Success = 's',
    Timeout = 't',
    Canceled = 'c',
    Conflict = 'e',
    FailedNotRetryable = 'f',
    FailedRetryable = 'r',
}
