using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;
using static BackgroundJobCodingChallenge.Services.IDatabaseService;
using LogicApp.Execution.Steps.models;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(InitEmailJobs), false, true)]
public class InitEmailJobs(IDatabaseService db, IQueueService queue) : ExecutionStep
{
    private const int PageSize = 1000;

    public override async Task<(JobState, ResultStatus)> Execute(JobState jobState, CancellationToken timeoutToken)
    {
        var keepGoing = false;

        var offset = GetOptionalExecutionItem(jobState, "offset", 0);

        while (keepGoing)
        { 
            SetJobExecutionItem(jobState, "offset", offset);

            if (timeoutToken.IsCancellationRequested)
                throw new JobRetryableException("Step was canceled.");

            var users = await LoadUsersPaginated(
                jobState.Scope.TenantId!,
                timeoutToken,
                PageSize,
                offset
            );

            if (users.Count == 0)
            {
                return (jobState, ResultStatus.Success);
            }

            var childJob = new JobState
            {
                ParentExecutionId = jobState.ExecutionId,
                Scope = jobState.Scope,
                ExecutionData = jobState.ExecutionData,
                KillDate = jobState.KillDate,
                QueueId = jobState.QueueId,
            };

            childJob.ExecutionData.Add("remainingUsers", users);
            childJob.ExecutionData.Add("allUsers", users);

            childJob.AllSequentialSteps.Add(new List<StepDefinition>()
            {
                new (){ Name = "BulkSendTenantUserEmails" },
                new (){ Name = "HandleFailedEmails" },
            });

            childJob.AllSequentialSteps.Add(new List<StepDefinition>()
            {
                new (){ Name = "NotifyJobComplete" }
            });

            SetJobExecutionItem(jobState, "offset", offset + PageSize);
            await queue.QueueMessageAsync(jobState.QueueId, childJob); //dont let this get interrupted with cancellation token
        }
        return (jobState, ResultStatus.Success);
    }

    private async Task<List<TenantUser>> LoadUsersPaginated(string tenantId, CancellationToken timeoutToken, int size, int offset)
    {

    }
}
