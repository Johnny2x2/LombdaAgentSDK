using LombdaAgentMAUI.Core.Services;

namespace LombdaAgentMAUI;

public partial class SettingsPage : ContentPage
{
    private readonly IConfigurationService _configService;
    private readonly IAgentApiService _agentApiService;

    public SettingsPage(IConfigurationService configService, IAgentApiService agentApiService)
    {
        InitializeComponent();
        _configService = configService;
        _agentApiService = agentApiService;
        
        BindingContext = _configService;
        LoadSettings();
    }

    private async void LoadSettings()
    {
        await _configService.LoadSettingsAsync();
        var apiUrlEntry = this.FindByName<Entry>("ApiUrlEntry");
        if (apiUrlEntry != null)
        {
            apiUrlEntry.Text = _configService.ApiBaseUrl;
        }
    }

    private async void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        try
        {
            var apiUrlEntry = this.FindByName<Entry>("ApiUrlEntry");
            var statusLabel = this.FindByName<Label>("StatusLabel");
            
            if (apiUrlEntry != null && !string.IsNullOrWhiteSpace(apiUrlEntry.Text))
            {
                // Validate URL format
                if (!Uri.TryCreate(apiUrlEntry.Text, UriKind.Absolute, out var uri))
                {
                    await DisplayAlert("Error", "Please enter a valid URL", "OK");
                    return;
                }

                _configService.ApiBaseUrl = apiUrlEntry.Text.TrimEnd('/') + "/";
                await _configService.SaveSettingsAsync();

                // Update the API service with the new URL
                _agentApiService.UpdateBaseUrl(_configService.ApiBaseUrl);

                if (statusLabel != null)
                {
                    statusLabel.Text = "Settings saved successfully!";
                    statusLabel.TextColor = Colors.Green;
                }

                await DisplayAlert("Success", "Settings saved successfully!", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Please enter an API URL", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save settings: {ex.Message}", "OK");
        }
    }

    private async void OnTestConnectionClicked(object? sender, EventArgs e)
    {
        try
        {
            var statusLabel = this.FindByName<Label>("StatusLabel");
            
            if (statusLabel != null)
            {
                statusLabel.Text = "Testing connection...";
                statusLabel.TextColor = Colors.Orange;
            }

            // Update configuration and API service with the current URL from the entry field
            var apiUrlEntry = this.FindByName<Entry>("ApiUrlEntry");
            if (apiUrlEntry != null && !string.IsNullOrWhiteSpace(apiUrlEntry.Text))
            {
                var testUrl = apiUrlEntry.Text.TrimEnd('/') + "/";
                _agentApiService.UpdateBaseUrl(testUrl);
            }

            // Test the connection by trying to get agents
            var agents = await _agentApiService.GetAgentsAsync();
            
            if (statusLabel != null)
            {
                statusLabel.Text = $"Connection successful! Found {agents.Count} agents.";
                statusLabel.TextColor = Colors.Green;
            }

            await DisplayAlert("Success", $"Connection successful! Found {agents.Count} agents.", "OK");
        }
        catch (Exception ex)
        {
            var statusLabel = this.FindByName<Label>("StatusLabel");
            if (statusLabel != null)
            {
                statusLabel.Text = "Connection failed!";
                statusLabel.TextColor = Colors.Red;
            }

            await DisplayAlert("Connection Failed", 
                $"Could not connect to the API:\n\n{ex.Message}\n\nPlease check:\n• API is running\n• URL is correct\n• Network connection", 
                "OK");
        }
    }
}