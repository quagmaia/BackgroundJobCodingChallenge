using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BackgroundJobCodingChallenge.Services;
using LogicApp.JobExecution;
using LogicApp.Services;
using LogicApp.Tasks;

namespace LogicApp
{
    public class Executioner
    {
        private readonly IJobStateManager _durableStateManager;
        private readonly IExecutionStepLookup _lookup;

        public Executioner(IJobStateManager durableStateManager, IExecutionStepLookup lookup)
        {
            _durableStateManager = durableStateManager;
            _lookup = lookup;
        }

        public async Task ExecuteSteps(string job)
        {
            using CancellationTokenSource timeout = new (TimeSpan.FromSeconds(60));

            var oldState = await _durableStateManager.Read<JobState>(job, timeout.Token);
            if (oldState.IsCompleted || oldState.IsCanceled)
                return;

            var jobState = oldState with { CurrentStep = oldState.CurrentStep + 1 };

            var jobShouldBeMarkedComplete = !jobState.AllSequentialSteps.Keys.Any(k => k >= jobState.CurrentStep);
            if (jobShouldBeMarkedComplete)
            {
                jobState.IsCompleted = true;
                await _durableStateManager.Write(jobState.ExecutionId, jobState, TimeSpan.FromMinutes(30));
            }

            if (!jobState.AllSequentialSteps.ContainsKey(jobState.CurrentStep))
                jobState.CurrentStep = jobState.AllSequentialSteps.Keys.First(k => k > jobState.CurrentStep); //just in case the steps are skipping numbers

            var stepDefns = jobState.AllSequentialSteps[jobState.CurrentStep];
            
            foreach (var stepDefn in stepDefns)
            {
                
            }

            await _durableStateManager.Write(jobState.ExecutionId, jobState, TimeSpan.FromMinutes(30));
            
        }

    }
