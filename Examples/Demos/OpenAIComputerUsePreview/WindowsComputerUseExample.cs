using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using NUnit.Framework.Internal.Execution;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Examples.Demos.OpenAIComputerUsePreview
{
    public class Program
    {
        public static void Main()
        {
            Task runner = Task.Run(async () => await StartDemo());

            Task.WaitAll(runner);
        }

        public static async Task StartDemo()
        {
#pragma warning disable computerPreview // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            WindowsComputerUseExample computerUse = new();
#pragma warning restore computerPreview // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            RunResult result = new();

            string task = Console.ReadLine();

            while (!task.Equals("EXIT()"))
            {
                result = await computerUse.ComputerPreviewRun(task, result);
                task = Console.ReadLine();
            }
        }
    }

    [Experimental("computerPreview")]
    public class WindowsComputerUseExample
    {
        //[Test]
        public async Task RunTest()
        {
            OpenAIModelClient openAIModelClient = new OpenAIModelClient("computer-use-preview", enableComputerCalls: true);

            Agent agent = new Agent(
                openAIModelClient,
                "Assistant",
                "You are a useful assistant that controls a windows computer to complete the users task."
                );

            RunResult result = await Runner.RunAsync(agent, "Can you find and open blender from my desktop dont ask just do?", computerUseCallback: HandleComputerAction);
        }

        public async Task<RunResult> ComputerPreviewRun(string task, RunResult? previousResult = null)
        {
            OpenAIModelClient openAIModelClient = new OpenAIModelClient("computer-use-preview", enableComputerCalls: true);

            Agent agent = new Agent(
                openAIModelClient,
                "Assistant",
                "You are a useful assistant that controls a windows computer to complete the users task."
                );

            return await Runner.RunAsync(agent, task, computerUseCallback: HandleComputerAction, messages: previousResult?.Messages);
        }

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
                    Console.WriteLine($"[Computer Call Action]({action}) {action.KeysToPress.ToArray()}");
                    ComputerToolUtility.Type(action.KeysToPress);
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
