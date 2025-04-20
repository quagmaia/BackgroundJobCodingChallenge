using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;
using LogicApp.Services;
using Microsoft.Extensions.Logging;
using Polly;

namespace LogicApp.Execution;

public class Executioner(IJobStateManager jobStateManager, IExecutionStepLookup lookup, IQueueService queue, ILogger<Executioner> logger)
{
    public async Task ExecuteSteps(string jobKey, JobState? incomingState)
    {
        using CancellationTokenSource timeout = new (TimeSpan.FromSeconds(600));

        var initialState = await jobStateManager.TryRead(jobKey, timeout.Token) ?? incomingState;
        
        if (initialState is null)
        {
            logger.LogError("Job state {id} not found", jobKey);
            return;
        }

        if (initialState.Completed || initialState.Canceled || initialState.Failed)
            return;

        var jobState = initialState with { };

        var shouldCancel = initialState.KillDate < DateTimeOffset.UtcNow;
        if (shouldCancel)
        {
            jobState.Canceled = true;
            await ForceUpdateJobState(jobState, TimeSpan.FromDays(30));
            return;
        }

        jobState.CurrentStep++;
        var markCompleteAndEnd = jobState.AllSequentialSteps.Count <= jobState.CurrentStep;
        if (markCompleteAndEnd)
        {
            jobState.Completed = true;
            UpdateJobState(jobState, TimeSpan.FromDays(30)).Wait(timeout.Token);
            return;
        }

        var finalResult = jobState with { };

        try
        {
            var stepGroup = jobState.AllSequentialSteps[jobState.CurrentStep];
            foreach (var stepDfn in stepGroup)
            {
                var tempState = jobState with { };
                try
                {
                    tempState = await RunStep(stepDfn, tempState, timeout.Token);
                    jobState.ExecutionData = tempState.ExecutionData;

                }
                catch (Exception e)
                {
                    finalResult = initialState with { ExecutionHistory = tempState.ExecutionHistory }; //the in-progress step data needs to all be retried, undo everything
                    
                    if (e is JobRetryableException)
                        throw;
                    
                    if (e is StepRetryableException sre)
                    {
                        if (sre.AllowRequeues)
                            throw new JobRetryableException($"Step {stepDfn.Name} ran out of retries; will requeue and attempt later", e);
                        throw new JobUnretryableException($"Step {stepDfn.Name} ran out of retries and cannot recover; canceling job", e);
                    }
                }
            }

            var ttl = jobState.Completed ? (TimeSpan?)TimeSpan.FromDays(30) : null;
            await UpdateJobState(jobState, ttl);

            if (!jobState.Completed || !jobState.Canceled || !jobState.Failed)
                await queue.QueueMessageAsync(jobState.QueueId, jobState);

        }
        catch (JobRetryableException e)
        {
            logger.LogError(e, "Job exceptioned. {retryable} {queueId} {parentJobId} {jobId} {tenantId} {currentJobSteps}", true, jobState.QueueId, jobState.ParentExecutionId, jobState.ExecutionId, jobState.Scope.TenantId, jobState.CurrentStep);
            await UpdateJobState(finalResult);
            await queue.QueueMessageAsync(finalResult.QueueId, finalResult);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Job exceptioned. {retryable} {queueId} {parentJobId} {jobId} {tenantId} {currentJobSteps}", false, jobState.QueueId, jobState.ParentExecutionId, jobState.ExecutionId, jobState.Scope.TenantId, jobState.CurrentStep);
            
            finalResult.Failed = true;
            await UpdateJobState(finalResult);
        }
    }

    //warning!! this mutates the jobState parameter(so that we don't lose execution history on thrown exceptions)
    private async Task<JobState> RunStep(StepDefinition stepDfn, JobState jobState, CancellationToken cancellationToken) => await Policy
        .Handle<StepRetryableException>()
        .RetryAsync(2)
        .ExecuteAsync(async () =>
        {
            var stepHistory = new ExecutionHistory()
            {
                Step = stepDfn.Name,
                StartTime = DateTimeOffset.UtcNow
            };

            var step = lookup.Load(stepDfn.Name)!;

            ResultStatus resultStatus = ResultStatus.Unknown;

            try
            {
                (jobState, resultStatus) = await step.Execute(jobState, cancellationToken);
            }
            catch (Exception e)
            {
                var retryable = e is StepRetryableException || e is JobRetryableException;

                logger.LogError(e, "Step {step} exceptioned. {retryable} {queueId} {parentJobId} {jobId} {tenantId} {currentJobSteps}", stepDfn.Name, retryable, jobState.QueueId, jobState.ParentExecutionId, jobState.ExecutionId, jobState.Scope.TenantId, jobState.CurrentStep);
                stepHistory.EndTime = DateTimeOffset.UtcNow;


                stepHistory.Result = retryable ? ResultStatus.FailedRetryable : ResultStatus.FailedNotRetryable;
                stepHistory.ResultMessage = e.Message;
                throw;
            }

            stepHistory.EndTime = DateTimeOffset.UtcNow;
            stepHistory.Result = resultStatus;

            jobState.ExecutionHistory.Add(stepHistory);

            if (resultStatus != ResultStatus.Success)
                logger.LogInformation("Step {step} completed as {status}. {queueId} {parentJobId} {jobId} {tenantId} {currentJobSteps}", stepDfn.Name, resultStatus, jobState.QueueId, jobState.ParentExecutionId, jobState.ExecutionId, jobState.Scope.TenantId, jobState.CurrentStep);

            return jobState;
        });

    private async Task UpdateJobState(JobState newState, TimeSpan? ttl = null) //intentionally not cancelable 
    {
        var executionHistroy = new ExecutionHistory()
        {
            Step = "SaveResult",
            StartTime = DateTimeOffset.UtcNow
        };

        try
        {
            await jobStateManager.Write(newState.LookupKey, newState, ttl, default);
        }
        catch (ETagConflict e)
        {
            executionHistroy.Result = ResultStatus.Conflict;
            executionHistroy.ResultMessage = e.Message;
        }
    }

    private async Task ForceUpdateJobState(JobState newState, TimeSpan? ttl) //intentionally not cancelable 
    {
        try
        {
            await jobStateManager.Write(newState.LookupKey, newState, ttl, default);
        }
        catch (ETagConflict)
        {
            logger.LogError("ETag conflict when trying update job state; will force update");
            var latest = await jobStateManager.Read<JobState>(newState.LookupKey, default);
            var newNewState = latest with 
            { 
                Canceled = newState.Canceled,
                Completed = newState.Completed,
                Failed = newState.Failed,
                ExecutionHistory = latest.ExecutionHistory,
                ExecutionData = newState.ExecutionData,
                CurrentStep = newState.CurrentStep,
                AllSequentialSteps = newState.AllSequentialSteps
            };
            await jobStateManager.Write(newNewState.LookupKey, newNewState, ttl, default);
        }
    }
}
