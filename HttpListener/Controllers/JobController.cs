using BackgroundJobCodingChallenge.Services;
using LogicApp.Models;
using LogicApp.Models.JobExecution;
using LogicApp.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HttpApp.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class JobController : ControllerBase
{
    public static readonly TimeSpan RequestTimeLimit = TimeSpan.FromSeconds(30);
    private readonly ILogger<JobController> _logger;
    private readonly IQueueService _queue;
    private readonly IDurableStateManager _durableStateManager;
    private readonly IExecutionStepLookup _lookup;

    public JobController(IDurableStateManager durableStateManager, IQueueService queue, IExecutionStepLookup lookup, ILogger<JobController> logger)
    {
        _durableStateManager = durableStateManager;
        _queue = queue;
        _lookup = lookup;
        _logger = logger;
    }

    [HttpPost(Name = "Enqueue/{queueChannel}")]
    public async Task<object> Enqueue([FromRoute] string queueChannel, [FromHeader] string tenantId, [FromHeader] string userId, [FromBody]EnqueueRequest request)
    {

        //validate the steps are legal
        var responseBody = new ApiResponse();

        foreach (var stepList in request.Steps.Values)
            foreach(var stepDefn in stepList)
                try
                {
                    _lookup.ValidateScopeForType(request.Scope, stepDefn.Name);
                }
                catch (Exception ex)
                {
                    responseBody.Errors.Add(ex);
                }
        
        if (responseBody.Errors.Count > 0)
            return new ObjectResult(responseBody) { StatusCode = StatusCodes.Status422UnprocessableEntity };

        var jobState = new JobState
        {
            AllSequentialSteps = request.Steps,
            Scope = request.Scope,
        };

        try
        {
            await _queue.QueueMessageAsync(request.QueueId, jobState, default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job {jobState}", jobState);
            responseBody.Errors.Add(ex);
            return new ObjectResult(jobState) { StatusCode = StatusCodes.Status500InternalServerError };
        }

        try
        {
            await _durableStateManager.Write(jobState.LookupKey, jobState, );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write new job state {jobState}; this is not a blocker but may impact performance and stability", jobState);
        }

        responseBody.Data = new 
        {
            jobId = jobState.ExecutionId,
        };

        return new ObjectResult(responseBody) { StatusCode = StatusCodes.Status201Created };
    }
}

public record EnqueueRequest 
{
    public required Scope Scope { get; init; }
    public int QueueId { get; init; }
    public OrderedDictionary<int, List<ExecutionIncoming>> Steps { get; init; } = new();
}
