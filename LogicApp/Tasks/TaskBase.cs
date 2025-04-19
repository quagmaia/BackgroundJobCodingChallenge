namespace LogicApp.Tasks;

public abstract class TaskBase<TInbput, TOutput>
    where TInbput : class
    where TOutput : class
{
    public abstract TOutput Execute(TInbput input, CancellationToken cancellationToken);
    public abstract void HandleError(TInbput input, TOutput output, Exception error, CancellationToken cancellationToken);

}
