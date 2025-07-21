# LombdaAgent MAUI

A cross-platform .NET MAUI application for interacting with LombdaAgent through the LombdaAgent API.

## Features

- **Cross-Platform**: Runs on Android, iOS, macOS, and Windows
- **Agent Management**: Create and manage agents through the API
- **Real-time Chat**: Send messages and receive responses from agents
- **Streaming Support**: Real-time streaming responses for better user experience
- **Agent List**: View all available agents
- **System Logs**: Monitor API communication and system events
- **Settings Configuration**: Configure API endpoint settings

## Getting Started

### Prerequisites

1. **LombdaAgent API**: Make sure the LombdaAgent API is running (typically on `https://localhost:5001`)
2. **OpenAI API Key**: Set your OpenAI API key in the API environment variables

### Setup Instructions

1. **Build and Run**: Build and run the LombdaAgentMAUI project
2. **Configure API**: Go to the Settings tab and enter your API URL
3. **Test Connection**: Use the "Test Connection" button to verify API connectivity
4. **Create Agent**: Use the "Create Agent" button to create a new agent
5. **Start Chatting**: Select an agent and start sending messages

### Application Layout

#### Chat Tab
- **Agent Selection**: Dropdown to select from available agents
- **Chat Area**: Displays conversation history with timestamps
- **Message Input**: Type and send messages to the selected agent
- **Streaming Toggle**: Enable/disable real-time streaming responses
- **Agent List**: Shows all available agents
- **System Logs**: Displays API communication logs

#### Settings Tab
- **API Configuration**: Set the base URL for the LombdaAgent API
- **Connection Testing**: Test the API connection
- **Instructions**: Setup guidance

### Features Explained

#### Agent Creation
Click "Create Agent" to create a new agent with a custom name. The agent will be available for chat immediately.

#### Streaming vs Regular Responses
- **Streaming**: Responses appear character by character as they're generated
- **Regular**: Complete responses appear after generation is finished

#### System Logs
Monitor API calls, errors, and system events in the right panel for debugging and transparency.

### API Integration

The app communicates with the LombdaAgent API using:
- **GET /v1/agents**: List all agents
- **POST /v1/agents**: Create new agents
- **POST /v1/agents/{id}/messages**: Send messages (regular)
- **POST /v1/agents/{id}/messages/stream**: Send messages (streaming)

### Platform Support

- **Android**: API 21+ (Android 5.0+)
- **iOS**: iOS 15.0+
- **macOS**: macOS 12.0+ (Mac Catalyst)
- **Windows**: Windows 10 build 19041+

### Configuration

The app uses secure storage to save API settings between sessions. Configure your API endpoint in the Settings tab.

### Troubleshooting

1. **Connection Issues**: Verify the API URL and ensure the API is running
2. **No Agents**: Create agents through the app or verify API connectivity
3. **Streaming Issues**: Check network connectivity and API endpoint

### Architecture

- **MVVM Pattern**: Uses data binding and observable collections
- **Dependency Injection**: Services registered in MauiProgram
- **HTTP Client**: RESTful API communication
- **Secure Storage**: Configuration persistence

## Development

### Project Structure
```
LombdaAgentMAUI/
??? Models/           # Data models (ApiModels.cs)
??? Services/         # API and configuration services
??? Converters/       # XAML value converters
??? MainPage.xaml    # Main chat interface
??? SettingsPage.xaml # Configuration page
??? MauiProgram.cs   # Service registration
```

### Key Components
- **AgentApiService**: Handles API communication
- **ConfigurationService**: Manages app settings
- **ChatMessage**: Message data model
- **Value Converters**: UI binding helpers