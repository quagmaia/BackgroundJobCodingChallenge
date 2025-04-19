using System;

namespace BackgroundJobCodingChallenge.Services;

public interface IDatabaseService
{
	delegate IQueryable<TResult> FCreateQuery<TEntity, TResult>(IQueryable<TEntity> query);

	Task<TResult> GetAsync<TEntity, TResult>(FCreateQuery<TEntity, TResult> createQuery, CancellationToken cancellation = default);

    Task<TEntity> CreateAsync<TEntity>(TEntity entities, CancellationToken cancellation = default);
	Task<TEntity> CreateAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellation = default);

	Task<TEntity> UpdateAsync<TEntity>(TEntity entities, CancellationToken cancellation = default);
	Task<TEntity> UpdateAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellation = default);

	Task DeleteAsync<TEntity>(TEntity entities, CancellationToken cancellation = default);
	Task DeleteAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellation = default);
}
