namespace LogicApp.Models;

public enum ResultStatus
{
    Unknown = 'z',
    Success = 's',
    Timeout = 't',
    Canceled = 'c',
    NeedRetry = 'r',
    NeedUserAction = 'u',
    FailedNoRetry = 'f',
    FailedNeedAttention = 'n',
    FailedCritical = 'c',
}
