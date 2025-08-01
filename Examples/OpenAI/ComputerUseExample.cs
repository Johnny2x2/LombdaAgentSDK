using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System.Drawing.Imaging;

namespace Examples.OpenAI
{
    public class ComputerUseExample
    {
        //requires Teir 3 account & access to use computer-use-preview currently
        [Test]

        public async Task Run()
        {
            OpenAIModelClient openAIModelClient = new OpenAIModelClient("computer-use-preview", enableComputerCalls:true);

            Agent agent = new Agent(
                openAIModelClient, 
                "Assistant",
                "You are a useful assistant that controls a computer to complete the users task."
                );
            
            //Runner needs to return callbacks for Computer Action
            RunResult result = await Runner.RunAsync(
                agent, 
                input:"open blender?", 
                verboseCallback:Console.WriteLine, 
                computerUseCallback: HandleComputerAction
                );

            Assert.That(result.Text, Is.Not.Empty, "RunResult should not be null");
        }

        //This is called first before Screen Shot is taken.
        public static ModelMessageImageFileContent HandleComputerAction(ComputerToolAction action)
        {
            switch (action.Kind)
            {
                case ModelComputerCallAction.Click:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MouseButtonClick}");
                    break;
                case ModelComputerCallAction.DoubleClick:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
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
                    break;
                case ModelComputerCallAction.Screenshot:
                    Console.WriteLine($"[Computer Call Action]({action})");
                    break;
                case ModelComputerCallAction.Scroll:
                    Console.WriteLine($"[Computer Call Action]({action}) {action.MoveCoordinates}");
                    Console.WriteLine($"[Computer Call Horizontal Offset Value]({action}) {action.ScrollHorOffset}");
                    Console.WriteLine($"[Computer Call Vertical Offset Valu]({action}) {action.ScrollVertOffset}");
                    break;
                case ModelComputerCallAction.Type:
                    Console.WriteLine($"[Computer Call Action TypeText Value]({action}) {action.TypeText}");
                    break;
                case ModelComputerCallAction.Wait:
                    Console.WriteLine($"[Computer Call Action]({action})");
                    Thread.Sleep(1000);
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
