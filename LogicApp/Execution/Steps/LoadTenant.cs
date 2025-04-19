using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogicApp.JobExecution;
using LogicApp.Models.Records;
using LogicApp.Models;
using static BackgroundJobCodingChallenge.Services.IDatabaseService;
using BackgroundJobCodingChallenge.Services;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(ValidateTenantIsActive), false, true)]
public class ValidateTenantIsActive(IDatabaseService db) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken)
    {
        if (timeoutToken.IsCancellationRequested)
            throw new JobRetryableException("Step was canceled.");

        FCreateQuery<Tenant, Tenant> lookup = tenants =>
            from tenant in tenants
            where tenant.Id == incomingState.Scope.TenantId
            select tenant;

        var tenant = await db.GetAsync(lookup, timeoutToken);

        if (tenant.Active)
            return (incomingState, ResultStatus.Success);
        else
            throw new JobUnretryableException($"Tenant {incomingState.Scope.TenantId} is not active.");

    }
}

