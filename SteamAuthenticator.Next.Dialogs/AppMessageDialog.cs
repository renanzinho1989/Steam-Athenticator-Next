using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using SteamAuthenticator.Next.Services.Configuration;

namespace SteamAuthenticator.Next.Dialogs;

public class AppMessageDialog : Window, IComponentConnector
{
	private MessageBoxResult _result;

	internal Button TertiaryButton;

	internal Button SecondaryButton;

	internal Button PrimaryButton;

	private bool _contentLoaded;

	public string DialogTitleText { get; set; } = "Steam Authenticator Next";

	public string MessageText { get; set; } = string.Empty;

	public string IconGlyph { get; set; } = "\ue946";

	public Brush IconBackground { get; set; } = new SolidColorBrush(Color.FromRgb(18, 58, 103));

	public Brush IconForeground { get; set; } = new SolidColorBrush(Color.FromRgb(21, 156, byte.MaxValue));

	public AppMessageDialog()
	{
		InitializeComponent();
	}

	public static MessageBoxResult Show(Window? owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
	{
		AppMessageDialog appMessageDialog = new AppMessageDialog();
		appMessageDialog.Owner = owner ?? GetDefaultOwner();
		appMessageDialog.Title = caption;
		appMessageDialog.DialogTitleText = caption;
		appMessageDialog.MessageText = messageBoxText;
		appMessageDialog.Configure(button, icon);
		appMessageDialog.ShowDialog();
		return appMessageDialog._result;
	}

	public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
	{
		return Show(GetDefaultOwner(), messageBoxText, caption, button, icon);
	}

	private static Window? GetDefaultOwner()
	{
		object obj = Application.Current?.Windows.OfType<Window>().FirstOrDefault((Window window) => window.IsActive);
		if (obj == null)
		{
			Application current = Application.Current;
			if (current == null)
			{
				return null;
			}
			obj = current.MainWindow;
		}
		return (Window?)obj;
	}

	private void Configure(MessageBoxButton button, MessageBoxImage icon)
	{
		ConfigureIcon(icon);
		ConfigureButtons(button);
	}

	private void ConfigureIcon(MessageBoxImage icon)
	{
		switch (icon)
		{
		case MessageBoxImage.Exclamation:
			IconGlyph = "\ue7ba";
			IconBackground = new SolidColorBrush(Color.FromRgb(70, 54, 29));
			IconForeground = new SolidColorBrush(Color.FromRgb(246, 193, 92));
			return;
		default:
			if (icon == MessageBoxImage.Hand)
			{
				break;
			}
			switch (icon)
			{
			case MessageBoxImage.Hand:
				break;
			case MessageBoxImage.Question:
				IconGlyph = "\ue897";
				IconBackground = new SolidColorBrush(Color.FromRgb(36, 51, 68));
				IconForeground = new SolidColorBrush(Color.FromRgb(21, 156, byte.MaxValue));
				return;
			default:
			{
				MessageBoxImage messageBoxImage = icon;
				if (messageBoxImage != MessageBoxImage.Asterisk)
				{
					_ = 64;
				}
				IconGlyph = "\ue946";
				IconBackground = new SolidColorBrush(Color.FromRgb(18, 58, 103));
				IconForeground = new SolidColorBrush(Color.FromRgb(21, 156, byte.MaxValue));
				return;
			}
			}
			break;
		case MessageBoxImage.Hand:
			break;
		}
		IconGlyph = "\uea39";
		IconBackground = new SolidColorBrush(Color.FromRgb(74, 28, 34));
		IconForeground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 107, 129));
	}

	private void ConfigureButtons(MessageBoxButton button)
	{
		string content = L("OK", "OK");
		string content2 = L("Sim", "Yes");
		string content3 = L("Não", "No");
		string content4 = L("Cancelar", "Cancel");
		PrimaryButton.Visibility = Visibility.Visible;
		SecondaryButton.Visibility = Visibility.Collapsed;
		TertiaryButton.Visibility = Visibility.Collapsed;
		PrimaryButton.IsDefault = false;
		PrimaryButton.IsCancel = false;
		SecondaryButton.IsDefault = false;
		SecondaryButton.IsCancel = false;
		TertiaryButton.IsDefault = false;
		TertiaryButton.IsCancel = false;
		switch (button)
		{
		case MessageBoxButton.OKCancel:
			PrimaryButton.Content = content;
			SecondaryButton.Content = content4;
			SecondaryButton.Visibility = Visibility.Visible;
			PrimaryButton.IsDefault = true;
			SecondaryButton.IsCancel = true;
			break;
		case MessageBoxButton.YesNo:
			PrimaryButton.Content = content2;
			SecondaryButton.Content = content3;
			SecondaryButton.Visibility = Visibility.Visible;
			PrimaryButton.IsDefault = true;
			SecondaryButton.IsCancel = true;
			break;
		case MessageBoxButton.YesNoCancel:
			PrimaryButton.Content = content2;
			SecondaryButton.Content = content3;
			TertiaryButton.Content = content4;
			SecondaryButton.Visibility = Visibility.Visible;
			TertiaryButton.Visibility = Visibility.Visible;
			PrimaryButton.IsDefault = true;
			TertiaryButton.IsCancel = true;
			break;
		default:
			PrimaryButton.Content = content;
			PrimaryButton.IsDefault = true;
			PrimaryButton.IsCancel = true;
			break;
		}
	}

	private static string L(string ptBr, string english)
	{
		try
		{
			return string.Equals(new PortableAppSettingsService().Load().Language, "en-US", StringComparison.OrdinalIgnoreCase) ? english : ptBr;
		}
		catch
		{
			return CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? english : ptBr;
		}
	}

	private void PrimaryButton_Click(object sender, RoutedEventArgs e)
	{
		string text = PrimaryButton.Content?.ToString();
		MessageBoxResult result = ((!(text == "Sim") && !(text == "Yes")) ? MessageBoxResult.OK : MessageBoxResult.Yes);
		_result = result;
		base.DialogResult = true;
	}

	private void SecondaryButton_Click(object sender, RoutedEventArgs e)
	{
		string text = SecondaryButton.Content?.ToString();
		MessageBoxResult result = ((!(text == "Cancelar") && !(text == "Cancel")) ? MessageBoxResult.No : MessageBoxResult.Cancel);
		_result = result;
		base.DialogResult = false;
	}

	private void TertiaryButton_Click(object sender, RoutedEventArgs e)
	{
		_result = MessageBoxResult.Cancel;
		base.DialogResult = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/appmessagedialog.xaml", UriKind.Relative);
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
			TertiaryButton = (Button)target;
			TertiaryButton.Click += TertiaryButton_Click;
			break;
		case 2:
			SecondaryButton = (Button)target;
			SecondaryButton.Click += SecondaryButton_Click;
			break;
		case 3:
			PrimaryButton = (Button)target;
			PrimaryButton.Click += PrimaryButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
