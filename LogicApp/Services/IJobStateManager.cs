namespace BackgroundJobCodingChallenge.Services;

public interface IJobStateManager
{
    Task<JobState> Read<JobState>(string key, CancellationToken cancellation = default);
    Task<JobState> ValidateETag<JobState>(string key, string eTag, CancellationToken cancellation = default); //throws ETagConflict if there is an eTag mismatch
    Task<JobState> Write<JobState>(string key, JobState entities, TimeSpan? ttl, CancellationToken cancellation = default); //throws ETagConflict if there is an eTag mismatch 
    Task<JobState> Patch<JobState>(string key, JobState entities); 

}

public class ETagConflict(string message) : Exception(message) { }