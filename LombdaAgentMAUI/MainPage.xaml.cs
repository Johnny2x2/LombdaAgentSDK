using System.Collections.ObjectModel;
using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;

namespace LombdaAgentMAUI;

public partial class MainPage : ContentPage
{
    private readonly IAgentApiService _agentApiService;
    private readonly IConfigurationService _configService;
    private readonly ObservableCollection<ChatMessage> _chatMessages;
    private readonly ObservableCollection<string> _agentList;
    private string? _currentAgentId;
    private string? _currentThreadId;
    private CancellationTokenSource? _streamingCancellationTokenSource;

    public MainPage(IAgentApiService agentApiService, IConfigurationService configService)
    {
        InitializeComponent();
        
        _agentApiService = agentApiService;
        _configService = configService;
        _chatMessages = new ObservableCollection<ChatMessage>();
        _agentList = new ObservableCollection<string>();

        // Set up data binding after InitializeComponent
        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            // Load configuration first
            await _configService.LoadSettingsAsync();

            // Find controls and set up bindings
            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            var agentListView = this.FindByName<CollectionView>("AgentListView");

            if (chatCollectionView != null)
                chatCollectionView.ItemsSource = _chatMessages;
            
            if (agentListView != null)
                agentListView.ItemsSource = _agentList;

            await LoadAgentsAsync();
            LogSystemMessage("Application started. Please select or create an agent.");
            
            // Add welcome message with setup instructions if no agents found
            if (_agentList.Count == 0)
            {
                LogSystemMessage("No agents found. Please check your API configuration in Settings.");
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error during page load: {ex.Message}");
        }
    }

    private async Task LoadAgentsAsync()
    {
        try
        {
            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = true;

            LogSystemMessage("Loading agents...");
            
            var agents = await _agentApiService.GetAgentsAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _agentList.Clear();
                var agentPicker = this.FindByName<Picker>("AgentPicker");
                if (agentPicker != null)
                {
                    agentPicker.ItemsSource = null;
                    agentPicker.ItemsSource = agents;
                }
                
                foreach (var agent in agents)
                {
                    _agentList.Add(agent);
                }
                
                LogSystemMessage($"Loaded {agents.Count} agents.");
            });
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error loading agents: {ex.Message}");
        }
        finally
        {
            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = false;
        }
    }

    private async void OnCreateAgentClicked(object? sender, EventArgs e)
    {
        try
        {
            var name = await DisplayPromptAsync("Create Agent", "Enter agent name:", "OK", "Cancel", "Assistant");
            if (string.IsNullOrWhiteSpace(name))
                return;

            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = true;

            LogSystemMessage($"Creating agent '{name}'...");

            var response = await _agentApiService.CreateAgentAsync(name);
            if (response != null)
            {
                LogSystemMessage($"Created agent: {response.Name} (ID: {response.Id})");
                await LoadAgentsAsync();
            }
            else
            {
                LogSystemMessage("Failed to create agent.");
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error creating agent: {ex.Message}");
        }
        finally
        {
            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = false;
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadAgentsAsync();
    }

    private void OnAgentSelected(object? sender, EventArgs e)
    {
        var agentPicker = this.FindByName<Picker>("AgentPicker");
        if (agentPicker?.SelectedItem is string selectedAgentId)
        {
            _currentAgentId = selectedAgentId;
            _currentThreadId = null; // Reset thread for new agent
            _chatMessages.Clear();
            LogSystemMessage($"Selected agent: {selectedAgentId}");
        }
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentAgentId))
        {
            await DisplayAlert("Error", "Please select an agent first.", "OK");
            return;
        }

        var messageEditor = this.FindByName<Editor>("MessageEditor");
        var message = messageEditor?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Error", "Please enter a message.", "OK");
            return;
        }

        try
        {
            // Add user message to chat
            var userMessage = new ChatMessage
            {
                Text = message,
                IsUser = true,
                Timestamp = DateTime.Now
            };
            _chatMessages.Add(userMessage);
            
            if (messageEditor != null)
                messageEditor.Text = string.Empty;

            // Scroll to bottom
            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            if (chatCollectionView != null && _chatMessages.Count > 0)
            {
                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
            }

            // Disable send button during processing
            var sendButton = this.FindByName<Button>("SendButton");
            if (sendButton != null)
            {
                sendButton.IsEnabled = false;
                sendButton.Text = "Sending...";
            }

            var streamingCheckBox = this.FindByName<CheckBox>("StreamingCheckBox");
            if (streamingCheckBox?.IsChecked == true)
            {
                await SendStreamingMessage(message);
            }
            else
            {
                await SendRegularMessage(message);
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error sending message: {ex.Message}");
            await DisplayAlert("Error", $"Failed to send message: {ex.Message}", "OK");
        }
        finally
        {
            var sendButton = this.FindByName<Button>("SendButton");
            if (sendButton != null)
            {
                sendButton.IsEnabled = true;
                sendButton.Text = "Send";
            }
        }
    }

    private async Task SendRegularMessage(string message)
    {
        LogSystemMessage($"Sending message to agent {_currentAgentId}...");

        var response = await _agentApiService.SendMessageAsync(_currentAgentId!, message, _currentThreadId);
        if (response != null)
        {
            _currentThreadId = response.ThreadId;

            var agentMessage = new ChatMessage
            {
                Text = response.Text,
                IsUser = false,
                Timestamp = DateTime.Now
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _chatMessages.Add(agentMessage);
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                if (chatCollectionView != null && _chatMessages.Count > 0)
                {
                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                }
            });

            LogSystemMessage("Response received.");
        }
        else
        {
            LogSystemMessage("Failed to get response from agent.");
        }
    }

    private async Task SendStreamingMessage(string message)
    {
        LogSystemMessage($"Starting streaming message to agent {_currentAgentId}...");

        // Create a placeholder message for streaming
        var agentMessage = new ChatMessage
        {
            Text = "",
            IsUser = false,
            Timestamp = DateTime.Now
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _chatMessages.Add(agentMessage);
        });

        _streamingCancellationTokenSource?.Cancel();
        _streamingCancellationTokenSource = new CancellationTokenSource();

        await _agentApiService.SendMessageStreamAsync(
            _currentAgentId!,
            message,
            _currentThreadId,
            (streamedText) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    agentMessage.Text += streamedText;
                    // Force UI update by removing and re-adding (simple approach)
                    var index = _chatMessages.IndexOf(agentMessage);
                    if (index >= 0)
                    {
                        _chatMessages[index] = new ChatMessage
                        {
                            Text = agentMessage.Text,
                            IsUser = false,
                            Timestamp = agentMessage.Timestamp
                        };
                    }

                    // Auto-scroll to bottom
                    var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                    if (chatCollectionView != null && _chatMessages.Count > 0)
                    {
                        chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: false);
                    }
                });
            },
            _streamingCancellationTokenSource.Token
        );

        LogSystemMessage("Streaming completed.");
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _chatMessages.Clear();
        _currentThreadId = null; // Reset the conversation thread
        LogSystemMessage("Chat cleared. New conversation will start with next message.");
    }

    private void LogSystemMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}";

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var systemLogLabel = this.FindByName<Label>("SystemLogLabel");
            if (systemLogLabel != null)
            {
                systemLogLabel.Text += logMessage + Environment.NewLine;
                
                // Auto-scroll to bottom of logs
                var logScrollView = this.FindByName<ScrollView>("LogScrollView");
                if (logScrollView != null)
                {
                    logScrollView.ScrollToAsync(0, systemLogLabel.Height, false);
                }
            }
        });

        System.Diagnostics.Debug.WriteLine(logMessage);
    }
}
