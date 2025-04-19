using LogicApp.JobExecution;
using LogicApp.Models;

namespace LogicApp.Execution.Steps;

public abstract class ExecutionStep
{

    protected static T? GetOptionalExecutionItem<T>(JobState incomingState, string key, T? defaultValue)
    {
        return incomingState.ExecutionData.TryGetValue(key, out var thing)
            ? thing is T 
                ? thing ?? defaultValue
                : defaultValue
            : defaultValue;
    }

    protected static T GetJobRequiredExecutionItem<T>(JobState incomingState, string key) 
    {
        return incomingState.ExecutionData.TryGetValue(key, out var thing)
            ? thing is T
                ? thing ?? throw new JobUnretryableException($"Execution data {key} is null.")
                : throw new JobUnretryableException($"Execution data {key} is not of type {typeof(T).Name}.")
            : throw new JobUnretryableException($"Execution data {key} not found.");
    }

    protected static void SetJobExecutionItem(JobState incomingState, string key, dynamic? value)
    {
        if (incomingState.ExecutionData.ContainsKey(key))
            incomingState.ExecutionData[key] = value;
        else
            incomingState.ExecutionData.Add(key, value);
    }


    public abstract Task<(JobState, ResultStatus)> Execute(JobState incomingState, CancellationToken timeoutToken);

}
