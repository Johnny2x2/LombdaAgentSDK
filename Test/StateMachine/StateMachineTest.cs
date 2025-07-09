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

        public class IntPlus3State : BaseState<int, int>
        {
            public override async Task<int> Invoke(int input)
            {
                return input + 3;
            }
        }

        public class IntPlus4State : BaseState<int, int>
        {
            public override async Task<int> Invoke(int input)
            {
                return input + 4;
            }
        }

        public class SummingState : BaseState<int, int>
        {
            public SummingState()
            {
                CombineInput = true;
            }

            public override async Task<int> Invoke(int input)
            {
                return Input.Sum();
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

            int result = 0;

            state1.AddTransition((output) => { Console.WriteLine($"Result was {output}"); result = output; return true; }, new ExitState());
            
            StateMachine stateMachine = new();

            await stateMachine.Run(state1, "test input");

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

        [Test]
        public async Task TestStateTransitionWithResultsParallel()
        {
            ConvertStringToIntState inputState = new()
            {
                AllowsParallelTransitions = true
            };

            IntPlus3State state3 = new();
            IntPlus4State state4 = new();
            SummingState summingState = new();
            ConvertIntToStringState resultState = new();

            //should happen in parallel and get result
            inputState.AddTransition(_=> true, state3);
            inputState.AddTransition(_ => true, state4);

            //summing State should Have 2 Inputs now
            state3.AddTransition(_ => true, summingState);
            state4.AddTransition(_ => true, summingState);

            summingState.AddTransition(_ => true, resultState);

            resultState.AddTransition(_ => true, new ExitState());

            ResultingStateMachine<string, string> stateMachine = new();

            stateMachine.SetEntryState(inputState);
            stateMachine.SetOutputState(resultState);

            List<string?> stateResults = await stateMachine.Run("3");

            Console.WriteLine(stateResults[0]);
        }
    }
}
