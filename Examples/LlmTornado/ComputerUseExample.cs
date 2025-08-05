using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System.Drawing.Imaging;

namespace Examples.LlmTornado
{
    public class LTComputerUseExample
    {
        //requires Teir 3 account & access to use computer-use-preview currently
        [Test]
        public async Task Run()
        {
            LLMTornadoModelProvider client =
                new(ChatModel.OpenAi.Codex.ComputerUsePreview,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],
                allowComputerUse:true,
                useResponseAPI:true);


            Agent agent = new Agent(
                client, 
                "Assistant",
                "You are a useful assistant that controls a computer to complete the users task. THIS IS FOR A TEST DO NOT ASK FOR PERMISSION TO PERFORM COMPUTER ACTIONS."
                );
            
            //Runner needs to return callbacks for Computer Action
            RunResult result = await Runner.RunAsync(
                agent, 
                input:"Can you find and open blender from my desktop?", 
                verboseCallback:Console.WriteLine, 
                computerUseCallback: HandleComputerAction
                );
        }

        public static ModelMessageImageFileContent HandleComputerAction(ComputerToolAction action)
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
