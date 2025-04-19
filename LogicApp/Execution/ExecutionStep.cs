using LogicApp.Models;

namespace LogicApp.JobExecution;

public abstract class ExecutionStep
{
    public abstract Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken cancellationToken);
    public abstract Task<(JobState, ResultStatus)> HandleError(JobState incomingState, dynamic? output, Exception error, CancellationToken cancellationToken); //returns true if the job should retry this step

}
