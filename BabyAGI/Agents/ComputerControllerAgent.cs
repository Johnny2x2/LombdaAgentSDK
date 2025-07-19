using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System.Threading.Tasks;
using static LombdaAgentSDK.Runner;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace BabyAGI.Agents
{
    public class ComputerControllerAgent
    {
        public string ResponseID { get; set; } = string.Empty;  
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        public RunnerVerboseCallbacks runnerVerboseCallbacks { get; set; }

        public ComputerControllerAgent(LombdaAgent MainRunner ) 
        {
            if(MainRunner.VerboseCallback is not null)
            {
                this.runnerVerboseCallbacks = MainRunner?.VerboseCallback;
            }
            this.CancellationTokenSource = MainRunner.CancellationTokenSource;
        } 

        public ComputerControllerAgent(string responseId = "", CancellationTokenSource? cancellationTokenSource = null, RunnerVerboseCallbacks? verboseCallback = null)
        {
            this.CancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
            if(verboseCallback is not null)
            {
                this.runnerVerboseCallbacks = verboseCallback;
            }

            if(!string.IsNullOrEmpty(responseId))
            {
                this.ResponseID = responseId;
            }
        }

        public async Task<string> RunComputerAgent(string task)
        {

            LLMTornadoModelProvider client =
                new(ChatModel.OpenAi.Codex.ComputerUsePreview,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],
                allowComputerUse: true,
                useResponseAPI: true);


            Agent agent = new Agent(
                client,
                "Assistant",
                "You are a useful assistant that controls a computer to complete the users task. After the task is complete please report what you did and the state of the PC."
                );

            //Runner needs to return callbacks for Computer Action
            RunResult result = await Runner.RunAsync(
                agent,
                input: task,
                verboseCallback: runnerVerboseCallbacks,
                computerUseCallback: HandleComputerAction,
                responseID: ResponseID,
                maxTurns: 50,
                cancellationToken: CancellationTokenSource
                );

            return result.Text ?? "Task Finished";
        }

        //This is called first before Screen Shot is taken.
        public static void HandleComputerAction(ComputerToolAction action)
        {
            switch (action.Kind)
            {
                case ModelComputerCallAction.Click:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MouseButtonClick}");
                    switch (action.MouseButtonClick)
                    {
                        case MouseButtons.Left:
                            ComputerToolUtility.MoveAndClick(
                            action.MoveCoordinates.X,
                            action.MoveCoordinates.Y
                            ); break;
                        case MouseButtons.Right:
                            ComputerToolUtility.MoveAndRightClick(
                            action.MoveCoordinates.X,
                            action.MoveCoordinates.Y
                            ); break;
                        case MouseButtons.Middle: ComputerToolUtility.MiddleClick(); break;
                        default:
                            break;
                    }
                    break;
                case ModelComputerCallAction.DoubleClick:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    ComputerToolUtility.MoveAndDoubleClick(
                        action.MoveCoordinates.X,
                        action.MoveCoordinates.Y);
                    break;
                case ModelComputerCallAction.Drag:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.StartDragLocation}");
                    break;
                case ModelComputerCallAction.KeyPress:
                    Console.WriteLine($"[Computer Call Action]({action}) {string.Join(",",action.KeysToPress.ToArray())}");
                    ComputerToolUtility.PressKey(action.KeysToPress[0]);
                    break;
                case ModelComputerCallAction.Move:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    if (action.MoveCoordinates != null)
                    {
                        ComputerToolUtility.MoveCursorSmooth(action.MoveCoordinates.X,
                            action.MoveCoordinates.Y);
                    }
                    break;
                case ModelComputerCallAction.Screenshot:
                    Console.WriteLine($"[Computer Call Action]({action})");
                    break;
                case ModelComputerCallAction.Scroll:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    Console.WriteLine($"[Computer Call Horizontal Offset Value]({action}) {action.ScrollHorOffset}");
                    Console.WriteLine($"[Computer Call Vertical Offset Valu]({action}) {action.ScrollVertOffset}");
                    ComputerToolUtility.Scroll(action.ScrollVertOffset);
                    break;
                case ModelComputerCallAction.Type:
                    Console.WriteLine($"[Computer Call Action TypeText Value]({action}) {action.TypeText}");
                    ComputerToolUtility.Type(action.TypeText);
                    break;
                case ModelComputerCallAction.Wait:
                    Console.WriteLine($"[Computer Call Action]({action})");
                    Thread.Sleep(2000);
                    break;
                default:
                    break;
            }
        }

    }
}
