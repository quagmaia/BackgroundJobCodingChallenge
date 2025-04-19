namespace LogicApp.Models;

public enum ResultStatus
{
    Unknown = 'z',
    Success = 's',
    PartialSuccess = 'p',
    InProgress = 'i',
    Timeout = 't',
    Canceled = 'c',
    Conflict = 'e',
    FailedNotRetryable = 'f',
    FailedRetryable = 'r'
}
