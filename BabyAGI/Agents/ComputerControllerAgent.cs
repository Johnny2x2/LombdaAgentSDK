using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System.Drawing.Imaging;
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
        public static ModelMessageImageFileContent HandleComputerAction(ComputerToolAction action)
        {
            switch (action.Kind)
            {
                case ModelComputerCallAction.Click:
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
                    ComputerToolUtility.MoveAndDoubleClick(
                        action.MoveCoordinates.X,
                        action.MoveCoordinates.Y);
                    break;
                case ModelComputerCallAction.Drag:
                    ComputerToolUtility.Drag(
                        action.MoveCoordinates.X,
                        action.MoveCoordinates.Y);
                    break;
                case ModelComputerCallAction.KeyPress:
                    ComputerToolUtility.PressKey(action.KeysToPress[0]);
                    break;
                case ModelComputerCallAction.Move:
                    ComputerToolUtility.MoveCursorSmooth(
                        action.MoveCoordinates.X,
                        action.MoveCoordinates.Y);
                    break;
                case ModelComputerCallAction.Screenshot:
                    break;
                case ModelComputerCallAction.Scroll:
                    ComputerToolUtility.Scroll(action.ScrollVertOffset);
                    break;
                case ModelComputerCallAction.Type:
                    ComputerToolUtility.Type(action.TypeText);
                    break;
                case ModelComputerCallAction.Wait:
                    Thread.Sleep(2000);
                    break;
                default:
                    break;
            }

            return CreateScreenShot();
        }

        public static ModelMessageImageFileContent CreateScreenShot()
        {
            byte[] ss = ComputerToolUtility.TakeScreenshotByteArray(ImageFormat.Png);

            GC.Collect();
            // Return the screenshot as a ModelMessageImageFileContent
            return new ModelMessageImageFileContent(BinaryData.FromBytes(ss), "image/png");
        }

    }
}


        