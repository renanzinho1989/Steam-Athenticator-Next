using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using SteamAuth;
using SteamAuthenticator.Next.Dialogs;
using SteamAuthenticator.Next.Services.Vault;

namespace SteamAuthenticator.Next.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
	private const int ExternalReloadRetryAttempts = 8;

	private static readonly TimeSpan ExternalReloadRetryDelay = TimeSpan.FromMilliseconds(450L, 0L);

	private readonly SteamVaultService _vaultService = new SteamVaultService();

	private readonly DispatcherTimer _timer;

	private readonly RelayCommand _copyCodeCommand;

	private readonly RelayCommand _showConfirmationsCommand;

	private readonly RelayCommand _newAccountCommand;

	private string _searchText = string.Empty;

	private string _currentCode = "-----";

	private string _countdownLabel = "Aguardando conta";

	private string _statusMessage = "Inicializando cofre...";

	private string _vaultStatus = "Procurando maFiles...";

	private string _vaultModeLabel = "Modo leitura";

	private string _protectionButtonText = "Protecao";

	private double _progressValue;

	private string? _vaultRoot;

	private string? _vaultPassphrase;

	private AccountItemViewModel? _selectedAccount;

	private bool _shouldShowWelcomeExperience;

	private long _lastCodeSlice = -1L;

	private ulong _lastCodeSteamId;

	private bool _isEnglishLanguage;

	public ObservableCollection<AccountItemViewModel> Accounts { get; }

	public ICollectionView AccountsView { get; }

	public RelayCommand CopyCodeCommand => _copyCodeCommand;

	public RelayCommand ShowConfirmationsCommand => _showConfirmationsCommand;

	public RelayCommand NewAccountCommand => _newAccountCommand;

	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (SetProperty(ref _searchText, value, "SearchText"))
			{
				AccountsView.Refresh();
			}
		}
	}

	public AccountItemViewModel? SelectedAccount
	{
		get
		{
			return _selectedAccount;
		}
		set
		{
			if (SetProperty(ref _selectedAccount, value, "SelectedAccount"))
			{
				ResetCodeCache();
				RaisePropertyChanged("HasSelectedAccount");
				RaisePropertyChanged("SelectedAccountTitle");
				UpdateCodeState();
				_showConfirmationsCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public string CurrentCode
	{
		get
		{
			return _currentCode;
		}
		private set
		{
			if (SetProperty(ref _currentCode, value, "CurrentCode"))
			{
				_copyCodeCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public string CountdownLabel
	{
		get
		{
			return _countdownLabel;
		}
		private set
		{
			SetProperty(ref _countdownLabel, value, "CountdownLabel");
		}
	}

	public string StatusMessage
	{
		get
		{
			return _statusMessage;
		}
		private set
		{
			SetProperty(ref _statusMessage, value, "StatusMessage");
		}
	}

	public string VaultStatus
	{
		get
		{
			return _vaultStatus;
		}
		private set
		{
			SetProperty(ref _vaultStatus, value, "VaultStatus");
		}
	}

	public string VaultModeLabel
	{
		get
		{
			return _vaultModeLabel;
		}
		private set
		{
			SetProperty(ref _vaultModeLabel, value, "VaultModeLabel");
		}
	}

	public string ProtectionButtonText
	{
		get
		{
			return _protectionButtonText;
		}
		private set
		{
			SetProperty(ref _protectionButtonText, value, "ProtectionButtonText");
		}
	}

	public double ProgressValue
	{
		get
		{
			return _progressValue;
		}
		set
		{
			SetProperty(ref _progressValue, value, "ProgressValue");
		}
	}

	public string SelectedAccountTitle
	{
		get
		{
			if (SelectedAccount != null)
			{
				return L("Conta: ", "Account: ") + SelectedAccount.DisplayName;
			}
			return L("Conta: nenhuma selecionada", "Account: none selected");
		}
	}

	public bool HasSelectedAccount => SelectedAccount != null;

	public string AppVersion => "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

	public SteamGuardAccount? SelectedSteamAccount => SelectedAccount?.Account;

	public string? VaultRoot => _vaultRoot;

	public string VaultFolderPath => _vaultRoot ?? _vaultService.GetPortableVaultRoot();

	public string? VaultPassphrase => _vaultPassphrase;

	public bool ShouldShowWelcomeExperience => _shouldShowWelcomeExperience;

	public MainWindowViewModel()
	{
		_vaultRoot = _vaultService.EnsurePortableVaultRootExists();
		Accounts = new ObservableCollection<AccountItemViewModel>();
		AccountsView = CollectionViewSource.GetDefaultView(Accounts);
		AccountsView.Filter = FilterAccounts;
		_copyCodeCommand = new RelayCommand(CopyCode, () => !string.IsNullOrWhiteSpace(CurrentCode) && CurrentCode != "-----");
		_showConfirmationsCommand = new RelayCommand(ShowConfirmations, () => SelectedAccount != null);
		_newAccountCommand = new RelayCommand(CreateAccountPlaceholder);
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1L)
		};
		_timer.Tick += delegate
		{
			UpdateCodeState();
		};
	}

	public void SetLanguage(string language)
	{
		bool flag = string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase);
		if (_isEnglishLanguage == flag)
		{
			return;
		}
		_isEnglishLanguage = flag;
		RaisePropertyChanged("SelectedAccountTitle");
		UpdateCodeState();
		VaultStatus = LocalizeKnownText(VaultStatus);
		VaultModeLabel = LocalizeKnownText(VaultModeLabel);
		ProtectionButtonText = LocalizeKnownText(ProtectionButtonText);
		StatusMessage = LocalizeKnownText(StatusMessage);
	}

	private string L(string ptBr, string english)
	{
		if (!_isEnglishLanguage)
		{
			return ptBr;
		}
		return english;
	}

	private string LocalizeKnownText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		switch (text.Trim())
		{
		case "Aguardando conta":
		case "Waiting for account":
			return L("Aguardando conta", "Waiting for account");
		case "Atualizando codigo...":
		case "Updating code...":
			return L("Atualizando codigo...", "Updating code...");
		case "Procurando maFiles...":
		case "Searching maFiles...":
			return L("Procurando maFiles...", "Searching maFiles...");
		case "Inicializando cofre...":
		case "Initializing vault...":
			return L("Inicializando cofre...", "Initializing vault...");
		case "Cofre portatil vazio":
		case "Cofre portátil vazio":
		case "Portable vault empty":
			return L("Cofre portatil vazio", "Portable vault empty");
		case "Cofre portatil carregado":
		case "Portable vault loaded":
			return L("Cofre portatil carregado", "Portable vault loaded");
		case "Cofre portatil criptografado":
		case "Portable vault encrypted":
			return L("Cofre portatil criptografado", "Portable vault encrypted");
		case "Protecao":
		case "Protection":
			return L("Protecao", "Protection");
		case "Protegido":
		case "Protected":
			return L("Protegido", "Protected");
		case "Sem senha":
		case "No password":
			return L("Sem senha", "No password");
		case "Importe maFile":
		case "Import maFile":
			return L("Importe maFile", "Import maFile");
		case "Ativar senha":
		case "Enable passphrase":
			return L("Ativar senha", "Enable passphrase");
		case "Trocar senha":
		case "Change passphrase":
			return L("Trocar senha", "Change passphrase");
		case "Codigo copiado para a area de transferencia.":
		case "Code copied to clipboard.":
			return L("Codigo copiado para a area de transferencia.", "Code copied to clipboard.");
		case "Nenhuma conta encontrada no cofre atual.":
		case "No account found in current vault.":
			return L("Nenhuma conta encontrada no cofre atual.", "No account found in current vault.");
		case "Cofre aberto com sucesso.":
		case "Vault opened successfully.":
			return L("Cofre aberto com sucesso.", "Vault opened successfully.");
		case "Nenhuma conta ainda. Importe um maFile para criar o cofre ao lado do programa.":
		case "No account yet. Import a maFile to create the vault beside the app.":
			return L("Nenhuma conta ainda. Importe um maFile para criar o cofre ao lado do programa.", "No account yet. Import a maFile to create the vault beside the app.");
		default:
			return text;
		}
	}

	public async Task InitializeAsync(Func<string, string?> requestPassphrase)
	{
		string passphrase = null;
		while (true)
		{
			try
			{
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(passphrase, _vaultRoot)), passphrase);
				StatusMessage = ((Accounts.Count == 0) ? L("Nenhuma conta encontrada no cofre atual.", "No account found in current vault.") : L("Cofre aberto com sucesso.", "Vault opened successfully."));
				break;
			}
			catch (VaultPassphraseRequiredException)
			{
				passphrase = requestPassphrase("Digite a senha do seu cofre para abrir as contas.");
				if (string.IsNullOrWhiteSpace(passphrase))
				{
					StatusMessage = L("Abertura do cofre cancelada.", "Vault opening canceled.");
					break;
				}
			}
			catch (VaultPassphraseInvalidException)
			{
				passphrase = requestPassphrase("Senha invalida. Tente novamente.");
				if (string.IsNullOrWhiteSpace(passphrase))
				{
					StatusMessage = L("Abertura do cofre cancelada.", "Vault opening canceled.");
					break;
				}
			}
			catch (VaultManifestIncompleteException)
			{
				StatusMessage = L("A pasta maFiles ainda esta sendo copiada. Aguarde alguns segundos.", "The maFiles folder is still being copied. Please wait a few seconds.");
				_shouldShowWelcomeExperience = false;
				UpdateTimerState();
				UpdateCodeState();
				break;
			}
			catch (VaultNotFoundException)
			{
				_vaultRoot = _vaultService.EnsurePortableVaultRootExists();
				RaisePropertyChanged("VaultFolderPath");
				VaultStatus = L("Cofre portatil vazio", "Portable vault empty");
				VaultModeLabel = L("Importe maFile", "Import maFile");
				ProtectionButtonText = L("Ativar senha", "Enable passphrase");
				StatusMessage = L("Nenhuma conta ainda. Importe um maFile para criar o cofre ao lado do programa.", "No account yet. Import a maFile to create the vault beside the app.");
				_shouldShowWelcomeExperience = true;
				UpdateTimerState();
				UpdateCodeState();
				break;
			}
			catch (Exception ex5)
			{
				StatusMessage = ex5.Message;
				break;
			}
		}
	}

	public async Task ImportAccountAsync(string sourceFilePath, Func<string, string?> requestPassphrase)
	{
		string sourcePassphrase = null;
		string destinationPassphrase = _vaultPassphrase;
		while (true)
		{
			try
			{
				ImportAccountResult importResult = await RunVaultOperationAsync(() => _vaultService.ImportAccount(sourceFilePath, sourcePassphrase, destinationPassphrase, _vaultRoot));
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(importResult.ActivePassphrase, importResult.VaultRoot)), importResult.ActivePassphrase, importResult.ImportedSteamId);
				StatusMessage = "Conta importada com sucesso: " + (SelectedAccount?.DisplayName ?? importResult.ImportedSteamId.ToString());
				break;
			}
			catch (VaultSourcePassphraseRequiredException)
			{
				sourcePassphrase = requestPassphrase("Digite a senha do maFile de origem para importar esta conta.");
				if (string.IsNullOrWhiteSpace(sourcePassphrase))
				{
					StatusMessage = "Importacao cancelada.";
					break;
				}
			}
			catch (VaultSourcePassphraseInvalidException)
			{
				sourcePassphrase = requestPassphrase("Senha invalida para o maFile de origem. Tente novamente.");
				if (string.IsNullOrWhiteSpace(sourcePassphrase))
				{
					StatusMessage = "Importacao cancelada.";
					break;
				}
			}
			catch (VaultDestinationPassphraseRequiredException)
			{
				destinationPassphrase = requestPassphrase("Digite a senha do seu cofre local para salvar a conta importada.");
				if (string.IsNullOrWhiteSpace(destinationPassphrase))
				{
					StatusMessage = "Importacao cancelada.";
					break;
				}
			}
			catch (Exception ex4)
			{
				StatusMessage = "Nao foi possivel importar o maFile: " + ex4.Message;
				break;
			}
		}
	}

	public async Task ImportVaultFolderAsync(string sourceFolderPath, Func<string, string?> requestPassphrase)
	{
		string sourcePassphrase = null;
		string destinationPassphrase = _vaultPassphrase;
		while (true)
		{
			try
			{
				ImportVaultFolderResult importResult = await RunVaultOperationAsync(() => _vaultService.ImportVaultFolder(sourceFolderPath, sourcePassphrase, destinationPassphrase, _vaultRoot));
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(importResult.ActivePassphrase, importResult.VaultRoot)), importResult.ActivePassphrase);
				StatusMessage = ((importResult.ImportedCount == 1) ? "1 conta importada da pasta maFiles." : $"{importResult.ImportedCount} contas importadas da pasta maFiles.");
				_shouldShowWelcomeExperience = false;
				break;
			}
			catch (VaultPassphraseRequiredException)
			{
				sourcePassphrase = requestPassphrase("Digite a senha da pasta maFiles de origem para importar as contas.");
				if (string.IsNullOrWhiteSpace(sourcePassphrase))
				{
					StatusMessage = "Importacao da pasta cancelada.";
					break;
				}
			}
			catch (VaultPassphraseInvalidException)
			{
				sourcePassphrase = requestPassphrase("Senha invalida para a pasta maFiles de origem. Tente novamente.");
				if (string.IsNullOrWhiteSpace(sourcePassphrase))
				{
					StatusMessage = "Importacao da pasta cancelada.";
					break;
				}
			}
			catch (VaultDestinationPassphraseRequiredException)
			{
				destinationPassphrase = requestPassphrase("Digite a senha do seu cofre local para salvar as contas importadas.");
				if (string.IsNullOrWhiteSpace(destinationPassphrase))
				{
					StatusMessage = "Importacao da pasta cancelada.";
					break;
				}
			}
			catch (Exception ex4)
			{
				StatusMessage = "Nao foi possivel importar a pasta maFiles: " + ex4.Message;
				break;
			}
		}
	}

	public async Task SaveAccountAsync(SteamGuardAccount account, Func<string, string?> requestPassphrase)
	{
		string activePassphrase = _vaultPassphrase;
		while (true)
		{
			try
			{
				SaveAccountResult saveResult = await RunVaultOperationAsync(() => _vaultService.SaveAccount(account, activePassphrase, _vaultRoot));
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(saveResult.ActivePassphrase, saveResult.VaultRoot)), saveResult.ActivePassphrase, saveResult.SavedSteamId);
				StatusMessage = "Conta salva: " + (SelectedAccount?.DisplayName ?? saveResult.SavedSteamId.ToString());
				break;
			}
			catch (VaultDestinationPassphraseRequiredException)
			{
				activePassphrase = requestPassphrase("Digite a senha do seu cofre para salvar a conta.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Salvamento cancelado.";
					break;
				}
			}
			catch (VaultPassphraseRequiredException)
			{
				activePassphrase = requestPassphrase("Digite a senha do seu cofre para continuar.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Operacao cancelada.";
					break;
				}
			}
			catch (VaultPassphraseInvalidException)
			{
				activePassphrase = requestPassphrase("Senha invalida. Digite novamente a senha do seu cofre.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Operacao cancelada.";
					break;
				}
			}
		}
	}

	public string ExportVaultArchive(string destinationArchivePath)
	{
		return _vaultService.ExportVaultArchive(destinationArchivePath, _vaultRoot);
	}

	public async Task UpdateProtectionAsync(string? newPassphrase, Func<string, string?> requestPassphrase)
	{
		string activePassphrase = _vaultPassphrase;
		while (true)
		{
			try
			{
				VaultProtectionResult protectionResult = await RunVaultOperationAsync(() => _vaultService.UpdateProtection(activePassphrase, newPassphrase, _vaultRoot));
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(protectionResult.ActivePassphrase, protectionResult.VaultRoot)), protectionResult.ActivePassphrase, SelectedAccount?.SteamId);
				StatusMessage = (protectionResult.IsEncrypted ? "Senha do cofre atualizada com sucesso." : "Senha do cofre removida com sucesso.");
				break;
			}
			catch (VaultPassphraseRequiredException)
			{
				activePassphrase = requestPassphrase("Digite a senha atual do seu cofre para continuar.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Alteracao do cofre cancelada.";
					break;
				}
			}
			catch (VaultPassphraseInvalidException)
			{
				activePassphrase = requestPassphrase("Senha atual invalida. Digite novamente a senha do seu cofre.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Alteracao do cofre cancelada.";
					break;
				}
			}
		}
	}

	public async Task RemoveAccountAsync(AccountItemViewModel account, bool deleteMaFile, Func<string, string?> requestPassphrase)
	{
		string activePassphrase = _vaultPassphrase;
		while (true)
		{
			try
			{
				RemoveAccountResult removeResult = await RunVaultOperationAsync(() => _vaultService.RemoveAccount(account.SteamId, deleteMaFile, _vaultRoot));
				if (!removeResult.AccountRemoved)
				{
					StatusMessage = "A conta selecionada nao foi encontrada no manifest.";
					break;
				}
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(activePassphrase, removeResult.VaultRoot)), activePassphrase);
				StatusMessage = (deleteMaFile ? ("Conta removida do cofre: " + account.DisplayName) : ("Conta removida do manifest: " + account.DisplayName));
				break;
			}
			catch (VaultPassphraseRequiredException)
			{
				activePassphrase = requestPassphrase("Digite a senha do seu cofre para continuar.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Operacao cancelada.";
					break;
				}
			}
			catch (VaultPassphraseInvalidException)
			{
				activePassphrase = requestPassphrase("Senha invalida. Digite novamente a senha do seu cofre.");
				if (string.IsNullOrWhiteSpace(activePassphrase))
				{
					StatusMessage = "Operacao cancelada.";
					break;
				}
			}
		}
	}

	public async Task<VaultReloadOutcome> ReloadAccountsAsync(Func<string, string?> requestPassphrase, bool triggeredByExternalChange = false)
	{
		string passphrase = _vaultPassphrase;
		ulong? selectedSteamId = SelectedAccount?.SteamId;
		int attempt = 0;
		while (true)
		{
			try
			{
				ApplyVaultState(await RunVaultOperationAsync(() => _vaultService.LoadAccounts(passphrase, _vaultRoot)), passphrase, selectedSteamId);
				if (triggeredByExternalChange)
				{
					StatusMessage = ((Accounts.Count == 0) ? "Cofre atualizado. Nenhuma conta encontrada." : $"Cofre atualizado. {Accounts.Count} contas carregadas.");
				}
				return VaultReloadOutcome.Loaded;
			}
			catch (VaultPassphraseRequiredException)
			{
				passphrase = requestPassphrase("O cofre foi atualizado. Digite a senha para recarregar as contas.");
				if (string.IsNullOrWhiteSpace(passphrase))
				{
					StatusMessage = "Atualizacao do cofre cancelada.";
					return VaultReloadOutcome.Cancelled;
				}
			}
			catch (VaultPassphraseInvalidException)
			{
				passphrase = requestPassphrase("Senha invalida. Digite novamente a senha do cofre para recarregar as contas.");
				if (string.IsNullOrWhiteSpace(passphrase))
				{
					StatusMessage = "Atualizacao do cofre cancelada.";
					return VaultReloadOutcome.Cancelled;
				}
			}
			catch (VaultManifestIncompleteException) when (triggeredByExternalChange && attempt < 8)
			{
				await Task.Delay(ExternalReloadRetryDelay);
			}
			catch (VaultManifestIncompleteException)
			{
				StatusMessage = (triggeredByExternalChange ? "A pasta maFiles ainda esta sendo copiada. Mantendo as contas atuais." : "A pasta maFiles ainda esta sendo copiada. Aguarde alguns segundos.");
				return VaultReloadOutcome.PendingCopy;
			}
			catch (VaultNotFoundException) when (triggeredByExternalChange && attempt < 8 && VaultFolderHasPendingArtifacts())
			{
				await Task.Delay(ExternalReloadRetryDelay);
			}
			catch (VaultNotFoundException)
			{
				if (triggeredByExternalChange && VaultFolderHasPendingArtifacts())
				{
					StatusMessage = "A pasta maFiles ainda esta sendo atualizada. Mantendo as contas atuais.";
					return VaultReloadOutcome.PendingCopy;
				}
				_vaultRoot = _vaultService.EnsurePortableVaultRootExists();
				_vaultPassphrase = null;
				RaisePropertyChanged("VaultFolderPath");
				Accounts.Clear();
				SelectedAccount = null;
				VaultStatus = L("Cofre portatil vazio", "Portable vault empty");
				VaultModeLabel = L("Importe maFile", "Import maFile");
				ProtectionButtonText = L("Ativar senha", "Enable passphrase");
				StatusMessage = L("Nenhuma conta ainda. Importe um maFile para criar o cofre ao lado do programa.", "No account yet. Import a maFile to create the vault beside the app.");
				_shouldShowWelcomeExperience = true;
				ResetCodeCache();
				UpdateTimerState();
				UpdateCodeState();
				return VaultReloadOutcome.EmptyVault;
			}
			catch (Exception ex7)
			{
				StatusMessage = (triggeredByExternalChange ? ("Nao foi possivel atualizar o cofre: " + ex7.Message) : ex7.Message);
				return VaultReloadOutcome.Failed;
			}
			attempt++;
		}
	}

	private bool VaultFolderHasPendingArtifacts()
	{
		try
		{
			string vaultFolderPath = VaultFolderPath;
			if (!Directory.Exists(vaultFolderPath))
			{
				return false;
			}
			if (File.Exists(Path.Combine(vaultFolderPath, "manifest.json")))
			{
				return true;
			}
			return Directory.EnumerateFiles(vaultFolderPath, "*.maFile", SearchOption.TopDirectoryOnly).Any();
		}
		catch (IOException)
		{
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return true;
		}
	}

	public void SetStatusMessage(string message)
	{
		StatusMessage = message;
	}

	private void ApplyVaultState(VaultLoadResult result, string? passphrase, ulong? selectSteamId = null)
	{
		_vaultRoot = result.VaultRoot;
		_vaultPassphrase = (result.IsEncrypted ? passphrase : null);
		RaisePropertyChanged("VaultFolderPath");
		VaultStatus = (result.IsEncrypted ? L("Cofre portatil criptografado", "Portable vault encrypted") : L("Cofre portatil carregado", "Portable vault loaded"));
		VaultModeLabel = (result.IsEncrypted ? L("Protegido", "Protected") : L("Sem senha", "No password"));
		ProtectionButtonText = (result.IsEncrypted ? L("Trocar senha", "Change passphrase") : L("Ativar senha", "Enable passphrase"));
		_shouldShowWelcomeExperience = false;
		Accounts.Clear();
		foreach (VaultAccountRecord item in result.Accounts.OrderBy<VaultAccountRecord, string>((VaultAccountRecord x) => x.Account.AccountName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
		{
			Accounts.Add(new AccountItemViewModel
			{
				Account = item.Account,
				DisplayName = (string.IsNullOrWhiteSpace(item.Account.AccountName) ? item.SteamId.ToString() : item.Account.AccountName),
				SourceFile = item.SourceFile,
				SteamId = item.SteamId
			});
		}
		AccountsView.Refresh();
		ResetCodeCache();
		SelectedAccount = (selectSteamId.HasValue ? Accounts.FirstOrDefault((AccountItemViewModel x) => x.SteamId == selectSteamId.Value) : null);
		UpdateTimerState();
	}

	private bool FilterAccounts(object item)
	{
		if (!(item is AccountItemViewModel accountItemViewModel))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(SearchText))
		{
			return true;
		}
		return accountItemViewModel.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
	}

	private void UpdateCodeState()
	{
		if (SelectedAccount == null)
		{
			ResetCodeCache();
			CurrentCode = "-----";
			CountdownLabel = L("Aguardando conta", "Waiting for account");
			ProgressValue = 0.0;
			return;
		}
		long num = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		long num2 = num / 30;
		if (_lastCodeSlice != num2 || _lastCodeSteamId != SelectedAccount.SteamId)
		{
			CurrentCode = SelectedAccount.Account.GenerateSteamGuardCodeForTime(num) ?? "-----";
			_lastCodeSlice = num2;
			_lastCodeSteamId = SelectedAccount.SteamId;
		}
		int num3 = 30 - (int)(num % 30);
		if (num3 == 30)
		{
			num3 = 0;
		}
		ProgressValue = 30 - num3;
		CountdownLabel = ((num3 <= 1) ? L("Atualizando codigo...", "Updating code...") : (_isEnglishLanguage ? $"{num3}s remaining" : $"{num3}s restantes"));
	}

	private void CopyCode()
	{
		if (!string.IsNullOrWhiteSpace(CurrentCode) && !(CurrentCode == "-----"))
		{
			Clipboard.SetText(CurrentCode);
			StatusMessage = L("Codigo copiado para a area de transferencia.", "Code copied to clipboard.");
		}
	}

	private void ShowConfirmations()
	{
		if (SelectedAccount == null)
		{
			return;
		}
		try
		{
			StatusMessage = "Consultando confirmacoes da conta selecionada...";
			Confirmation[] array = SelectedAccount.Account.FetchConfirmations();
			StatusMessage = ((array.Length == 0) ? ("Nenhuma confirmacao pendente para " + SelectedAccount.DisplayName + ".") : $"{array.Length} confirmacoes carregadas para {SelectedAccount.DisplayName}.");
		}
		catch (Exception ex)
		{
			StatusMessage = "Nao foi possivel consultar confirmacoes: " + ex.Message;
		}
	}

	private void CreateAccountPlaceholder()
	{
		AppMessageDialog.Show("A criacao de conta sera migrada na proxima etapa do rewrite. O botao Importar maFile ja traz contas existentes para o app novo.", "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	private void ResetCodeCache()
	{
		_lastCodeSlice = -1L;
		_lastCodeSteamId = 0uL;
	}

	private void UpdateTimerState()
	{
		if (Accounts.Count == 0)
		{
			if (_timer.IsEnabled)
			{
				_timer.Stop();
			}
		}
		else if (!_timer.IsEnabled)
		{
			_timer.Start();
		}
	}

	private static Task<T> RunVaultOperationAsync<T>(Func<T> operation)
	{
		return Task.Run(operation);
	}
}
