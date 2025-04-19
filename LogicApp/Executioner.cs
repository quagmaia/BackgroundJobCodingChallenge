using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;
using LogicApp.Services;
using Microsoft.Extensions.Logging;
using Polly;

namespace LogicApp;

public class Executioner(IJobStateManager jobStateManager, IExecutionStepLookup lookup, IQueueService queue, ILogger<Executioner> logger)
{
    public async Task ExecuteSteps(string jobKey, JobState? incomingState)
    {
        using CancellationTokenSource timeout = new (TimeSpan.FromSeconds(600));

        var oldState = await jobStateManager.TryRead<JobState>(jobKey, timeout.Token) ?? incomingState;
        
        if (oldState is null)
        {
            logger.LogError("Job state {id} not found", jobKey);
            return;
        }

        if (oldState.IsCompleted || oldState.IsCanceled)
            return;

        var jobState = oldState with { };

        var shouldCancel = oldState.KillDate < DateTimeOffset.UtcNow;
        if (shouldCancel)
        {
            jobState.IsCanceled = true;
            await ForceUpdateJobState(jobState, TimeSpan.FromDays(30));
            return;
        }

        jobState.CurrentStep++;
        var markCompleteAndEnd = jobState.AllSequentialSteps.Count <= jobState.CurrentStep;
        if (markCompleteAndEnd)
        {
            jobState.IsCompleted = true;
            UpdateJobState(jobState, TimeSpan.FromDays(30)).Wait(timeout.Token);
            return;
        }

        try
        {
            var stepGroup = jobState.AllSequentialSteps[jobState.CurrentStep];
            foreach (var stepDfn in stepGroup)
            {
                await RunStep(timeout, jobState, stepDfn);

            }
        }
        catch
        {

        }
        finally
        {
            var ttl = jobState.IsCompleted ? (TimeSpan?)TimeSpan.FromDays(30) : null;
            await UpdateJobState(jobState, ttl);

            if (!jobState.IsCompleted || !jobState.IsCanceled)
                await queue.QueueMessageAsync(jobState.QueueId, jobState);
        }
    }

    private async Task<JobState> RunStep(ExecutionIncoming stepDfn, JobState input, CancellationToken cancellationToken) => await Policy
        .Handle<StepRetryableException>()
        .RetryAsync(2)
        .ExecuteAsync(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var stepHistory = new ExecutionHistory()
            {
                Step = stepDfn.Name,
                StartTime = DateTimeOffset.UtcNow
            };

            var step = lookup.Load(stepDfn.Name)!;

            JobState jobState = input with { }; //avoids accidental mutation of input
            ResultStatus resultStatus = ResultStatus.Unknown;

            try
            {
                (jobState, resultStatus) = await step.Execute(jobState, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Step {step} failed. {queueId} {parentJobId} {jobId} {tenantId} {currentJobSteps}",stepDfn.Name, input.QueueId, input.ParentExecutionId, input.ExecutionId, input.Scope.TenantId, input.CurrentStep);
                stepHistory.EndTime = DateTimeOffset.UtcNow;

                if (e is StepRetryableException || e is JobRetryableException)
                {
                    stepHistory.Result = ResultStatus.FailedRetryable;
                    stepHistory.ResultMessage = e.Message;
                    throw;
                }

                stepHistory.Result = ResultStatus.FailedNotRetryable;
                stepHistory.ResultMessage = e.Message;
            }
            finally
            {
                jobState.ExecutionHistory.Add(stepHistory);
            }

            if (resultStatus != ResultStatus.Success)
            {
                
            }

            return jobState;
        });



    private async Task UpdateJobState(JobState newState, TimeSpan? ttl) //intentionally not cancelable 
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
                IsCanceled = newState.IsCanceled,
                IsCompleted = newState.IsCompleted,
                ExecutionHistory = latest.ExecutionHistory,
                ExecutionData = newState.ExecutionData,
                CurrentStep = newState.CurrentStep,
                AllSequentialSteps = newState.AllSequentialSteps
            };
            await jobStateManager.Write(newNewState.LookupKey, newNewState, ttl, default);
        }
    }
}
