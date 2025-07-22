using System.Collections.ObjectModel;
using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;

namespace LombdaAgentMAUI;

public partial class MainPage : ContentPage
{
    private readonly IAgentApiService _agentApiService;
    private readonly IConfigurationService _configService;
    private readonly ISessionManagerService _sessionManager;
    private readonly ObservableCollection<ChatMessage> _chatMessages;
    private readonly ObservableCollection<string> _agentList;
    private readonly ObservableCollection<string> _agentTypes;
    private string? _currentAgentId;
    private string? _currentThreadId;
    private string? _currentResponseId; // Track last response ID for API continuity
    private CancellationTokenSource? _streamingCancellationTokenSource;

    public MainPage(IAgentApiService agentApiService, IConfigurationService configService, ISessionManagerService sessionManager)
    {
        InitializeComponent();
        
        _agentApiService = agentApiService;
        _configService = configService;
        _sessionManager = sessionManager;
        _chatMessages = new ObservableCollection<ChatMessage>();
        _agentList = new ObservableCollection<string>();
        _agentTypes = new ObservableCollection<string>();

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
            await LoadAgentTypesAsync();
            
            // Try to restore the last selected agent after loading agents
            await RestoreLastSelectedAgentAsync();
            
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

    private async Task LoadAgentTypesAsync()
    {
        try
        {
            LogSystemMessage("Loading agent types...");
            
            var agentTypes = await _agentApiService.GetAgentTypesAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _agentTypes.Clear();
                foreach (var agentType in agentTypes)
                {
                    _agentTypes.Add(agentType);
                }
                
                LogSystemMessage($"Loaded {agentTypes.Count} agent types: {string.Join(", ", agentTypes)}");
            });
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error loading agent types: {ex.Message}");
        }
    }

    private async void OnCreateAgentClicked(object? sender, EventArgs e)
    {
        try
        {
            // First, check if we have agent types loaded
            if (_agentTypes.Count == 0)
            {
                LogSystemMessage("No agent types available. Refreshing agent types...");
                await LoadAgentTypesAsync();
                
                if (_agentTypes.Count == 0)
                {
                    await DisplayAlert("Error", "No agent types available. Please check your API connection.", "OK");
                    return;
                }
            }

            // First, show agent type selection
            string selectedAgentType = "Default";
            if (_agentTypes.Count > 1)
            {
                var typeOptions = _agentTypes.ToArray();
                selectedAgentType = await DisplayActionSheet("Select Agent Type", "Cancel", null, typeOptions);
                
                if (selectedAgentType == "Cancel" || string.IsNullOrEmpty(selectedAgentType))
                    return;
            }
            else if (_agentTypes.Count == 1)
            {
                selectedAgentType = _agentTypes.First();
            }

            // Then, get the agent name
            var name = await DisplayPromptAsync("Create Agent", $"Enter name for {selectedAgentType} agent:", "Create", "Cancel", "Assistant");
            if (string.IsNullOrWhiteSpace(name))
                return;

            var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
            if (loadingOverlay != null)
                loadingOverlay.IsVisible = true;

            LogSystemMessage($"Creating agent '{name}' of type '{selectedAgentType}'...");

            var response = await _agentApiService.CreateAgentAsync(name, selectedAgentType);
            if (response != null)
            {
                LogSystemMessage($"Created agent: {response.Name} (ID: {response.Id}, Type: {selectedAgentType})");
                await LoadAgentsAsync();
            }
            else
            {
                LogSystemMessage($"Failed to create agent. Check if agent type '{selectedAgentType}' is valid.");
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
        await LoadAgentTypesAsync();
    }

    private void OnAgentSelected(object? sender, EventArgs e)
    {
        var agentPicker = this.FindByName<Picker>("AgentPicker");
        if (agentPicker?.SelectedItem is string selectedAgentId)
        {
            SelectAgent(selectedAgentId);
            
            // Sync the list view selection
            var agentListView = this.FindByName<CollectionView>("AgentListView");
            if (agentListView != null)
            {
                var index = _agentList.IndexOf(selectedAgentId);
                if (index >= 0)
                {
                    agentListView.SelectedItem = selectedAgentId;
                }
            }
        }
    }

    // Event handler for when an agent is selected from the agent list
    private void OnAgentListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is string selectedAgentId)
        {
            SelectAgent(selectedAgentId);
            
            // Sync the picker selection
            var agentPicker = this.FindByName<Picker>("AgentPicker");
            if (agentPicker != null)
            {
                agentPicker.SelectedItem = selectedAgentId;
            }
        }
    }

    // Common method to handle agent selection from both controls
    private async void SelectAgent(string agentId)
    {
        try
        {
            // Save current session before switching if there's an active agent
            if (!string.IsNullOrEmpty(_currentAgentId))
            {
                await SaveCurrentSessionAsync();
            }

            _currentAgentId = agentId;
            
            // Update the current agent label
            var currentAgentLabel = this.FindByName<Label>("CurrentAgentLabel");
            if (currentAgentLabel != null)
            {
                currentAgentLabel.Text = "Loading agent details...";
            }
            
            LogSystemMessage($"Selected agent: {agentId}");
            
            // Load session for the selected agent
            await LoadAgentSessionAsync(agentId);
            
            // Save this as the last selected agent
            await _sessionManager.SaveLastSelectedAgentIdAsync(agentId);
            
            // Try to fetch agent details for better display
            try
            {
                var agentDetails = await _agentApiService.GetAgentAsync(agentId);
                if (agentDetails != null && currentAgentLabel != null)
                {
                    currentAgentLabel.Text = $"{agentDetails.Name} (ID: {agentId})";
                    LogSystemMessage($"Loaded agent details: {agentDetails.Name}");
                }
                else if (currentAgentLabel != null)
                {
                    currentAgentLabel.Text = $"Agent ID: {agentId}";
                }
            }
            catch (Exception ex)
            {
                LogSystemMessage($"Could not load agent details: {ex.Message}");
                if (currentAgentLabel != null)
                {
                    currentAgentLabel.Text = $"Agent ID: {agentId}";
                }
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error selecting agent: {ex.Message}");
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
            var userMessage = new ChatMessage
            {
                Text = message,
                IsUser = true,
                Timestamp = DateTime.Now
            };
            _chatMessages.Add(userMessage);
            
            // Save session after adding user message
            await SaveCurrentSessionAsync();
            
            if (messageEditor != null)
                messageEditor.Text = string.Empty;

            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            if (chatCollectionView != null && _chatMessages.Count > 0)
            {
                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
            }

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
            LogSystemMessage($"Response received - ThreadId: {response.ThreadId}");
            LogSystemMessage($"Response text length: {response.Text?.Length ?? 0}");

            _currentThreadId = response.ThreadId;
            // Note: Regular messages don't typically return response IDs like streaming API

            var agentMessage = new ChatMessage
            {
                Text = response.Text ?? "[No response text received]",
                IsUser = false,
                Timestamp = DateTime.Now
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _chatMessages.Add(agentMessage);
                LogSystemMessage($"Added agent message to chat. Total messages: {_chatMessages.Count}");
                
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                if (chatCollectionView != null && _chatMessages.Count > 0)
                {
                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                }
            });

            // Save session after receiving response
            await SaveCurrentSessionAsync();

            LogSystemMessage("Response processing completed.");
        }
        else
        {
            LogSystemMessage("Failed to get response from agent - response was null.");
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var errorMessage = new ChatMessage
                {
                    Text = "❌ Failed to get response from agent. Please try again.",
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                _chatMessages.Add(errorMessage);
            });
            
            // Save session even with error message
            await SaveCurrentSessionAsync();
        }
    }

    private async Task SendStreamingMessage(string message)
    {
        LogSystemMessage($"🚀 Starting streaming message to agent {_currentAgentId}...");

        var agentMessage = new ChatMessage
        {
            Text = "🤖 Initializing...",
            IsUser = false,
            Timestamp = DateTime.Now
        };

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _chatMessages.Add(agentMessage);
            LogSystemMessage("✅ Added placeholder message to chat");
        });

        _streamingCancellationTokenSource?.Cancel();
        _streamingCancellationTokenSource = new CancellationTokenSource();
        _streamingCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

        var streamedContent = "";
        var hasReceivedContent = false;
        var eventCount = 0;
        var updateCount = 0;
        var startTime = DateTime.Now;
        
        var messageLock = new object();
        var hasReceivedFirstDelta = false;

        // Run the streaming operation on a background task to prevent UI blocking
        try
        {
            LogSystemMessage("🔄 Starting streaming request...");
            
            // Use Task.Run to execute streaming on background thread
            await Task.Run(async () =>
            {
                try
                {
                    var resultThreadId = await _agentApiService.SendMessageStreamWithEventsAsync(
                        _currentAgentId!,
                        message,
                        _currentThreadId,
                        // Text callback - runs on background thread
                        (streamedText) =>
                        {
                            lock (messageLock)
                            {
                                hasReceivedContent = true;
                                streamedContent += streamedText;
                                updateCount++;
                                hasReceivedFirstDelta = true;
                            }
                            
                            // Only log every 10th chunk to reduce log spam
                            if (updateCount % 10 == 0)
                            {
                                LogSystemMessage($"📥 Received {updateCount} text chunks (total: {streamedContent.Length} chars)");
                            }
                            
                            // Capture content for UI update
                            string currentContent;
                            lock (messageLock)
                            {
                                currentContent = streamedContent;
                            }

                            Dispatcher.DispatchAsync(async () =>
                            {
                                try
                                {
                                    if (_chatMessages.Contains(agentMessage))
                                    {
                                        agentMessage.Text = currentContent;

                                        // CRITICAL: Yield control to let the GUI framework work
                                        await Task.Yield();
                                    }
                                    else
                                    {
                                        LogSystemMessage("⚠️ Agent message no longer in collection");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogSystemMessage($"❌ Error updating UI: {ex.Message}");
                                }
                            });
                        },
                        // Event callback - runs on background thread
                        (eventData) =>
                        {
                            eventCount++;
                            var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
                            
                            // Track response ID from streaming events
                            if (eventData.EventType == "created" && !string.IsNullOrEmpty(eventData.ResponseId))
                            {
                                _currentResponseId = eventData.ResponseId;
                                LogSystemMessage($"📝 Stream created (ID: {eventData.ResponseId})");
                            }
                            
                            // Only log important events to reduce clutter
                            switch (eventData.EventType)
                            {
                                case "connected":
                                    LogSystemMessage("✅ Connected to streaming endpoint");
                                    break;
                                    
                                case "created":
                                    // Already logged above when tracking response ID
                                    break;
                                    
                                case "complete":
                                    LogSystemMessage($"🏁 Response complete (Thread: {eventData.ThreadId})");
                                    break;
                                    
                                case "error":
                                case "stream_error":
                                    LogSystemMessage($"❌ Error: {eventData.Error}");
                                    break;
                                    
                                case "reasoning":
                                    LogSystemMessage($"🧠 Reasoning step received");
                                    break;
                                    
                                // Skip logging delta events as they're too verbose
                                case "delta":
                                    break;
                                    
                                default:
                                    LogSystemMessage($"ℹ️ Event: {eventData.EventType}");
                                    break;
                            }

                            // Queue UI update without blocking the streaming thread
                            Dispatcher.DispatchAsync(async () =>
                            {
                                try
                                {
                                    if (!_chatMessages.Contains(agentMessage))
                                    {
                                        return;
                                    }
                                    
                                    bool shouldUpdateFromEvent;
                                    lock (messageLock)
                                    {
                                        shouldUpdateFromEvent = !hasReceivedFirstDelta;
                                    }
                                    
                                    switch (eventData.EventType)
                                    {
                                        case "connected":
                                            if (shouldUpdateFromEvent)
                                            {
                                                agentMessage.Text = "🔗 Connected, waiting for response...";
                                            }
                                            break;
                                            
                                        case "created":
                                            if (shouldUpdateFromEvent)
                                            {
                                                agentMessage.Text = "⚡ Processing your request...";
                                            }
                                            break;
                                            
                                        case "complete":
                                            if (!string.IsNullOrEmpty(eventData.ThreadId))
                                            {
                                                _currentThreadId = eventData.ThreadId;
                                            }
                                            break;
                                            
                                        case "error":
                                        case "stream_error":
                                            agentMessage.Text = $"❌ Error: {eventData.Error}";
                                            break;
                                    }
                                    
                                    // CRITICAL: Yield control to let the GUI framework work
                                    await Task.Yield();
                                }
                                catch (Exception ex)
                                {
                                    LogSystemMessage($"❌ Error in event handler: {ex.Message}");
                                }
                            });
                        },
                        _streamingCancellationTokenSource.Token
                    );

                    LogSystemMessage("✅ Streaming request completed");
                    
                    // Final updates on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        string finalContent;
                        lock (messageLock)
                        {
                            finalContent = streamedContent;
                        }
                        
                        if (!string.IsNullOrEmpty(finalContent))
                        {
                            if (_chatMessages.Contains(agentMessage))
                            {
                                agentMessage.Text = finalContent;
                                LogSystemMessage($"✅ Response complete ({finalContent.Length} characters)");
                            }
                        }

                        var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                        if (chatCollectionView != null && _chatMessages.Count > 0)
                        {
                            chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                        }
                    });

                    if (!string.IsNullOrEmpty(resultThreadId))
                    {
                        _currentThreadId = resultThreadId;
                        LogSystemMessage($"✅ Thread ID: {resultThreadId}");
                    }
                    
                    var totalTime = (DateTime.Now - startTime).TotalSeconds;
                    LogSystemMessage($"📊 Streaming completed in {totalTime:0.1}s");
                    
                    if (!hasReceivedContent)
                    {
                        LogSystemMessage("⚠️ No streaming content received - check API connection");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (_chatMessages.Contains(agentMessage))
                            {
                                agentMessage.Text = "❌ No response received. Please try again.";
                            }
                        });
                    }
                    
                    // Save session after streaming completes
                    await SaveCurrentSessionAsync();
                }
                catch (Exception streamEx)
                {
                    LogSystemMessage($"❌ Error during streaming: {streamEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            LogSystemMessage($"❌ Error in streaming logic: {ex.Message}");
        }
        finally
        {
            // Final cancellation and cleanup
            _streamingCancellationTokenSource?.Cancel();
            _streamingCancellationTokenSource = null;
            
            LogSystemMessage("🔚 Streaming process ended");
        }
    }

    private void LogSystemMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        System.Diagnostics.Debug.WriteLine($"[T{Environment.CurrentManagedThreadId}] {logMessage}");
        
        if (MainThread.IsMainThread)
        {
            UpdateLogUI(logMessage);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => UpdateLogUI(logMessage));
        }
    }

    private void UpdateLogUI(string logMessage)
    {
        try
        {
            var systemLogLabel = this.FindByName<Label>("SystemLogLabel");
            if (systemLogLabel != null)
            {
                if (systemLogLabel.Text.Length > 50000)
                {
                    var lines = systemLogLabel.Text.Split(Environment.NewLine).Skip(100).ToList();
                    systemLogLabel.Text = string.Join(Environment.NewLine, lines) + Environment.NewLine + "[...truncated...]" + Environment.NewLine;
                }
                
                systemLogLabel.Text += logMessage + Environment.NewLine;
                
                var logScrollView = this.FindByName<ScrollView>("LogScrollView");
                if (logScrollView != null)
                {
                    Task.Run(async () => 
                    {
                        try
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await logScrollView.ScrollToAsync(0, systemLogLabel.Height, false);
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error scrolling log: {ex.Message}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating log UI: {ex.Message}");
        }
    }

    private async Task SaveCurrentSessionAsync()
    {
        if (string.IsNullOrEmpty(_currentAgentId))
            return;

        try
        {
            var session = new AgentSession
            {
                AgentId = _currentAgentId,
                ThreadId = _currentThreadId,
                LastResponseId = _currentResponseId,
                Messages = _chatMessages.ToList(),
                LastActivity = DateTime.Now
            };

            // Try to get agent name for better session display
            try
            {
                var agentDetails = await _agentApiService.GetAgentAsync(_currentAgentId);
                if (agentDetails != null)
                {
                    session.AgentName = agentDetails.Name;
                }
            }
            catch
            {
                // If we can't get agent details, just use the ID
                session.AgentName = _currentAgentId;
            }

            await _sessionManager.SaveSessionAsync(session);
            
            // Update Clear Session button visibility
            var clearSessionButton = this.FindByName<Button>("ClearSessionButton");
            if (clearSessionButton != null)
            {
                clearSessionButton.IsVisible = _chatMessages.Count > 0;
            }
            
            LogSystemMessage($"Saved session for agent {_currentAgentId} ({_chatMessages.Count} messages)");
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error saving session: {ex.Message}");
        }
    }

    /// <summary>
    /// Load the chat session for the specified agent
    /// </summary>
    private async Task LoadAgentSessionAsync(string agentId)
    {
        try
        {
            var session = await _sessionManager.GetSessionAsync(agentId);
            var clearSessionButton = this.FindByName<Button>("ClearSessionButton");
            
            // Clear current messages
            _chatMessages.Clear();
            
            if (session != null && session.Messages.Count > 0)
            {
                // Restore session data
                _currentThreadId = session.ThreadId;
                _currentResponseId = session.LastResponseId;
                
                // Restore chat messages
                foreach (var message in session.Messages)
                {
                    _chatMessages.Add(message);
                }
                
                // Show clear session button since there's history
                if (clearSessionButton != null)
                {
                    clearSessionButton.IsVisible = true;
                }
                
                LogSystemMessage($"Restored session for agent {agentId}: {session.Messages.Count} messages, ThreadId: {session.ThreadId}");
                
                // Scroll to the bottom to show the latest message
                if (_chatMessages.Count > 0)
                {
                    var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                    if (chatCollectionView != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Task.Delay(100); // Small delay to ensure UI is updated
                            chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: false);
                        });
                    }
                }
            }
            else
            {
                // No existing session, start fresh
                _currentThreadId = null;
                _currentResponseId = null;
                
                // Hide clear session button since there's no history
                if (clearSessionButton != null)
                {
                    clearSessionButton.IsVisible = false;
                }
                
                LogSystemMessage($"Starting new session for agent {agentId}");
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error loading session for agent {agentId}: {ex.Message}");
            // On error, start with a clean session
            _chatMessages.Clear();
            _currentThreadId = null;
            _currentResponseId = null;
            
            // Hide clear session button on error
            var clearSessionButton = this.FindByName<Button>("ClearSessionButton");
            if (clearSessionButton != null)
            {
                clearSessionButton.IsVisible = false;
            }
        }
    }

    /// <summary>
    /// Restore the last selected agent and its session
    /// </summary>
    private async Task RestoreLastSelectedAgentAsync()
    {
        try
        {
            var lastAgentId = await _sessionManager.GetLastSelectedAgentIdAsync();
            if (!string.IsNullOrEmpty(lastAgentId) && _agentList.Contains(lastAgentId))
            {
                LogSystemMessage($"Restoring last selected agent: {lastAgentId}");
                
                // Update UI controls to show the selected agent
                var agentPicker = this.FindByName<Picker>("AgentPicker");
                var agentListView = this.FindByName<CollectionView>("AgentListView");
                
                if (agentPicker != null)
                {
                    agentPicker.SelectedItem = lastAgentId;
                }
                
                if (agentListView != null)
                {
                    agentListView.SelectedItem = lastAgentId;
                }
                
                // Load the agent session without triggering the selection events
                await LoadAgentSessionAsync(lastAgentId);
                _currentAgentId = lastAgentId;
                
                // Update agent label
                var currentAgentLabel = this.FindByName<Label>("CurrentAgentLabel");
                if (currentAgentLabel != null)
                {
                    try
                    {
                        var agentDetails = await _agentApiService.GetAgentAsync(lastAgentId);
                        if (agentDetails != null)
                        {
                            currentAgentLabel.Text = $"{agentDetails.Name} (ID: {lastAgentId})";
                        }
                        else
                        {
                            currentAgentLabel.Text = $"Agent ID: {lastAgentId}";
                        }
                    }
                    catch
                    {
                        currentAgentLabel.Text = $"Agent ID: {lastAgentId}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error restoring last selected agent: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when the page is appearing - good place to save session
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    /// <summary>
    /// Called when the page is disappearing - save current session
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Save current session when page is disappearing
        if (!string.IsNullOrEmpty(_currentAgentId))
        {
            Task.Run(async () => await SaveCurrentSessionAsync());
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _chatMessages.Clear();
        _currentThreadId = null;
        _currentResponseId = null;
        LogSystemMessage("Chat cleared. New conversation will start with next message.");
        
        // Save the cleared session
        if (!string.IsNullOrEmpty(_currentAgentId))
        {
            Task.Run(async () => await SaveCurrentSessionAsync());
        }
    }

    private async void OnClearSessionClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentAgentId))
            return;

        try
        {
            var result = await DisplayAlert("Clear Session", 
                $"This will permanently delete the chat history for this agent. Are you sure?", 
                "Yes, Clear", "Cancel");
                
            if (result)
            {
                // Clear the session from storage
                await _sessionManager.ClearSessionAsync(_currentAgentId);
                
                // Clear current chat display
                _chatMessages.Clear();
                _currentThreadId = null;
                _currentResponseId = null;
                
                LogSystemMessage($"Session cleared for agent {_currentAgentId}");
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Error clearing session: {ex.Message}");
        }
    }
}