namespace BackgroundJobCodingChallenge.Services;

public interface INotificationService
{
    Task<TEntity> PushEmail<TEntity>(TEntity entities);
    Task<TEntity> PushSMS<TEntity>(TEntity entities);
    Task<TEntity> PushAppNotify<TEntity>(TEntity entities);
    Task<TEntity> PushInternalAlert<TEntity>(TEntity entities);
}
