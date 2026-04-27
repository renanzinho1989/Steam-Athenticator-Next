using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SteamAuthenticator.Next.Dialogs;

public class TextPromptDialog : Window, IComponentConnector
{
	internal TextBox ValueTextBox;

	internal PasswordBox ValuePasswordBox;

	private bool _contentLoaded;

	public string Prompt { get; set; } = "Digite um valor.";

	public string Hint { get; set; } = string.Empty;

	public string ConfirmText { get; set; } = "Confirmar";

	public string CancelText { get; set; } = "Cancelar";

	public string Value { get; set; } = string.Empty;

	public bool IsPassword { get; set; }

	public TextPromptDialog()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		PositionWithinOwner();
		if (IsPassword)
		{
			ValueTextBox.Visibility = Visibility.Collapsed;
			ValuePasswordBox.Visibility = Visibility.Visible;
			ValuePasswordBox.Focus();
		}
		else
		{
			ValueTextBox.Visibility = Visibility.Visible;
			ValuePasswordBox.Visibility = Visibility.Collapsed;
			ValueTextBox.Focus();
			ValueTextBox.SelectAll();
		}
	}

	private void PositionWithinOwner()
	{
		if (base.Owner == null)
		{
			base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			return;
		}
		UpdateLayout();
		double left = base.Owner.Left;
		double top = base.Owner.Top;
		double num = ((base.Owner.ActualWidth > 0.0) ? base.Owner.ActualWidth : base.Owner.Width);
		double num2 = ((base.Owner.ActualHeight > 0.0) ? base.Owner.ActualHeight : base.Owner.Height);
		if (base.Owner is MainWindow mainWindow)
		{
			Rect? newAccountPromptPlacement = mainWindow.GetNewAccountPromptPlacement();
			if (newAccountPromptPlacement.HasValue)
			{
				base.Left = left + newAccountPromptPlacement.Value.X;
				base.Top = top + newAccountPromptPlacement.Value.Y;
				double num3 = left + 16.0;
				double num4 = left + Math.Max(16.0, num - base.ActualWidth - 16.0);
				if (base.Left < num3)
				{
					base.Left = num3;
				}
				else if (base.Left > num4)
				{
					base.Left = num4;
				}
				double num5 = top + 48.0;
				double num6 = top + Math.Max(48.0, num2 - base.ActualHeight - 20.0);
				if (base.Top < num5)
				{
					base.Top = num5;
				}
				else if (base.Top > num6)
				{
					base.Top = num6;
				}
				return;
			}
		}
		base.Left = left + (num - base.ActualWidth) / 2.0;
		base.Top = top + 120.0;
		double num7 = top + Math.Max(40.0, num2 - base.ActualHeight - 50.0);
		if (base.Top > num7)
		{
			base.Top = num7;
		}
	}

	private void ValuePasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		Value = ValuePasswordBox.Password;
	}

	private void Confirm_Click(object sender, RoutedEventArgs e)
	{
		if (IsPassword)
		{
			Value = ValuePasswordBox.Password;
		}
		base.DialogResult = true;
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
	}

	public static string? Request(Window owner, string title, string prompt, string hint = "", bool isPassword = false, string confirmText = "Confirmar", string cancelText = "Cancelar")
	{
		TextPromptDialog textPromptDialog = new TextPromptDialog
		{
			Owner = owner,
			Title = title,
			Prompt = prompt,
			Hint = hint,
			IsPassword = isPassword,
			ConfirmText = confirmText,
			CancelText = cancelText
		};
		if (textPromptDialog.ShowDialog() != true)
		{
			return null;
		}
		return textPromptDialog.Value;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/textpromptdialog.xaml", UriKind.Relative);
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
			((TextPromptDialog)target).Loaded += Window_Loaded;
			break;
		case 2:
			ValueTextBox = (TextBox)target;
			break;
		case 3:
			ValuePasswordBox = (PasswordBox)target;
			ValuePasswordBox.PasswordChanged += ValuePasswordBox_OnPasswordChanged;
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
