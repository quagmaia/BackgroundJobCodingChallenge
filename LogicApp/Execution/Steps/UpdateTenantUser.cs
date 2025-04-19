using BackgroundJobCodingChallenge.Services;
using LogicApp.Execution.Steps.models;
using LogicApp.JobExecution;
using LogicApp.Models;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(UpdateTenantUser), false, true)]
public class UpdateTenantUser(IDatabaseService db) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken)
    {
        if (timeoutToken.IsCancellationRequested)
            throw new JobRetryableException("Step was canceled.");

        var userId = GetJobRequiredExecutionItem<string>(incomingState, "userId");
        var user = GetJobRequiredExecutionItem<TenantUser>(incomingState, $"user_{userId}");

        //push update
        await db.UpdateAsync(user, timeoutToken);

        return (incomingState, ResultStatus.Success);
    }


}
