using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LombdaAgentSDK.StateMachine;
using NUnit.Framework;

namespace Test 
{
    public class TestState : BaseState<int, string>
    {
        public List<int> EnteredInputs = new();
        public List<string> InvokedInputs = new();
        public bool ExitCalled = false;

        
        public override Task<string> Invoke(int input)
        {
            InvokedInputs.Add(input.ToString());
            return Task.FromResult($"Result:{input}");
        }

        public override async Task EnterState(int input)
        {
            EnteredInputs.Add(input);
            await Task.CompletedTask;
        }

        public override async Task ExitState()
        {
            ExitCalled = true;
            await Task.CompletedTask;
        }
    }

    [TestFixture]
    public class BaseStateGenericTests
    {
        [Test]
        public void GetInputType_ReturnsGenericType()
        {
            var state = new TestState();
            Assert.AreEqual(typeof(int), state.GetInputType());
        }

        [Test]
        public void GetOutputType_ReturnsGenericType()
        {
            var state = new TestState();
            Assert.AreEqual(typeof(string), state.GetOutputType());
        }

        [Test]
        public void OutputResults_Property_GetSet_Works()
        {
            var state = new TestState();
            var results = new List<StateResult<string>> { new StateResult<string>("1", "foo") };
            state.OutputResults = results;
            Assert.AreEqual(results, state.OutputResults);
        }

        [Test]
        public void InputProcesses_Property_GetSet_Works()
        {
            var state = new TestState();
            var process = new StateProcess<int>(state, 42);
            var processes = new List<StateProcess<int>> { process };
            state.InputProcesses = processes;
            Assert.AreEqual(processes, state.InputProcesses);
        }

        [Test]
        public void Transitions_Property_GetSet_Works()
        {
            var state = new TestState();
            var transition = new StateTransition<string>();
            var transitions = new List<StateTransition<string>> { transition };
            state.Transitions = transitions;
            Assert.AreEqual(transitions, state.Transitions);
        }

        [Test]
        public void Output_Property_ReturnsResults()
        {
            var state = new TestState();
            var result = new StateResult<string>("1", "bar");
            state.OutputResults = new List<StateResult<string>> { result };
            Assert.Contains("bar", state.Output);
        }

        [Test]
        public void Input_Property_ReturnsInputs()
        {
            var state = new TestState();
            var process = new StateProcess<int>(state, 99);
            state.InputProcesses = new List<StateProcess<int>> { process };
            Assert.Contains(99, state.Input);
        }

        [Test]
        public async Task _EnterState_StateProcessT_AddsProcessAndCallsEnterState()
        {
            var state = new TestState();
            var process = new StateProcess<int>(state, 123);
            await state._EnterState(process);
            Assert.Contains(123, state.EnteredInputs);
            Assert.Contains(process, state.InputProcesses);
        }

        [Test]
        public async Task _EnterState_StateProcess_AddsProcessAndCallsEnterState()
        {
            var state = new TestState();
            var process = new StateProcess<int>(state, 456);
            await state._EnterState(process);
            Assert.That(state.EnteredInputs, Does.Contain(456));
            Assert.That(state.InputProcesses, Does.Contain(process));
        }

        [Test]
        public async Task _ExitState_ClearsInputProcessesAndCallsExitState()
        {
            var state = new TestState();
            var process = new StateProcess<int>(state, 1);
            state.InputProcesses = new List<StateProcess<int>> { process };
            await state._ExitState();
            Assert.IsEmpty(state.InputProcesses);
            Assert.IsTrue(state.ExitCalled);
        }

        [Test]
        public void _Invoke_ThrowsIfNoInputProcesses()
        {
            var state = new TestState();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await state._Invoke());
        }

        [Test]
        public async Task _Invoke_ProcessesEachInput()
        {
            var state = new TestState();
            var process1 = new StateProcess<int>(state, 10);
            var process2 = new StateProcess<int>(state, 20);
            state.InputProcesses = new List<StateProcess<int>> { process1, process2 };
            var results = await state._Invoke();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(r => r.Result == "Result:10"));
            Assert.IsTrue(results.Any(r => r.Result == "Result:20"));
            Assert.IsTrue(state.WasInvoked);
        }

        [Test]
        public async Task _Invoke_UsesCombineInput()
        {
            var state = new TestState();
            state.CombineInput = true;
            var process = new StateProcess<int>(state, 77);
            state.InputProcesses = new List<StateProcess<int>> { process };
            var results = await state._Invoke();
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Result:77", results[0].Result);
        }

        [Test]
        public void AddTransition_AddsTransition()
        {
            var state = new TestState();
            var nextState = new LambdaState<int, string>((input) => input.ToString());
            bool called = false;
            TransitionEvent<string> evt = (output) => { called = true; return true; };
            state.AddTransition(evt, nextState);
            Assert.AreEqual(1, state.Transitions.Count);
            Assert.AreEqual(nextState, state.Transitions[0].NextState);
        }

        [Test]
        public void AddTransition_WithConversion_AddsTransition()
        {
            var state = new TestState();
            TransitionEvent<string> evt = (output) => true;
            ConversionMethod<string, int> conv = (output) => 1;
            var nextState = new TestState();
            state.AddTransition(evt, conv, nextState);
            Assert.AreEqual(1, state.Transitions.Count);
            Assert.AreEqual(nextState, state.Transitions[0].NextState);
        }

        [Test]
        public void CheckConditions_ReturnsList()
        {
            var state = new TestState();
            var process = new StateProcess<int>(state, 5);
            var result = new StateResult<string>(process.ID, "foo");
            state.InputProcesses = new List<StateProcess<int>> { process };
            state.OutputResults = new List<StateResult<string>> { result };
            var conditions = state.CheckConditions();
            Assert.NotNull(conditions);
            Assert.IsInstanceOf<List<StateProcess>>(conditions);
        }
    }
}
