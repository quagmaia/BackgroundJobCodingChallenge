using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;
using Microsoft.Extensions.Logging;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(UpdateTenantUser), true, false)]
public class BulkSendGlobalUserEmails(IDatabaseService db, ILogger<BulkSendTenantUserEmails> logger) : ExecutionStep
{
    public override Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken)
    {
        //initializes a BulkSendTenantUserEmails job for every single tenant
        throw new NotImplementedException();
    }
}
