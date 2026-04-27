using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SteamAuthenticator.Next.Dialogs;

namespace SteamAuthenticator.Next;

public class App : Application
{
	private const int MinimumSplashDurationMilliseconds = 350;

	private bool _contentLoaded;

	public bool ForceExitRequested { get; set; }

	public App()
	{
		base.DispatcherUnhandledException += OnDispatcherUnhandledException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	private async void OnStartup(object sender, StartupEventArgs e)
	{
		long splashStartedAt = Environment.TickCount64;
		SplashWindow splashWindow = new SplashWindow();
		splashWindow.Show();
		await base.Dispatcher.InvokeAsync(delegate
		{
		}, DispatcherPriority.ApplicationIdle);
		MainWindow mainWindow = (MainWindow)(base.MainWindow = new MainWindow());
		int num = Math.Max(0, 350 - (int)(Environment.TickCount64 - splashStartedAt));
		if (num > 0)
		{
			await Task.Delay(num);
		}
		mainWindow.Show();
		splashWindow.Close();
	}

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		ForceExitRequested = true;
		HandleFatalException("Erro inesperado na interface", e.Exception);
		e.Handled = true;
		Shutdown(-1);
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		ForceExitRequested = true;
		if (e.ExceptionObject is Exception exception)
		{
			HandleFatalException("Erro fatal ao iniciar o app", exception);
		}
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		HandleFatalException("Erro em tarefa em segundo plano", e.Exception);
		e.SetObserved();
	}

	private static void HandleFatalException(string title, Exception exception)
	{
		string text = Path.Combine(AppContext.BaseDirectory, "SteamAuthenticatorNext-error.log");
		string contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{exception}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}";
		try
		{
			File.AppendAllText(text, contents);
		}
		catch
		{
		}
		AppMessageDialog.Show($"{title}.{Environment.NewLine}{Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}Log: {text}", "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.Startup += OnStartup;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
