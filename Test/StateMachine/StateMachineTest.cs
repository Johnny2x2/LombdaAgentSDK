using LombdaAgentSDK.StateMachine;

namespace Test
{
    public class StateMachineTest
    {
        public class MockState : BaseState<string, int>
        {
            public override async Task<int> Invoke()
            {
                Assert.That(Input.GetType(), Is.EqualTo(typeof(string)));
                Output = 3;
                Assert.That(Output.GetType(), Is.EqualTo(typeof(int)));
                return Output;
            }
        }

        public class ConvertIntToStringState : BaseState<int, string>
        {
            public override async Task<string> Invoke()
            {
                Assert.That(Input.GetType(), Is.EqualTo(typeof(int)));
                Output = Input.ToString();
                Assert.That(Output.GetType(), Is.EqualTo(typeof(string)));
                return Output;
            }
        }

        public class ConvertStringToIntState : BaseState<string, int>
        {
            public override async Task<int> Invoke()
            {
                Assert.That(Input.GetType(), Is.EqualTo(typeof(string)));
                bool passed = int.TryParse(Input, out int output);
                if (!passed) { throw new InvalidCastException($"Cannot Parse {Input} into Int"); }
                Output = output;
                Assert.That(Output.GetType(), Is.EqualTo(typeof(int)));
                return Output;
            }
        }
        public class weird { }
        public class ConvertWeirdToIntState : BaseState<weird, weird>
        {
            public override async Task<weird> Invoke()
            {
                return Output;
            }
        }

        [Test]
        public async Task TestStateTransition()
        {
            MockState state1 = new MockState();

            state1.Input = "test input";

            int result = 0;

            state1.Transitions?.Add(new StateTransition<int>((output) => { Console.WriteLine($"Result was {output}"); result = output; return true; }, new MockState()));

            StateMachine stateMachine = new();

            stateMachine.Run(state1);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public async Task TestStateTransitionWithResults()
        {
            ConvertStringToIntState state1 = new();
            ConvertIntToStringState state2 = new();
            ConvertWeirdToIntState weirdState = new();
            int result = 0;

            state1.AddTransition((output) => { Console.WriteLine($"Result was {output}"); result = output; return true; }, state2);

            ResultingStateMachine<string, string> stateMachine = new();

            Assert.Throws(typeof(InvalidCastException), () => stateMachine.SetEntryState(weirdState));
            Assert.Throws(typeof(InvalidCastException), () => stateMachine.SetOutputState(weirdState));
            Assert.ThrowsAsync(typeof(InvalidOperationException), async () => await stateMachine.Run("3"));
            stateMachine.SetEntryState(state1);
            Assert.ThrowsAsync(typeof(InvalidOperationException), async () => await stateMachine.Run("3"));
            stateMachine.SetOutputState(state2);

            string? stateResult = await stateMachine.Run("3");

            Assert.That(result, Is.EqualTo(3));
            Assert.That(stateMachine.Result, Is.EqualTo("3"));
            Assert.That(stateResult, Is.EqualTo("3"));
        }
    }
}
