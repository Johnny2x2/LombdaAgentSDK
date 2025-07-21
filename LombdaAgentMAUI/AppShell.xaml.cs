namespace LombdaAgentMAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute("MainPage", typeof(MainPage));
        Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
	}
}
