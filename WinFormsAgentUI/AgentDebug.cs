using BabyAGI;
using Examples.Demos.FunctionGenerator;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System.Data;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace WinFormsAgentUI
{
    public partial class AgentDebug : Form
    {
        private delegate void TextUpdateDelegate(string item);
        /// <summary>
        /// Gets or sets the <see cref="BabyAGIRunner"/> instance that manages the agent's execution.
        /// </summary>
        BabyAGIRunner Agent { get; set; }
        /// <summary>
        /// Gets or sets the most recent response received from the server.
        /// </summary>
        public string LatestResponse { get; set; } = string.Empty;
        /// <summary>
        /// Set a file path to upload an image to the agent
        /// </summary>
        public string loadedFilePath { get; set; } = string.Empty;

        public AgentDebug()
        {
            InitializeComponent();
            // Initialize the agent with a new instance of BabyAGIRunner
            Agent = new BabyAGIRunner();
            //Setup the verbose logging 
            Agent.verboseEvent += Agent_VerboseLog;
            Agent.streamingEvent += Agent_VerboseLog;
            Agent.RootVerboseEvent += Root_VerboseLog;
            Agent.RootStreamingEvent += StreamChat;
            Agent.StateMachineAdded += AddStateWatcher;
            Agent.StateMachineRemoved += RemoveStateWatcher;
            Agent.StartingExecution += Agent_StartingExecution;
            Agent.FinishedExecution += Agent_FinishedExecution;
            Agent.UserInputRequested += UserInputRequest;
        }


        private string UserInputRequest(string prompt)
        {
            // Replace 'this' with your main form instance, e.g. 'MainForm'
            if (this.InvokeRequired)
            {
                // We're on a background thread; marshal to UI thread and return result
                return (string)this.Invoke(new Func<string>(() => UserInputRequest(prompt)));
            }
            else
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    string.IsNullOrEmpty(prompt) ? "Please enter your input:" : prompt,
                    "User Input Request",
                    string.Empty);

                if (string.IsNullOrEmpty(input))
                {
                    throw new ArgumentException("User input cannot be empty.");
                }
                return input;
            }
        }

        /// <summary>
        /// Used to update the UI when the agent is finished executing.
        /// </summary>
        private void Agent_FinishedExecution()
        {
            SendButton.Text = "Send";
        }

        /// <summary>
        /// Initiates the execution process by updating the UI to reflect a cancellable state.
        /// </summary>
        /// <remarks>This method changes the text of the Send button to "Cancel" to indicate that the
        /// execution can be stopped.</remarks>
        private void Agent_StartingExecution()
        {
            SendButton.Text = "Cancel";
        }

        /// <summary>
        /// Adds a state to the process and subscribes to its events if applicable.
        /// </summary>
        /// <remarks>If the state within the <paramref name="stateProcess"/> implements <see
        /// cref="IAgentState"/>,  the method subscribes to its verbose logging and streaming callbacks.</remarks>
        /// <param name="stateProcess">The state process to be added. Must not be null.</param>
        void AddState(StateProcess stateProcess)
        {
            AddListBoxItem(stateProcess.State.GetType().Name);
            if (stateProcess.State is IAgentState agentState)
            {
                agentState.RunningVerboseCallback += Agent_VerboseLog;
                agentState.RunningStreamingCallback += Agent_VerboseLog;
            }
        }

        void RemoveState(BaseState state)
        {
            RemoveListBoxItem(state.GetType().Name);
            if (state is IAgentState agentState)
            {
                agentState.RunningVerboseCallback -= Agent_VerboseLog;
                agentState.RunningStreamingCallback -= Agent_VerboseLog;
            }
        }

        /// <summary>
        /// Add states to the ListBox control in a thread-safe manner.
        /// </summary>
        /// <param name="item"></param>
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

        /// <summary>
        /// Removes the specified item from the list box.
        /// </summary>
        /// <remarks>This method ensures thread safety by checking if the call needs to be marshaled to
        /// the UI thread. If the method is called from a non-UI thread, it uses <see
        /// cref="System.Windows.Forms.Control.Invoke"/>  to perform the operation on the UI thread.</remarks>
        /// <param name="item">The item to be removed from the list box. Must not be null.</param>
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

        /// <summary>
        /// Subscribes to the state machine's state change events to add or remove states from the
        /// </summary>
        /// <param name="stateMachine"></param>
        void AddStateWatcher(StateMachine stateMachine)
        {
            stateMachine.OnStateEntered += AddState;
            stateMachine.OnStateExited += RemoveState;
        }

        /// <summary>
        /// Removes the state watcher from the specified state machine.
        /// </summary>
        /// <remarks>This method detaches event handlers from the state machine's state entry and exit
        /// events, effectively stopping the monitoring of state changes.</remarks>
        /// <param name="stateMachine">The state machine from which the state watcher will be removed. Cannot be null.</param>
        void RemoveStateWatcher(StateMachine stateMachine)
        {
            stateMachine.OnStateEntered -= AddState;
            stateMachine.OnStateExited -= RemoveState;
        }

        /// <summary>
        /// dates the UI with verbose log messages from the agent.
        /// </summary>
        /// <param name="e"></param>
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

        /// <summary>
        /// Logs a verbose message to the system's rich text box, ensuring thread-safe operations.
        /// </summary>
        /// <remarks>This method checks if the current thread is different from the UI thread and uses
        /// <see cref="System.Windows.Forms.Control.Invoke"/> to perform the logging operation on the UI thread if
        /// necessary. This ensures that UI updates are performed safely across threads.</remarks>
        /// <param name="e">The message to be logged. Cannot be null or empty.</param>
        void Root_VerboseLog(string e)
        {
            if (SystemRichTextBox.InvokeRequired) // Check if invoking is required
            {
                // If on a different thread, use Invoke to call this method on the UI thread
                SystemRichTextBox.Invoke(new TextUpdateDelegate(Root_VerboseLog), e);
            }
            else
            {
                SystemRichTextBox.AppendText("[Control Agent]: " + e + Environment.NewLine);
            }
        }

        /// <summary>
        /// Handles the click event of the Send button, initiating or canceling a request.
        /// </summary>
        /// <remarks>If the button text is "Send", a request is initiated. Otherwise, the ongoing request
        /// is canceled.</remarks>
        /// <param name="sender">The source of the event, typically the Send button.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private async void SendButton_Click(object sender, EventArgs e)
        {
            if (SendButton.Text == "Send") { await SendRequest(); } else { Agent.CancelExecution(); }
        }

        /// <summary>
        /// Sends a user input request to the assistant and processes the response asynchronously.
        /// </summary>
        /// <remarks>This method retrieves the user's input from the input text box, adds it to the chat
        /// display, and initiates the assistant's response. The input text is trimmed of leading and trailing
        /// whitespace before being processed. The chat display is updated to include the user's input and a placeholder
        /// for the assistant's response.</remarks>
        /// <returns></returns>
        private async Task SendRequest()
        {
            var text = InputRichTextBox.Text.Trim(); // Get the text from the input box and trim it
            AddToChat("User", text); // Add the user input to the chat display
            InputRichTextBox.Clear(); // Clear the input box for the next message
            ChatRichTextBox.AppendText($"\n[Assistant]: "); // Add a placeholder for the assistant's response in the chat display
            await StartAssistantResponse(text); // Start the assistant's response asynchronously
        }

        /// <summary>
        /// Initiates a response from the assistant by adding the specified text to the conversation.
        /// </summary>
        /// <remarks>If an image file is loaded, the method adds the image to the conversation along with
        /// the text. Otherwise, it adds only the text. The method clears the loaded file path after
        /// processing.</remarks>
        /// <param name="text">The text to be added to the conversation. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response from the assistant
        /// as a string.</returns>
        private async Task<string> StartAssistantResponse(string text)
        {
            string result = string.Empty;

            // Check if a file is loaded for image processing
            if (!string.IsNullOrEmpty(loadedFilePath))
            {
                result = await Agent.AddImageToConversation(text, loadedFilePath, streaming: true);
                loadedFilePath = string.Empty;
            }
            else
            {
                result = await Agent.AddToConversation(text, streaming: true);
            }

            return result;
        }

        /// <summary>
        /// Appends a message to the chat display with a specified role label.
        /// </summary>
        /// <remarks>The message is appended to the chat display in a new line, prefixed by the role
        /// label.</remarks>
        /// <param name="role">The role of the sender, such as "User" or "System".</param>
        /// <param name="message">The message content to be added to the chat.</param>
        public void AddToChat(string role, string message)
        {
            ChatRichTextBox.AppendText($"\n[{role}]: " + message + Environment.NewLine);
        }

        /// <summary>
        /// Streams a chat message to the chat display.
        /// </summary>
        /// <remarks>This method ensures that the message is appended to the chat display on the correct
        /// UI thread. If the method is called from a non-UI thread, it will invoke the append operation on the UI
        /// thread.</remarks>
        /// <param name="message">The message to be displayed in the chat. Cannot be null or empty.</param>
        public void StreamChat(string message)
        {
            if (ChatRichTextBox.InvokeRequired)
            {
                ChatRichTextBox.Invoke(new Action(() => AppendToChat(message)));
            }
            else
            {
                AppendToChat(message);
            }
        }

        /// <summary>
        /// Appends a message to the chat display.
        /// </summary>
        /// <remarks>The chat display will automatically scroll to show the newly appended
        /// message.</remarks>
        /// <param name="message">The message to append to the chat. Cannot be null or empty.</param>
        private void AppendToChat(string message)
        {
            ChatRichTextBox.AppendText(message);
            ChatRichTextBox.ScrollToCaret();
        }

        /// <summary>
        /// Handles the click event of the Add File button, allowing the user to select a file to upload.
        /// </summary>
        /// <remarks>This method opens a file dialog for the user to select a file. If a file is selected,
        /// its path is stored.</remarks>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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

        private void SystemRichTextBox_TextChanged(object sender, EventArgs e)
        {
            SystemRichTextBox.SelectionStart = SystemRichTextBox.Text.Length; // Scroll to the end of the text
            SystemRichTextBox.ScrollToCaret();
        }
    }
}
