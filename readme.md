# Architecture

## Queueing

Ideally, every tenant should have its own queue, and the global queue should be separate as well. 
This avoids the noisy neighbor issue where one particularly busy tenant does not hold up the line for others.

## Steps

A step is a unit of execution. A step can do any of the following:

1. read the job's execution data
1. write/overwrite the job's execution data
1. read from the Db
1. write to the Db
1. call an external service
1. enqueue another job
1. throw an exception
1. handle a token cancelation due to timeout

Some steps are idempotent, meaning it's safe to retry them. Others are not, and we need to be very careful about how we handle errors in that case.
Steps should be designed such that they would not run for longer than a few seconds.

## Execution Jobs

An execution job is defined by list of grouped steps. It is a completely internal concept that users should not be aware of. All externally visible data must be managed within the steps themselves. 

The steps are grouped up in order to allow the job to be reinserted into the queue, such as after retrying. If a timeout occurs during this group's running, all of the steps' accumulated results are thrown out, and the job is requeued.

### Group steps based on Idempotency
This means that it's very important that steps which are not idempotent should be separated into groups.

For example, say you want a job that will do the following:
1. load a user
2. apply a transaction to the user's account
1. send a notification to the user

It's important to not apply the transaction more than once in the case of a retry, so we should arrange the steps like so:
1.
	a. load a user
	b. apply a transaction to the user's account
2. 
	a. send a notification to the user

### Group steps based on expected run time
Step grouping also allows us to keep long-running jobs from blocking the queue. 

For example, say you want a job that will do the following:
1. load a user
2. make an Http call to a service that is slow or unreliable
3. apply the result to the user's account

The middle step has a high chance of running long or retrying too often, so we should separate it from everything else like so:
1. 
	a. load a user
2. 
	a. make an Http call to a service that is slow or unreliable
3. 
 	a. apply the result to the user's account


All jobs have a KillDate, which is a safety mechanism to prevent them from running an unresonably long length of time. 
However, it's still not great because it can result in dropped data. We must make sure to alert the relevant parties when a job gets killed.

### Spinning off child jobs

When you want some steps to run asynchronously to the main job, or when you want to change the execution scope (global -> tenant or vice versa), you can use a step to enqueue a brand new child job. 
The results of the child job will not have an impact on the parent job's completion status*. 
If you plan to paginate through large amounts of data and would like to do some of that in parallel, child jobs are the best way to go. 

Here's an example where you may use child jobs:
A (inputs: `string tenantId`, `long offset` which defaults to zero)
	1. ensure that the tenant for `tenantId` is active
	2. load as many pages of tenant users as you can within a time limit, starting from the `offset` value
	3. enqueue job B for each page of users 
	5. if there are more pages to load, enqueue a new job A with the new `offset` value

B (inputs: `List<user> users`)
	1. load the users from the Db
	2. apply a coupon to each user's account
	3. send a notification to each user
	4. push an update to some sort of custom aggregated result ("Total notified users: X");

# Additional Thoughts

Queue jobs have been around a long time, so this exact architecture has been implemented many times in the past. Here are some libraries and services that, if used, would change my implementation somewhat:

Microsoft Azure Queues and Durable Functions
Benefits:
	- Microsoft handles the scaling for you
	- built-in retries
	- built-in timeouts
	- dead-letter queue
	- native logging
Drawbacks:
	- Microsoft handles the scaling for you (and it's not very good at it!)
	- costly
	- queue messages are not easy to audit
	- a lot of the built-in stuff is hidden behind magic, so you may accidentally duplicate functionality or miss something important
	- the built-in retry logic isn't great, you'll probably want to add your own, too
	- azure functions (or at least the older versions) don't support user secrets, increasing the risk accidentally checking in sensitive data

Nats with JetStream [see docs](https://docs.nats.io/using-nats/developer/develop_jetstream)
Benefits:
	- open source
	- very fast
	- pub/sub model allows multiple subscribers to the same message, which is useful for parallel tasks
	- Jetsteam allows for longer TTL, historical records, downtime recovery, etc.
	- all config is in code, so it's easy to audit
	- easy to contain within a single cluster or private network
Drawbacks:
	- not as widely used as other solutions, so there may be a learning curve
	- you have to manage your own load-balancing
	- designed for many microservices, so probably not be suitable for the monolithic code I've written
	- setup is tricky, you'll definitely want a good infra team to manage it
	- this is just a queue with basic pub/sub stuff, not a framework

Temporal [see docs](https://docs.temporal.io/evaluate/)
Benefits:
	- open source
	- built-in observability supports metrics, logging, individual step tracing, auditing, etc.
	- built in timeout/retry handling
	- built-in cancelation support
	- built-in scheduling
	- can self-host or use Temporal's cloud service
Drawbacks
	- not as widely used as other solutions, so there may be a learning curve
	- config is tricky for self-hosting, you'll definitely want a good infra team to manage it
	- I'm sure I'm missing things! I have very little experience with Temporal compared to the other two.

# Footnotes 
> *I would like to implement a way to aggregate the results of multiple jobs back into the parent, but it may not be worth the complexity it would introduce. 
> Might be better to just have a custom model that each job push their results to. In either case, you will likely need resource locking to avoid conflicts.