using System.Text.Json.Serialization;
using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;
using LogicApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace HttpApp.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class JobController(IJobStateManager jobStateManager, IQueueService queue, IExecutionStepLookup lookup, ILogger<JobController> logger) : ControllerBase
{
    public static readonly TimeSpan RequestTimeLimit = TimeSpan.FromSeconds(30);

    [HttpPatch(Name = "cancel/{jobKey}")]
    public async Task<object> Cancel([FromRoute] string jobKey, [FromHeader] string tenantId, [FromHeader] string userId)
    {
        try
        {
            var job = await jobStateManager.TryRead(jobKey);
            if (job == null)
                return new ObjectResult(new ApiResponse()) { StatusCode = StatusCodes.Status404NotFound };

            var updated = job with { Canceled = true };
            await jobStateManager.Write(jobKey, updated, null);
            return new ObjectResult(new ApiResponse()) { StatusCode = StatusCodes.Status302Found };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to cancel job {jobKey}", jobKey);
            return new ObjectResult(new ApiResponse()) { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }

    [HttpPost(Name = "enqueue/{queueChannel}")]
    public async Task<object> Enqueue([FromRoute] string queueChannel, [FromHeader] string tenantId, [FromHeader] string userId, [FromBody]EnqueueRequest request)
    {
        var scope = new Scope() { TenantId = tenantId };
        //validate the steps are legal
        var responseBody = new ApiResponse();

        foreach (var step in request.Steps.SelectMany(s => s))
            try
            {
                lookup.ValidateScopeForType(scope, step.Name);
            }
            catch (Exception ex)
            {
                responseBody.Errors.Add(ex);
            }
        
        if (responseBody.Errors.Count > 0)
            return new ObjectResult(responseBody) { StatusCode = StatusCodes.Status422UnprocessableEntity };

        var jobState = new JobState
        {
            Scope = scope,
            AllSequentialSteps = request.Steps,
            QueueId = request.QueueId,
        };

        try
        {
            await queue.QueueMessageAsync(request.QueueId, jobState, default);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue job {jobState}", jobState);
            responseBody.Errors.Add(ex);
            return new ObjectResult(jobState) { StatusCode = StatusCodes.Status500InternalServerError };
        }

        try
        {
            await jobStateManager.Write(jobState.LookupKey, jobState, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write new job state {jobState}; this is not a blocker but may impact performance and stability", jobState);
        }

        responseBody.Data = new 
        {
            jobId = jobState.ExecutionId,
        };

        return new ObjectResult(responseBody) { StatusCode = StatusCodes.Status201Created };
    }
}
