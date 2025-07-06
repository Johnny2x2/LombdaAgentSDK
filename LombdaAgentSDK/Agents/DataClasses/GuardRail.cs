namespace LombdaAgentSDK.Agents.DataClasses
{
    public delegate Task<GuardRailFunctionOutput> GuardRailFunction(string? input = "");

    //Used to check the input or output of a message to see if it meets certain criteria.
    //Triggers the runner to stop processing if the criteria are not met and tripwire is triggered.
    public class GuardRailFunctionOutput
    {
        public string OutputInfo { get; set; }
        public bool TripwireTriggered { get; set; }

        public GuardRailFunctionOutput(string outputInfo = "", bool tripwireTriggered = false)
        {
            OutputInfo = outputInfo;
            TripwireTriggered = tripwireTriggered;
        }
    }
}
