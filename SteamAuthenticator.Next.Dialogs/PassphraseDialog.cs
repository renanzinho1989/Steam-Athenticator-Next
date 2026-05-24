using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SteamAuthenticator.Next.Dialogs;

public class PassphraseDialog : Window, IComponentConnector
{
	internal PasswordBox PassphraseBox;

	private bool _contentLoaded;

	public string Prompt { get; set; } = "Digite a senha do cofre.";

	public string? Passphrase { get; private set; }

	public PassphraseDialog()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		PassphraseBox.Focus();
	}

	private void Confirm_Click(object sender, RoutedEventArgs e)
	{
		Passphrase = PassphraseBox.Password;
		base.DialogResult = true;
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
	}

	public static string? Request(Window owner, string title, string prompt)
	{
		PassphraseDialog passphraseDialog = new PassphraseDialog
		{
			Owner = owner,
			Title = title,
			Prompt = prompt
		};
		if (passphraseDialog.ShowDialog() != true)
		{
			return null;
		}
		return passphraseDialog.Passphrase;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/passphrasedialog.xaml", UriKind.Relative);
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
			((PassphraseDialog)target).Loaded += Window_Loaded;
			break;
		case 2:
			PassphraseBox = (PasswordBox)target;
			break;
		case 3:
			((Button)target).Click += Confirm_Click;
			break;
		case 4:
			((Button)target).Click += Cancel_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
