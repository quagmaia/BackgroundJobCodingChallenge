namespace BackgroundJobCodingChallenge.Services;

public interface IDatabaseService
{
	delegate IQueryable<TResult> FCreateQuery<TEntity, TResult>(IQueryable<TEntity> query);

	Task<TResult> GetAsync<TEntity, TResult>(FCreateQuery<TEntity, TResult> createQuery);

	Task<TEntity> CreateAsync<TEntity>(TEntity entities);
	Task<TEntity> CreateAsync<TEntity>(IEnumerable<TEntity> entities);

	Task<TEntity> UpdateAsync<TEntity>(TEntity entities);
	Task<TEntity> UpdateAsync<TEntity>(IEnumerable<TEntity> entities);

	Task DeleteAsync<TEntity>(TEntity entities);
	Task DeleteAsync<TEntity>(IEnumerable<TEntity> entities);
}
