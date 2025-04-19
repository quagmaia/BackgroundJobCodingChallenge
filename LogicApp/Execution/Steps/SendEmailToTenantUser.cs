using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(UpdateTenantUser), false, true)]
public class SendEmailToTenantUser(IDatabaseService db) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState jobState, CancellationToken timeoutToken)
    {
        if (timeoutToken.IsCancellationRequested)
            throw new JobRetryableException("Step was canceled.");

        var result = ResultStatus.Unknown;


        var userId = GetJobRequiredExecutionItem<string>(jobState, "userId");
        var user = GetJobRequiredExecutionItem<TenantUser>(jobState, $"user_{userId}");
        var userEmail = user.PreferredEmail;

        SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", null);

        var missingEmail = string.IsNullOrWhiteSpace(userEmail);

        if (missingEmail)
        {
            SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "missing");
            return (jobState, result);
        }

        var emailContentId = GetJobRequiredExecutionItem<string>(jobState, "emailContentId");
        var emailContent = GetJobRequiredExecutionItem<dynamic>(jobState, $"emailContent_{emailContentId}_content");

        jobState.ExecutionData.TryAdd($"user_{userId}_emailContent_{emailContentId}_sent", false);

        try
        {
            //do the thing
            //...
            
            jobState.ExecutionData[$"user_{userId}_emailContent_{emailContentId}_sent"] = true;
            SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "active");
            result = ResultStatus.Success;
        }
        catch (EmailInactiveException)
        {
            SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "inactive");
            result = ResultStatus.PartialSuccess;
        }
        catch (EmailBlockedException)
        {
            SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "blocked");
            result = ResultStatus.PartialSuccess;
        }
        catch (TimeoutException te)
        {
            throw new StepRetryableException("Email sending timed out.", te);
        }

        return (jobState, result);

    }


}
internal class EmailInactiveException : Exception { }
internal class EmailBlockedException : Exception { }