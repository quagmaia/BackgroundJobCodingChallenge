using LogicApp.JobExecution;

namespace BackgroundJobCodingChallenge.Services;

public interface IJobStateManager
{
    Task<JobState> Read(string key, CancellationToken cancellation = default);
    Task<JobState?> TryRead(string key, CancellationToken cancellation = default);
    Task<JobState> ValidateETag(string key, string eTag, CancellationToken cancellation = default); //throws ETagConflict if there is an eTag mismatch
    Task<JobState> Write(string key, JobState entities, TimeSpan? ttl, CancellationToken cancellation = default); //throws ETagConflict if there is an eTag mismatch 

}

public class ETagConflict(string message) : Exception(message) { }