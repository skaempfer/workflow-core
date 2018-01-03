using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class WorkflowData
    {
        public int StepDelay { get; set; }
        public string Text { get; set; }
        public string Outcome1 { get; set; }
        public string Outcome2 { get; set; }
    }

    internal class AsyncConcurrentStep1 : StepBodyAsync
    {
        public int StepDelay{ get; set; }

        public string StepInput { get; set; }

        public string StepOutcome { get; set; }

        public async override Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            await Task.Delay(StepDelay);

            StepOutcome = StepInput;    

            return ExecutionResult.Next();
        }
    }

    public class ConcurrencyTestWorkflow : IWorkflow<WorkflowData>
    {
        public string Id => "ConcurrencyTestWorkflow";

        public int Version => 1;

        public void Build(IWorkflowBuilder<WorkflowData> builder)
        {
            builder
                .StartWith<AsyncConcurrentStep1>()
                    .Input(step => step.StepInput, data => data.Text)
                    .Input(step => step.StepDelay, data => data.StepDelay)
                    .Output(data => data.Outcome1, step => step.StepOutcome)
                .If(data => true)
                .Do(then => then.StartWith<AsyncConcurrentStep1>()
                    .Input(step => step.StepInput, data => data.Text)
                    .Input(step => step.StepDelay, data => data.StepDelay)
                    .Output(data => data.Outcome2, step => step.StepOutcome))
                .EndWorkflow();
        }
    }

    public class ConcurrentScenario : WorkflowTest<ConcurrencyTestWorkflow, WorkflowData>
    {
        public ConcurrentScenario()
        {
            Setup();
        }

        [Fact]
        public async Task Scenario()
        {
            var wf1Data = new WorkflowData
            {
                Text = "Instance 1",
                StepDelay = 3000
            };
            var wf1Id = StartWorkflow(wf1Data);

            var wf2Data = new WorkflowData
            {
                Text = "Instance 2",
                StepDelay = 2000
            };
            var wf2Id = StartWorkflow(wf2Data);

            WaitForWorkflowToComplete(wf1Id, TimeSpan.FromSeconds(30));
            WaitForWorkflowToComplete(wf2Id, TimeSpan.FromSeconds(30));

            var wf1 = PersistenceProvider.GetWorkflowInstance(wf1Id).Result;
            var wf2 = PersistenceProvider.GetWorkflowInstance(wf2Id).Result;

            int i = 1;
        }
    }
}
