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
    }
}
