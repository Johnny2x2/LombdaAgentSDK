using BabyAGI;
using Examples.Demos.FunctionGenerator;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System.Data;
using static System.Net.Mime.MediaTypeNames;

namespace WinFormsAgentUI
{
    public partial class AgentDebug : Form
    {
        private delegate void TextUpdateDelegate(string item);
        BabyAGIRunner Agent { get; set; }
        public string LatestResponse { get; set; } = string.Empty;
        public string loadedFilePath { get; set; } = string.Empty;

        public AgentDebug()
        {
            InitializeComponent();
            Agent = new BabyAGIRunner();
            Agent.verboseEvent += Agent_VerboseLog;
            Agent.streamingEvent += Agent_VerboseLog;
            Agent.RootVerboseEvent += Root_VerboseLog;
            Agent.RootStreamingEvent += StreamChat;
            Agent.StateMachineAdded += AddStateWatcher;
            Agent.StateMachineRemoved += RemoveStateWatcher;
            Agent.StartingExecution += Agent_StartingExecution;
            Agent.FinishedExecution += Agent_FinishedExecution;
        }

        private void Agent_FinishedExecution()
        {
            SendButton.Text = "Send";
        }

        private void Agent_StartingExecution()
        {
            SendButton.Text = "Cancel";
        }

        void AddState(StateProcess stateProcess)
        {
            AddListBoxItem(stateProcess.State.GetType().Name);
            if(stateProcess.State is IAgentState agentState)
            {
                agentState.RunningVerboseCallback += Agent_VerboseLog;
                agentState.RunningStreamingCallback += StreamChat;
            }
        }

        void RemoveState(BaseState state)
        {
            RemoveListBoxItem(state.GetType().Name);
            if (state is IAgentState agentState)
            {
                agentState.RunningVerboseCallback -= Agent_VerboseLog;
                agentState.RunningStreamingCallback -= StreamChat;
            }
        }

        private void AddListBoxItem(string item)
        {
            if (listBox1.InvokeRequired) // Check if invoking is required
            {
                // If on a different thread, use Invoke to call this method on the UI thread
                listBox1.Invoke(new TextUpdateDelegate(AddListBoxItem), item);
            }
            else
            {
                // If on the UI thread, directly add the item
                listBox1.Items.Add(item);
            }
        }

        private void RemoveListBoxItem(string item)
        {
            if (listBox1.InvokeRequired) // Check if invoking is required
            {
                // If on a different thread, use Invoke to call this method on the UI thread
                listBox1.Invoke(new TextUpdateDelegate(RemoveListBoxItem), item);
            }
            else
            {
                // If on the UI thread, directly add the item
                listBox1.Items.Remove(item);
            }
        }

        void AddStateWatcher(StateMachine stateMachine)
        {
            stateMachine.OnStateEntered += AddState;
            stateMachine.OnStateExited += RemoveState;
        }

        void RemoveStateWatcher(StateMachine stateMachine)
        {
            stateMachine.OnStateEntered -= AddState;
            stateMachine.OnStateExited -= RemoveState;
        }

        void Agent_VerboseLog(string e)
        {
            if (SystemRichTextBox.InvokeRequired) // Check if invoking is required
            {
                // If on a different thread, use Invoke to call this method on the UI thread
                SystemRichTextBox.Invoke(new TextUpdateDelegate(Agent_VerboseLog), e);
                return;
            }
            else
            {
                SystemRichTextBox.AppendText(e + Environment.NewLine);
            }  
        }

        void Root_VerboseLog(string e)
        {
            if (SystemRichTextBox.InvokeRequired) // Check if invoking is required
            {
                // If on a different thread, use Invoke to call this method on the UI thread
                SystemRichTextBox.Invoke(new TextUpdateDelegate(Root_VerboseLog), e);
                return;
            }
            else
            {
                SystemRichTextBox.AppendText("[Control Agent]: " + e + Environment.NewLine);
            }
        }

        private async void SendButton_Click(object sender, EventArgs e)
        {

            if (SendButton.Text == "Send") { SendRequest(); } else { Agent.CancelExecution(); }
        }

        private async Task SendRequest()
        {
            var text = InputRichTextBox.Text.Trim();
            AddToChat("User", text);
            InputRichTextBox.Clear();
            ChatRichTextBox.AppendText($"\n[Assistant]: ");
            await StartAssistantResponse(text);
        }

        private async Task<string> StartAssistantResponse(string text)
        {
            string result = string.Empty;   

            if (!string.IsNullOrEmpty(loadedFilePath))
            {
                result = await Agent.AddFileToConversation(text, loadedFilePath, streaming: true);
                loadedFilePath = string.Empty;
            }
            else
            {
                result = await Agent.AddToConversation(text, streaming: true);
            }
                
            return result;
        }

        public void AddToChat(string role, string message)
        {
            ChatRichTextBox.AppendText($"\n[{role}]: " + message + Environment.NewLine);
        }

        public void StreamChat(string message)
        {
            if(ChatRichTextBox.InvokeRequired)
            {
                ChatRichTextBox.Invoke(new Action(() => AppendToChat(message)));
            }
            else
            {
                AppendToChat(message);
            }
            
        }

        private void AppendToChat(string message)
        {
            ChatRichTextBox.AppendText(message);
            ChatRichTextBox.ScrollToCaret();
        }

        private void AddFileButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a file to upload",
            };

            //Filter = "All files" + "|*.*|*.jpg|*.png|*.gif|*.txt|*.md|*.json|*.csv|*.pdf|*.docx|*.pptx|*.html|*.css|.xml|",

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                loadedFilePath = openFileDialog.FileName;
             }
        }
    }
}
