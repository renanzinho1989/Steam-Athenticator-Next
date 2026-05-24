using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SteamAuth;

namespace SteamAuthenticator.Next.Dialogs;

public class ConfirmationsWindow : Window, INotifyPropertyChanged, IComponentConnector, IStyleConnector
{
	private readonly SteamGuardAccount _account;

	private readonly Func<SteamGuardAccount, Task>? _persistAccountAsync;

	private string _statusMessage = "Carregando confirmacoes...";

	private bool _hasConfirmations;

	private bool _contentLoaded;

	public ObservableCollection<ConfirmationItem> Confirmations { get; } = new ObservableCollection<ConfirmationItem>();

	public string HeaderTitle { get; }

	public string StatusMessage
	{
		get
		{
			return _statusMessage;
		}
		private set
		{
			if (!string.Equals(_statusMessage, value, StringComparison.Ordinal))
			{
				_statusMessage = value;
				OnPropertyChanged("StatusMessage");
			}
		}
	}

	public bool HasConfirmations
	{
		get
		{
			return _hasConfirmations;
		}
		private set
		{
			if (_hasConfirmations != value)
			{
				_hasConfirmations = value;
				OnPropertyChanged("HasConfirmations");
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public ConfirmationsWindow(SteamGuardAccount account, Func<SteamGuardAccount, Task>? persistAccountAsync = null)
	{
		InitializeComponent();
		_account = account;
		_persistAccountAsync = persistAccountAsync;
		base.DataContext = this;
		HeaderTitle = "Confirmacoes - " + account.AccountName;
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		await LoadConfirmationsAsync();
	}

	private async void Refresh_Click(object sender, RoutedEventArgs e)
	{
		await LoadConfirmationsAsync();
	}

	private async void Accept_Click(object sender, RoutedEventArgs e)
	{
		object obj = (sender as FrameworkElement)?.Tag;
		if (!(obj is ConfirmationItem item))
		{
			return;
		}
		try
		{
			StatusMessage = "Aceitando confirmacao...";
			await EnsureSessionReadyAsync();
			await _account.AcceptConfirmation(item.Confirmation);
			await LoadConfirmationsAsync();
		}
		catch (Exception ex)
		{
			ShowError(ex);
		}
	}

	private async void Cancel_Click(object sender, RoutedEventArgs e)
	{
		object obj = (sender as FrameworkElement)?.Tag;
		if (!(obj is ConfirmationItem item))
		{
			return;
		}
		try
		{
			StatusMessage = "Cancelando confirmacao...";
			await EnsureSessionReadyAsync();
			await _account.DenyConfirmation(item.Confirmation);
			await LoadConfirmationsAsync();
		}
		catch (Exception ex)
		{
			ShowError(ex);
		}
	}

	private async Task LoadConfirmationsAsync()
	{
		_ = 1;
		try
		{
			StatusMessage = "Atualizando confirmacoes...";
			await EnsureSessionReadyAsync();
			Confirmation[] obj = await _account.FetchConfirmationsAsync();
			Confirmations.Clear();
			Confirmation[] array = obj;
			foreach (Confirmation confirmation in array)
			{
				Confirmations.Add(new ConfirmationItem(confirmation));
			}
			HasConfirmations = Confirmations.Count > 0;
			StatusMessage = ((Confirmations.Count == 0) ? "Nada pendente para confirmar." : $"{Confirmations.Count} confirmacoes carregadas.");
		}
		catch (Exception ex)
		{
			ShowError(ex);
		}
	}

	private async Task EnsureSessionReadyAsync()
	{
		if (_account.Session == null)
		{
			throw new InvalidOperationException("A conta selecionada nao possui sessao valida. Refaça o login da conta.");
		}
		if (_account.Session.IsRefreshTokenExpired())
		{
			throw new InvalidOperationException("O refresh token expirou. Refaca o login da conta para abrir as confirmacoes.");
		}
		if (_account.Session.IsAccessTokenExpired())
		{
			await _account.Session.RefreshAccessToken();
			if (_persistAccountAsync != null)
			{
				await _persistAccountAsync(_account);
			}
		}
	}

	private void ShowError(Exception ex)
	{
		HasConfirmations = Confirmations.Count > 0;
		StatusMessage = "Nao foi possivel carregar as confirmacoes: " + ex.Message;
		AppMessageDialog.Show(this, ex.Message, "Confirmacoes", MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/dialogs/confirmationswindow.xaml", UriKind.Relative);
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
			((ConfirmationsWindow)target).Loaded += Window_Loaded;
			break;
		case 4:
			((Button)target).Click += Refresh_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			((Button)target).Click += Accept_Click;
			break;
		case 3:
			((Button)target).Click += Cancel_Click;
			break;
		}
	}
}
