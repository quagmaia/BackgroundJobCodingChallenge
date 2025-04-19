namespace BackgroundJobCodingChallenge.Services;

public interface ITriggerService
{
	delegate void FUnsubscribe();

	delegate Task FExecuteAsync(CancellationToken cancellationToken);

	FUnsubscribe Subscribe(FExecuteAsync executeAsync);

	void Unsubscribe(FExecuteAsync executeAsync);
}
