using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace BabyAGI.Agents
{
    public class ComputerControllerAgent
    {
        BabyAGIRunner MainRunner { get; set; }

        public ComputerControllerAgent( BabyAGIRunner MainRunner ) => this.MainRunner = MainRunner;

        //requires Teir 3 account & access to use computer-use-preview currently
        [Tool(Description = "Use this agent to accomplish task that require computer input or output like mouse movement, clicking, screen shots", In_parameters_description = ["The task you wish to accomplish."])]
        public async Task<string> ControlComputer(string task)
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
                input:task, 
                verboseCallback:Console.WriteLine, 
                computerUseCallback: HandleComputerAction,
                responseID:MainRunner.MainThreadId,
                maxTurns:50
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
