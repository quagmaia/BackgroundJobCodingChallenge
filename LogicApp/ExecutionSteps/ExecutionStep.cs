using LogicApp.JobExecution;

namespace LogicApp.Tasks;

public abstract class ExecutionStep
{
    public abstract Task<JobState> Execute(JobState incomingState, CancellationToken cancellationToken);
    public abstract Task<(JobState, bool shouldRetry)> HandleError(JobState incomingState, dynamic? output, Exception error, CancellationToken cancellationToken); //returns true if the job should retry this step

}
