using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Models;
using Microsoft.Extensions.Logging;

namespace LogicApp.Execution.Steps;

[ExecutionStep(nameof(UpdateTenantUser), false, true)]
public class BulkSendTenantUserEmails(IDatabaseService db, ILogger<BulkSendTenantUserEmails> logger) : ExecutionStep
{
    public override async Task<(JobState, ResultStatus)> Execute(JobState jobState, CancellationToken timeoutToken)
    {
        var result = ResultStatus.Unknown;

        var users = GetJobRequiredExecutionItem<List<TenantUser>>(jobState, $"remainingUsers");
        var previousErrors = GetJobRequiredExecutionItem<List<(TenantUser, Exception)>>(jobState, $"errors");
        
        var sentUsers = new List<TenantUser>();

        var ongoingErrors = previousErrors.ToList();

        foreach (var user in users)
        {
            if (timeoutToken.IsCancellationRequested)
            {
                var remainingUsers = users.Except(sentUsers).ToList();
                SetJobExecutionItem(jobState, $"remainingUsers", remainingUsers);
                return (jobState, ResultStatus.InProgress);
            }

            var userId = user.Id;
            var userEmail = user.PreferredEmail;

            SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "");

            var missingEmail = string.IsNullOrWhiteSpace(userEmail);

            if (missingEmail)
            {
                SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "missing");
                return (jobState, result);
            }

            var emailContent = GetJobRequiredExecutionItem<dynamic>(jobState, $"emailContent");

            try
            {
                await SendEmailToTenantUser(userEmail, emailContent);
                SetJobExecutionItem(jobState, $"user_{userId}_newEmailState", "active");
            }
            catch (Exception ex)
            {
                if (!(ex is EmailInactiveException || ex is EmailBlockedException))
                {
                    //log for dev attention
                }

                ongoingErrors.Add((user, ex));
            }

            sentUsers.Add(user);
        }

        SetJobExecutionItem(jobState, $"remainingUsers", users.Except(sentUsers).ToList());
        SetJobExecutionItem(jobState, $"errors", ongoingErrors);
        return (jobState, result);

    }

    private async Task SendEmailToTenantUser(string userEmail, dynamic emailContent)
    {
        throw new NotImplementedException();
    }
}
internal class EmailInactiveException : Exception { }
internal class EmailBlockedException : Exception { }