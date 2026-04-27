using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SteamAuthenticator.Next.Dialogs;

public class LoginDialog : Window, IComponentConnector
{
	internal TextBox UsernameTextBox;

	internal PasswordBox PasswordTextBox;

	private bool _contentLoaded;

	public string Prompt { get; set; } = "Entre na sua conta Steam.";

	public string Username { get; set; } = string.Empty;

	public string? Password { get; private set; }

	public LoginDialog()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(Username))
		{
			UsernameTextBox.Focus();
			return;
		}
		UsernameTextBox.Focus();
		UsernameTextBox.SelectAll();
		PasswordTextBox.Focus();
	}

	private void Confirm_Click(object sender, RoutedEventArgs e)
	{
		Password = PasswordTextBox.Password;
		base.DialogResult = true;
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
	}

	public static LoginDialogResult? Request(Window owner, string prompt, string username = "")
	{
		LoginDialog loginDialog = new LoginDialog
		{
			Owner = owner,
			Prompt = prompt,
			Username = username
		};
		if (loginDialog.ShowDialog() != true)
		{
			return null;
		}
		return new LoginDialogResult(loginDialog.Username, loginDialog.Password ?? string.Empty);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/logindialog.xaml", UriKind.Relative);
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
			((LoginDialog)target).Loaded += Window_Loaded;
			break;
		case 2:
			UsernameTextBox = (TextBox)target;
			break;
		case 3:
			PasswordTextBox = (PasswordBox)target;
			break;
		case 4:
			((Button)target).Click += Cancel_Click;
			break;
		case 5:
			((Button)target).Click += Confirm_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
