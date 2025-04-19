using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models.Records;
using LogicApp.Models;
using static BackgroundJobCodingChallenge.Services.IDatabaseService;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(LoadTenantUsers), false, true)]
public class InitEmailJobs(IDatabaseService db) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken)
    {
        if (timeoutToken.IsCancellationRequested)
            throw new JobRetryableException("Step was canceled.");

        var offset = GetOptionalExecutionItem<int>(incomingState, "offset", 0);

        var page = await LoadUsersPaginated(
            incomingState.Scope.TenantId,
            timeoutToken,
            1000,
            offset
        );

        

        if (page.Count == 0)
        {
            incomingState.Completed = true;
            return (incomingState, ResultStatus.Success);
        }

        
    }

    private async Task<List<TenantUser>> LoadUsersPaginated(string tenantId, CancellationToken timeoutToken, int size, int offset)
    {

    }
}
