using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SteamAuthenticator.Next.Dialogs;

public class SettingsWindow : Window, IComponentConnector
{
	internal CheckBox MinimizeOnCloseCheckBox;

	internal TextBlock ProtectionStatusText;

	internal Button DisableEncryptionButton;

	private bool _contentLoaded;

	public bool MinimizeOnClose { get; private set; }

	public bool DisableEncryptionRequested { get; private set; }

	public SettingsWindow(bool minimizeOnClose, bool vaultIsEncrypted)
	{
		InitializeComponent();
		MinimizeOnClose = minimizeOnClose;
		MinimizeOnCloseCheckBox.IsChecked = minimizeOnClose;
		DisableEncryptionButton.IsEnabled = vaultIsEncrypted;
		ProtectionStatusText.Text = (vaultIsEncrypted ? "Seu cofre atual esta protegido por senha. Use esta opcao para remover a senha e deixar os maFiles sem criptografia." : "Seu cofre ja esta sem senha. Os maFiles continuam visiveis na mesma pasta do programa.");
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		SyncState();
		base.OnClosing(e);
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		SyncState();
		Close();
	}

	private void DisableEncryption_Click(object sender, RoutedEventArgs e)
	{
		SyncState();
		DisableEncryptionRequested = true;
		Close();
	}

	private void SyncState()
	{
		MinimizeOnClose = MinimizeOnCloseCheckBox.IsChecked == true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/settingswindow.xaml", UriKind.Relative);
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
			MinimizeOnCloseCheckBox = (CheckBox)target;
			break;
		case 2:
			ProtectionStatusText = (TextBlock)target;
			break;
		case 3:
			DisableEncryptionButton = (Button)target;
			DisableEncryptionButton.Click += DisableEncryption_Click;
			break;
		case 4:
			((Button)target).Click += Close_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
