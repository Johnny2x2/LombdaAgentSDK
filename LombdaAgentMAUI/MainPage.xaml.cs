using LombdaAgentMAUI.Core.Models;
using LombdaAgentMAUI.Core.Services;
using System.Collections.ObjectModel;

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
            await _configService.LoadSettingsAsync();

            var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
            var agentListView = this.FindByName<CollectionView>("AgentListView");

            if (chatCollectionView != null)
                chatCollectionView.ItemsSource = _chatMessages;
            
            if (agentListView != null)
                agentListView.ItemsSource = _agentList;

            await LoadAgentsAsync();
            LogSystemMessage("Application started. Please select or create an agent.");
            
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
            
            await MainThread.InvokeOnMainThreadAsync(() =>
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

        await MainThread.InvokeOnMainThreadAsync(() =>
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
        
        var testMessage = new ChatMessage
        {
            Text = "",
            IsUser = false,
            Timestamp = DateTime.Now
        };

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _chatMessages.Add(testMessage);
        });

        var fullText = "This is a simulated streaming response to test the UI updates with property change notifications.";
        for (int i = 0; i < fullText.Length; i++)
        {
            await Task.Delay(50);
            var currentText = fullText.Substring(0, i + 1);
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                testMessage.Text = currentText;

                if (i % 20 == 0)
                {
                    var chatCollectionView = this.FindByName<CollectionView>("ChatCollectionView");
                    if (chatCollectionView != null && _chatMessages.Count > 0)
                    {
                        chatCollectionView.ScrollTo(_chatMessages.Last(), position: ScrollToPosition.End, animate: false);
                    }
                }
            });
        }
        
        await MainThread.InvokeOnMainThreadAsync(() =>
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
            _currentThreadId = null;
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
            var userMessage = new ChatMessage
            {
                Text = message,
                IsUser = true,
                Timestamp = DateTime.Now
            };
            _chatMessages.Add(userMessage);
            
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
            LogSystemMessage("🔄 Starting background streaming task...");
            
            // Use Task.Run to execute streaming on background thread
            await Task.Run(async () =>
            {
                try
                {
                    LogSystemMessage("🔄 Calling SendMessageStreamWithEventsAsync on background thread...");
                    
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
                            
                            LogSystemMessage($"📥 Text chunk #{updateCount}: '{streamedText.Replace("\n", "\\n")}' (total: {streamedContent.Length})");
                            
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
                                        LogSystemMessage($"✅ Updated UI with chunk #{updateCount} (length: {currentContent.Length})");

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
                            
                            LogSystemMessage($"🔄 Completed processing chunk #{updateCount}");
                        },
                        // Event callback - runs on background thread
                        (eventData) =>
                        {
                            eventCount++;
                            var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
                            LogSystemMessage($"🔔 Event #{eventCount}: {eventData.EventType} (at {elapsedMs:0}ms)");

                            // Queue UI update without blocking the streaming thread
                            Dispatcher.DispatchAsync(async () =>
                            {
                                try
                                {
                                    if (!_chatMessages.Contains(agentMessage))
                                    {
                                        LogSystemMessage("⚠️ Agent message no longer in collection during event");
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
                                                LogSystemMessage("📱 UI updated: Connected");
                                            }
                                            LogSystemMessage("✅ Connected to streaming endpoint");
                                            break;
                                            
                                        case "created":
                                            if (shouldUpdateFromEvent)
                                            {
                                                agentMessage.Text = "⚡ Processing your request...";
                                                LogSystemMessage("📱 UI updated: Processing");
                                            }
                                            LogSystemMessage($"📝 Stream created (ID: {eventData.ResponseId})");
                                            break;
                                            
                                        case "delta":
                                            LogSystemMessage($"📄 Delta event processed (length: {eventData.Text?.Length ?? 0})");
                                            break;
                                            
                                        case "complete":
                                            LogSystemMessage($"🏁 Response complete (Thread: {eventData.ThreadId})");
                                            if (!string.IsNullOrEmpty(eventData.ThreadId))
                                            {
                                                _currentThreadId = eventData.ThreadId;
                                            }
                                            break;
                                            
                                        case "error":
                                        case "stream_error":
                                            agentMessage.Text = $"❌ Error: {eventData.Error}";
                                            LogSystemMessage($"❌ Error: {eventData.Error}");
                                            break;
                                            
                                        case "reasoning":
                                            LogSystemMessage($"🧠 Reasoning: {eventData.Text?.Replace("\n", "\\n")}");
                                            break;
                                            
                                        default:
                                            LogSystemMessage($"ℹ️ Unknown event: {eventData.EventType}");
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

                    LogSystemMessage("✅ Background streaming completed successfully");
                    
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
                                LogSystemMessage($"✅ Final text update (length: {finalContent.Length})");
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
                    LogSystemMessage($"📊 Stats - Content: {streamedContent.Length} chars, Updates: {updateCount}, Events: {eventCount}, Time: {totalTime:0.00}s");
                    
                    if (!hasReceivedContent)
                    {
                        LogSystemMessage("⚠️ No streaming content received - check if streaming is working from API");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (_chatMessages.Contains(agentMessage))
                            {
                                agentMessage.Text = "❌ No response received. Please try again.";
                            }
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    LogSystemMessage("⏱️ Streaming cancelled/timeout");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (_chatMessages.Contains(agentMessage))
                        {
                            agentMessage.Text = "⏱️ Streaming timed out.";
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogSystemMessage($"❌ Streaming error: {ex.Message}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (_chatMessages.Contains(agentMessage))
                        {
                            agentMessage.Text = $"❌ Error: {ex.Message}";
                        }
                    });
                }
            }, _streamingCancellationTokenSource.Token);
            
            LogSystemMessage("✅ Streaming task completed, UI thread released");
        }
        catch (Exception ex)
        {
            LogSystemMessage($"❌ Error in streaming task: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_chatMessages.Contains(agentMessage))
                {
                    agentMessage.Text = $"❌ Task error: {ex.Message}";
                }
            });
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _chatMessages.Clear();
        _currentThreadId = null;
        LogSystemMessage("Chat cleared. New conversation will start with next message.");
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
                while (!reader.EndOfStream && lineCount < 20)
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