using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackgroundJobCodingChallenge.Services;
using LogicApp.Execution.Steps;
using LogicApp.JobExecution;
using LogicApp.Models;
using static BackgroundJobCodingChallenge.Services.IDatabaseService;

namespace LogicApp.Execution.Steps;


[ExecutionStep(nameof(LoadTenantUser), false, true)]
public class LoadTenantUser(IDatabaseService db) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken)
    {
        if (timeoutToken.IsCancellationRequested)
            throw new JobRetryableException("Step was canceled.");
        
        var userId = GetJobRequiredExecutionItem<string>(incomingState, "userId");

        FCreateQuery<TenantUser, TenantUser> lookup = query =>
            from user in query
            where user.Id == userId && user.TenantId == incomingState.Scope.TenantId
            select user;

        var user = await db.GetAsync(lookup, timeoutToken);
            if (user is null)
            {
            
            }

    }
}

public record TenantUser(string Id, string TenantId, string PreferredEmail);

[ExecutionStep(nameof(UpdateTenantUser), false, true)]
public class UpdateTenantUser(IDatabaseService db) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken)
    {
        if (timeoutToken.IsCancellationRequested)
            throw new JobRetryableException("Step was canceled.");

        var userId = GetJobRequiredExecutionItem<string>(incomingState, "userId");

        var user = GetJobRequiredExecutionItem<TenantUser>(incomingState, "user");

    }


}
