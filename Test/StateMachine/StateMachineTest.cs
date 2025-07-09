using LombdaAgentSDK.StateMachine;

namespace Test
{
    public class StateMachineTest
    {
        public class MockState : BaseState<string, int>
        {
            public override async Task<int> Invoke(string input)
            {
                Assert.That(GetInputType(), Is.EqualTo(typeof(string)));
                
                Assert.That(GetOutputType(), Is.EqualTo(typeof(int)));

                return 3;
            }
        }

        public class ConvertIntToStringState : BaseState<int, string>
        {
            public override async Task<string> Invoke(int input)
            {
                Assert.That(GetInputType(), Is.EqualTo(typeof(int)));

                Assert.That(GetOutputType(), Is.EqualTo(typeof(string)));

                return input.ToString();
            }
        }

        public class ConvertStringToIntState : BaseState<string, int>
        {
            public override async Task<int> Invoke(string input)
            {
                Assert.That(GetInputType(), Is.EqualTo(typeof(string)));
                bool passed = int.TryParse(input, out int output);
                if (!passed) { throw new InvalidCastException($"Cannot Parse {input} into Int"); }
                Assert.That(GetOutputType(), Is.EqualTo(typeof(int)));
                return output;
            }
        }


        public class weird { }
        public class ConvertWeirdToIntState : BaseState<weird, weird>
        {
            public override async Task<weird> Invoke(weird input)
            {
                return input;
            }
        }

        [Test]
        public async Task TestStateTransition()
        {
            MockState state1 = new MockState();

            state1.Input.Add("test input");

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
            state2.AddTransition(_ => true, new ExitState());

            ResultingStateMachine<string, string> stateMachine = new();

            Assert.Throws(typeof(InvalidCastException), () => stateMachine.SetEntryState(weirdState));
            Assert.Throws(typeof(InvalidCastException), () => stateMachine.SetOutputState(weirdState));
            Assert.ThrowsAsync(typeof(InvalidOperationException), async () => await stateMachine.Run("3"));
            stateMachine.SetEntryState(state1);
            Assert.ThrowsAsync(typeof(InvalidOperationException), async () => await stateMachine.Run("3"));
            stateMachine.SetOutputState(state2);

            List<string?> stateResults = await stateMachine.Run("3");

            Assert.That(result, Is.EqualTo(3));
            Assert.That(stateMachine.Results[0], Is.EqualTo("3"));
            Assert.That(stateResults[0], Is.EqualTo("3"));
        }
    }
}
