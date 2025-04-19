namespace BackgroundJobCodingChallenge.Services;

public interface IQueueService
{
	delegate void FUnsubscribe();

	delegate Task FProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken);

	Task QueueMessageAsync<TMessage>(
		int queueId,
		TMessage message,
		CancellationToken cancellationToken = default
	)
	where TMessage : class;

	FUnsubscribe SubscribeToMessages<TMessage>(
		int queueId,
		FProcessAsync<TMessage> processAsync
	)
	where TMessage : class;

	void UnsubscribeFromMessages<TMessage>(FProcessAsync<TMessage> processAsync);
}
