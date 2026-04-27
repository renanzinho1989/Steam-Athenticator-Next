using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SteamAuthenticator.Next.Dialogs;

public class WelcomeSetupDialog : Window, IComponentConnector
{
	private bool _contentLoaded;

	public WelcomeSetupChoice Choice { get; private set; }

	public WelcomeSetupDialog()
	{
		InitializeComponent();
	}

	private void ImportVault_Click(object sender, RoutedEventArgs e)
	{
		Choice = WelcomeSetupChoice.ImportVaultFolder;
		base.DialogResult = true;
	}

	private void FirstTime_Click(object sender, RoutedEventArgs e)
	{
		Choice = WelcomeSetupChoice.FirstTimeSetup;
		base.DialogResult = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/welcomesetupdialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += ImportVault_Click;
			break;
		case 2:
			((Button)target).Click += FirstTime_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
