using BabyAGI;
using Examples.Demos.FunctionGenerator;
using LombdaAgentSDK.StateMachine;

namespace WinFormsAgentUI
{
    public partial class AgentDebug : Form
    {
        BabyAGIRunner Agent { get; set; }
        public AgentDebug()
        {
            InitializeComponent();
            Agent = new BabyAGIRunner();
            Agent.StateMachineAdded += Agent_StateMachineAdded;
        }

        void Agent_StateMachineAdded(StateMachine sm)
        {
            Agent.ControlAgentVerboseCallback += Agent_VerboseLog;
        }

        void Agent_VerboseLog(string e)
        {
            SystemRichTextBox.AppendText(e + Environment.NewLine);
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            List<Task> tasks = new();
            Task.Run(async () => await Agent.StartNewConversation(InputRichTextBox.Text, streaming: true));
            Task.WhenAll(tasks);
        }

        public void AddToChat(string role, string message)
        {
            ChatRichTextBox.AppendText($"[{role}]: "+message + Environment.NewLine);
        }

    }
}
