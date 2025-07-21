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

    private async void OnTestResponseClicked(object? sender, EventArgs e)
    {
        // Add a test message to verify the UI is working
        LogSystemMessage("Adding test messages to verify UI...");
        
        var testUserMessage = new ChatMessage
        {
            Text = "This is a test user message",
            IsUser = true,
            Timestamp = DateTime.Now
        };
        
        var testAgentMessage = new ChatMessage
        {
            Text = "This is a test agent response to verify the UI is working correctly.",
            IsUser = false,
            Timestamp = DateTime.Now.AddSeconds(1)
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _chatMessages.Add(testUserMessage);
            _chatMessages.Add(testAgentMessage);
            
            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            if (chatCollectionView != null && _chatMessages.Count > 0)
            {
                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
            }
        });
        
        LogSystemMessage($"Test messages added. Total messages: {_chatMessages.Count}");
    }

    private async void OnTestStreamingClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentAgentId))
        {
            await DisplayAlert("Error", "Please select an agent first.", "OK");
            return;
        }

        LogSystemMessage("Testing streaming functionality...");
        
        // Simulate a streaming response using the new observable ChatMessage
        var testMessage = new ChatMessage
        {
            Text = "",
            IsUser = false,
            Timestamp = DateTime.Now
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _chatMessages.Add(testMessage);
        });

        // Simulate streaming text arriving character by character
        var fullText = "This is a simulated streaming response to test the UI updates with property change notifications.";
        for (int i = 0; i < fullText.Length; i++)
        {
            await Task.Delay(50); // 50ms delay between characters
            
            var currentText = fullText.Substring(0, i + 1);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Update the Text property directly - this will trigger PropertyChanged
                testMessage.Text = currentText;

                // Auto-scroll to bottom occasionally
                if (i % 20 == 0) // Every 20 characters
                {
                    var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                    if (chatCollectionView != null && _chatMessages.Count > 0)
                    {
                        chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: false);
                    }
                }
            });
        }
        
        // Final scroll
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            if (chatCollectionView != null && _chatMessages.Count > 0)
            {
                chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
            }
        });
        
        LogSystemMessage("Simulated streaming test completed.");
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
            // Add detailed logging to see what we received
            LogSystemMessage($"Response received - ThreadId: {response.ThreadId}");
            LogSystemMessage($"Response text length: {response.Text?.Length ?? 0}");
            LogSystemMessage($"Response text preview: {(string.IsNullOrEmpty(response.Text) ? "[EMPTY]" : response.Text.Substring(0, Math.Min(response.Text.Length, 100)))}...");

            _currentThreadId = response.ThreadId;

            var agentMessage = new ChatMessage
            {
                Text = response.Text ?? "[No response text received]",
                IsUser = false,
                Timestamp = DateTime.Now
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _chatMessages.Add(agentMessage);
                LogSystemMessage($"Added agent message to chat. Total messages: {_chatMessages.Count}");
                
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                if (chatCollectionView != null && _chatMessages.Count > 0)
                {
                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                    LogSystemMessage("Scrolled to bottom of chat");
                }
                else
                {
                    LogSystemMessage("Chat CollectionView not found or no messages");
                }
            });

            LogSystemMessage("Response processing completed.");
        }
        else
        {
            LogSystemMessage("Failed to get response from agent - response was null.");
            
            // Add a message to the chat indicating the failure
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var errorMessage = new ChatMessage
                {
                    Text = "❌ Failed to get response from agent. Please try again.",
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                _chatMessages.Add(errorMessage);
            });
        }
    }

    private async Task SendStreamingMessage(string message)
    {
        LogSystemMessage($"Starting streaming message to agent {_currentAgentId}...");

        // Create a placeholder message for streaming - now with property change notifications
        var agentMessage = new ChatMessage
        {
            Text = "🤖 Thinking...",
            IsUser = false,
            Timestamp = DateTime.Now
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _chatMessages.Add(agentMessage);
        });

        _streamingCancellationTokenSource?.Cancel();
        _streamingCancellationTokenSource = new CancellationTokenSource();
        
        // Set a longer timeout for streaming (5 minutes)
        _streamingCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(5));

        var streamedContent = "";
        var hasReceivedContent = false;

        try
        {
            LogSystemMessage("Calling SendMessageStreamWithThreadAsync...");
            
            var resultThreadId = await _agentApiService.SendMessageStreamWithThreadAsync(
                _currentAgentId!,
                message,
                _currentThreadId,
                (streamedText) =>
                {
                    hasReceivedContent = true;
                    streamedContent += streamedText;
                    
                    LogSystemMessage($"Received streaming chunk: '{streamedText}' (total length: {streamedContent.Length})");
                    
                    // Update the message text property directly - this will trigger UI updates via INotifyPropertyChanged
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            agentMessage.Text = streamedContent;

                            // Auto-scroll to bottom - but only occasionally to improve performance
                            if (streamedContent.Length % 30 == 0) // Every 30 characters
                            {
                                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                                if (chatCollectionView != null && _chatMessages.Count > 0)
                                {
                                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: false);
                                }
                            }
                        }
                        catch (Exception uiEx)
                        {
                            LogSystemMessage($"Error updating UI: {uiEx.Message}");
                        }
                    });
                },
                _streamingCancellationTokenSource.Token
            );

            LogSystemMessage("SendMessageStreamWithThreadAsync completed");

            // Final scroll to bottom
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                if (chatCollectionView != null && _chatMessages.Count > 0)
                {
                    chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: true);
                }
            });

            // Update the thread ID from the streaming response
            if (!string.IsNullOrEmpty(resultThreadId))
            {
                _currentThreadId = resultThreadId;
                LogSystemMessage($"Streaming completed successfully. Thread ID: {resultThreadId}");
            }
            else
            {
                LogSystemMessage("Streaming completed but no thread ID received");
            }
            
            LogSystemMessage($"Final content length: {streamedContent.Length}");
            
            if (!hasReceivedContent)
            {
                LogSystemMessage("Warning: No streaming content was received");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    agentMessage.Text = "❌ No response received from streaming. The agent may be taking longer than expected.";
                });
            }
        }
        catch (OperationCanceledException)
        {
            LogSystemMessage("Streaming was cancelled (timeout or user cancellation)");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                agentMessage.Text = "⏱️ Streaming timed out. The agent may be taking longer than expected.";
            });
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Streaming error: {ex.Message}");
            LogSystemMessage($"Error type: {ex.GetType().Name}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                agentMessage.Text = $"❌ Streaming error: {ex.Message}";
            });
        }
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

    private async void OnTestDirectStreamingClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentAgentId))
        {
            await DisplayAlert("Error", "Please select an agent first.", "OK");
            return;
        }

        LogSystemMessage("Testing direct streaming endpoint...");

        try
        {
            // Test the streaming endpoint directly
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_configService.ApiBaseUrl);
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var request = new
            {
                text = "Hello, this is a test message",
                threadId = _currentThreadId
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/agents/{_currentAgentId}/messages/stream")
            {
                Content = content
            };

            httpRequest.Headers.Add("Accept", "text/event-stream");
            httpRequest.Headers.Add("Cache-Control", "no-cache");

            LogSystemMessage("Sending direct HTTP request...");

            using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            
            LogSystemMessage($"Response status: {response.StatusCode}");
            LogSystemMessage($"Response content type: {response.Content.Headers.ContentType}");

            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                var lineCount = 0;
                while (!reader.EndOfStream && lineCount < 20) // Limit to first 20 lines for testing
                {
                    var line = await reader.ReadLineAsync();
                    if (line != null)
                    {
                        lineCount++;
                        LogSystemMessage($"Line {lineCount}: {line}");
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                LogSystemMessage($"Error response: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            LogSystemMessage($"Direct streaming test error: {ex.Message}");
        }
    }
}
