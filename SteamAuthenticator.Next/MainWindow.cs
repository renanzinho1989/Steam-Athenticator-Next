using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using SteamAuth;
using SteamAuthenticator.Next.Dialogs;
using SteamAuthenticator.Next.Services;
using SteamAuthenticator.Next.Services.Configuration;
using SteamAuthenticator.Next.ViewModels;

namespace SteamAuthenticator.Next;

public class MainWindow : Window, IComponentConnector, IStyleConnector
{
	private enum ConfirmationFilter
	{
		All,
		Trade,
		Market,
		Pending
	}

	private const string DarkThemeKey = "dark";

	private const string LightThemeKey = "light";

	private const string PortugueseLanguageKey = "pt-BR";

	private const string EnglishLanguageKey = "en-US";

	private static readonly Uri OpenPadlockIconUri = new Uri("pack://application:,,,/Assets/open-padlock.png", UriKind.Absolute);

	private static readonly Uri ClosedPadlockIconUri = new Uri("pack://application:,,,/Assets/lock-padlock-symbol-for-security-interface.png", UriKind.Absolute);

	private static readonly Dictionary<string, string> PtToEnStaticUiText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "Cofre desktop para Steam Guard", "Desktop vault for Steam Guard" },
		{ "Cofre portatil vazio", "Portable vault empty" },
		{ "Cofre portátil vazio", "Portable vault empty" },
		{ "Navegacao", "Navigation" },
		{ "Navegação", "Navigation" },
		{ "Codigo Steam Guard", "Steam Guard Code" },
		{ "Código Steam Guard", "Steam Guard Code" },
		{ "Seu codigo ativo e pronto para autenticacao.", "Your active code is ready for authentication." },
		{ "Seu código ativo e pronto para autenticação.", "Your active code is ready for authentication." },
		{ "Copiar codigo", "Copy code" },
		{ "Copiar código", "Copy code" },
		{ "Conta: nenhuma selecionada", "Account: none selected" },
		{ "nenhuma selecionada", "none selected" },
		{ "Contas", "Accounts" },
		{ "Selecione uma conta para visualizar o codigo atual.", "Select an account to view the current code." },
		{ "Selecione uma conta para visualizar o código atual.", "Select an account to view the current code." },
		{ "Aguardando conta", "Waiting for account" },
		{ "Atualizando codigo...", "Updating code..." },
		{ "Nenhuma conta ainda. Importe um maFile para criar o cofre ao lado do programa.", "No account yet. Import a maFile to create the vault beside the app." },
		{ "Confirmacoes", "Confirmations" },
		{ "Nada pendente para confirmar.", "Nothing pending to confirm." },
		{ "Nova conta", "New account" },
		{ "Adicione uma nova conta Steam ao seu cofre com um fluxo guiado.", "Add a new Steam account to your vault with a guided flow." },
		{ "Etapa 1: Login e senha", "Step 1: Login and password" },
		{ "Use o login e a senha da conta Steam que deseja adicionar.", "Use the Steam account login and password you want to add." },
		{ "Senha", "Password" },
		{ "Gerar autenticador", "Generate authenticator" },
		{ "Como funciona", "How it works" },
		{ "Use o login e a senha da conta Steam para iniciarmos o autenticador.", "Use your Steam account login and password so we can start the authenticator." },
		{ "A Steam pode pedir codigo de e-mail, SMS ou autenticador durante o fluxo guiado.", "Steam may ask for email, SMS, or authenticator code during the guided flow." },
		{ "Dicas rapidas", "Quick tips" },
		{ "Verifique sua caixa de entrada, SMS ou app atual.", "Check your inbox, SMS, or current app." },
		{ "Verifique sua caixa de entrada, SMS ou app atual", "Check your inbox, SMS, or current app" },
		{ "Verifique sua caixa de entrada, SMS ou app da Steam.", "Check your inbox, SMS, or current Steam app." },
		{ "Verifique sua caixa de entrada, SMS ou app da Steam", "Check your inbox, SMS, or current Steam app" },
		{ "O codigo expira rapido. Use-o enquanto a Steam pedir.", "The code expires quickly. Use it while Steam is asking for it." },
		{ "O código expira rápido. Use-o enquanto a Steam pedir.", "The code expires quickly. Use it while Steam is asking for it." },
		{ "O codigo expira rapido. Use-o enquanto a Steam pedir", "The code expires quickly. Use it while Steam is asking for it" },
		{ "O código expira rápido. Use-o enquanto a Steam pedir", "The code expires quickly. Use it while Steam is asking for it" },
		{ "O codigo expira rapido. Use-o enquanto a Steam solicitar.", "The code expires quickly. Use it while Steam is asking for it." },
		{ "O código expira rápido. Use-o enquanto a Steam solicitar.", "The code expires quickly. Use it while Steam is asking for it." },
		{ "O codigo expira rapido. Use-o enquanto a Steam solicitar", "The code expires quickly. Use it while Steam is asking for it" },
		{ "O código expira rápido. Use-o enquanto a Steam solicitar", "The code expires quickly. Use it while Steam is asking for it" },
		{ "Mantenha seu cofre protegido com senha forte", "Keep your vault protected with a strong password" },
		{ "Mantenha seu cofre protegido com senha forte.", "Keep your vault protected with a strong password." },
		{ "Preencha login e senha para iniciar a conta nova.", "Fill in username and password to start a new account." },
		{ "Preencha login e senha para iniciar a conta nova", "Fill in username and password to start a new account" },
		{ "Login cancelado ou nao concluido.", "Login canceled or not completed." },
		{ "O codigo e renovado automaticamente. Use-o enquanto a Steam estiver solicitando autenticacao.", "The code is automatically renewed. Use it while Steam is requesting authentication." },
		{ "O código é renovado automaticamente. Use-o enquanto a Steam estiver solicitando autenticação.", "The code is automatically renewed. Use it while Steam is requesting authentication." },
		{ "Conta adicionada ao cofre do app novo.", "Account added to the new app vault." },
		{ "App minimizado para a bandeja.", "App minimized to tray." },
		{ "App restaurado da bandeja.", "App restored from tray." },
		{ "Proteja os maFiles", "Protect the maFiles" },
		{ "Ajuste a interface e o comportamento da janela.", "Adjust interface and window behavior." },
		{ "Controle o ritmo das verificacoes e como o app acompanha pendencias das contas.", "Control check frequency and how the app tracks account pending items." },
		{ "Controle o ritmo das verificações e como o app acompanha pendências das contas.", "Control check frequency and how the app tracks account pending items." },
		{ "Veja onde o cofre esta salvo e acesse acoes rapidas para exportar, importar ou abrir os arquivos.", "See where the vault is stored and use quick actions to export, import, or open files." },
		{ "Veja onde o cofre está salvo e acesse ações rápidas para exportar, importar ou abrir os arquivos.", "See where the vault is stored and use quick actions to export, import, or open files." },
		{ "Ativar senha do cofre", "Enable vault password" },
		{ "Trocar senha do cofre", "Change vault password" },
		{ "Digite a senha que vai proteger o cofre portatil.", "Enter the password that will protect the portable vault." },
		{ "Digite a nova senha do cofre portatil.", "Enter the new vault password." },
		{ "Confirme a nova senha do cofre.", "Confirm the new vault password." },
		{ "Use pelo menos 8 caracteres para conseguir levar o cofre com seguranca no pendrive.", "Use at least 8 characters to safely carry the vault on a USB drive." },
		{ "Use uma senha com pelo menos 8 caracteres.", "Use a password with at least 8 characters." },
		{ "As senhas nao conferem.", "Passwords do not match." },
		{ "O cofre portatil foi atualizado com sucesso.", "The portable vault was updated successfully." },
		{ "O cofre ja esta sem senha.", "The vault already has no password." },
		{ "Remover senha do cofre", "Remove vault password" },
		{ "A senha do cofre foi removida com sucesso.", "Vault password removed successfully." },
		{ "Isso vai remover a senha do cofre portatil e deixar os maFiles sem criptografia.\nDeseja continuar?", "This will remove the portable vault password and leave maFiles unencrypted.\nDo you want to continue?" },
		{ "Continuar", "Continue" },
		{ "Confirmar", "Confirm" },
		{ "Cancelar", "Cancel" },
		{ "Ativar senha", "Enable password" },
		{ "Trocar senha", "Change password" },
		{ "Buscar conta", "Search account" }
	};

	private static readonly Dictionary<string, string> EnToPtStaticUiText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "Desktop vault for Steam Guard", "Cofre desktop para Steam Guard" },
		{ "Portable vault empty", "Cofre portatil vazio" },
		{ "Navigation", "Navegacao" },
		{ "Steam Guard Code", "Codigo Steam Guard" },
		{ "Your active code is ready for authentication.", "Seu codigo ativo e pronto para autenticacao." },
		{ "Copy code", "Copiar codigo" },
		{ "Account: none selected", "Conta: nenhuma selecionada" },
		{ "none selected", "nenhuma selecionada" },
		{ "Accounts", "Contas" },
		{ "Select an account to view the current code.", "Selecione uma conta para visualizar o codigo atual." },
		{ "Waiting for account", "Aguardando conta" },
		{ "Updating code...", "Atualizando codigo..." },
		{ "No account yet. Import a maFile to create the vault beside the app.", "Nenhuma conta ainda. Importe um maFile para criar o cofre ao lado do programa." },
		{ "Confirmations", "Confirmacoes" },
		{ "Nothing pending to confirm.", "Nada pendente para confirmar." },
		{ "New account", "Nova conta" },
		{ "Add a new Steam account to your vault with a guided flow.", "Adicione uma nova conta Steam ao seu cofre com um fluxo guiado." },
		{ "Step 1: Login and password", "Etapa 1: Login e senha" },
		{ "Use the Steam account login and password you want to add.", "Use o login e a senha da conta Steam que deseja adicionar." },
		{ "Password", "Senha" },
		{ "Generate authenticator", "Gerar autenticador" },
		{ "How it works", "Como funciona" },
		{ "Use your Steam account login and password so we can start the authenticator.", "Use o login e a senha da conta Steam para iniciarmos o autenticador." },
		{ "Steam may ask for email, SMS, or authenticator code during the guided flow.", "A Steam pode pedir codigo de e-mail, SMS ou autenticador durante o fluxo guiado." },
		{ "Quick tips", "Dicas rapidas" },
		{ "Check your inbox, SMS, or current app.", "Verifique sua caixa de entrada, SMS ou app atual." },
		{ "Check your inbox, SMS, or current app", "Verifique sua caixa de entrada, SMS ou app atual" },
		{ "Check your inbox, SMS, or current Steam app.", "Verifique sua caixa de entrada, SMS ou app da Steam." },
		{ "Check your inbox, SMS, or current Steam app", "Verifique sua caixa de entrada, SMS ou app da Steam" },
		{ "The code expires quickly. Use it while Steam is asking for it.", "O codigo expira rapido. Use-o enquanto a Steam pedir." },
		{ "The code expires quickly. Use it while Steam is asking for it", "O codigo expira rapido. Use-o enquanto a Steam pedir" },
		{ "Keep your vault protected with a strong password", "Mantenha seu cofre protegido com senha forte" },
		{ "Keep your vault protected with a strong password.", "Mantenha seu cofre protegido com senha forte." },
		{ "Fill in username and password to start a new account.", "Preencha login e senha para iniciar a conta nova." },
		{ "Fill in username and password to start a new account", "Preencha login e senha para iniciar a conta nova" },
		{ "Login canceled or not completed.", "Login cancelado ou nao concluido." },
		{ "The code is automatically renewed. Use it while Steam is requesting authentication.", "O codigo e renovado automaticamente. Use-o enquanto a Steam estiver solicitando autenticacao." },
		{ "Account added to the new app vault.", "Conta adicionada ao cofre do app novo." },
		{ "App minimized to tray.", "App minimizado para a bandeja." },
		{ "App restored from tray.", "App restaurado da bandeja." },
		{ "Protect the maFiles", "Proteja os maFiles" },
		{ "Adjust interface and window behavior.", "Ajuste a interface e o comportamento da janela." },
		{ "Control check frequency and how the app tracks account pending items.", "Controle o ritmo das verificacoes e como o app acompanha pendencias das contas." },
		{ "See where the vault is stored and use quick actions to export, import, or open files.", "Veja onde o cofre esta salvo e acesse acoes rapidas para exportar, importar ou abrir os arquivos." },
		{ "Enable vault password", "Ativar senha do cofre" },
		{ "Change vault password", "Trocar senha do cofre" },
		{ "Enter the password that will protect the portable vault.", "Digite a senha que vai proteger o cofre portatil." },
		{ "Enter the new vault password.", "Digite a nova senha do cofre portatil." },
		{ "Confirm the new vault password.", "Confirme a nova senha do cofre." },
		{ "Use at least 8 characters to safely carry the vault on a USB drive.", "Use pelo menos 8 caracteres para conseguir levar o cofre com seguranca no pendrive." },
		{ "Use a password with at least 8 characters.", "Use uma senha com pelo menos 8 caracteres." },
		{ "Passwords do not match.", "As senhas nao conferem." },
		{ "The portable vault was updated successfully.", "O cofre portatil foi atualizado com sucesso." },
		{ "The vault already has no password.", "O cofre ja esta sem senha." },
		{ "Remove vault password", "Remover senha do cofre" },
		{ "Vault password removed successfully.", "A senha do cofre foi removida com sucesso." },
		{ "This will remove the portable vault password and leave maFiles unencrypted.\nDo you want to continue?", "Isso vai remover a senha do cofre portatil e deixar os maFiles sem criptografia.\nDeseja continuar?" },
		{ "Continue", "Continuar" },
		{ "Confirm", "Confirmar" },
		{ "Cancel", "Cancelar" },
		{ "Enable password", "Ativar senha" },
		{ "Change password", "Trocar senha" },
		{ "Search account", "Buscar conta" }
	};

	private static readonly Dictionary<string, string> PtToEnStaticUiTextNormalized = CreateNormalizedLookup(PtToEnStaticUiText);

	private static readonly Dictionary<string, string> EnToPtStaticUiTextNormalized = CreateNormalizedLookup(EnToPtStaticUiText);

	private readonly SteamAccountWorkflow _steamWorkflow = new SteamAccountWorkflow();

	private readonly PortableAppSettingsService _settingsService = new PortableAppSettingsService();

	private readonly NotifyIcon? _trayIcon;

	private bool _welcomeHandled;

	private bool _allowClose;

	private bool _trayHintShown;

	private PortableAppSettings _appSettings;

	private readonly DispatcherTimer _vaultReloadDebounceTimer;

	private readonly DispatcherTimer _confirmationsAutoRefreshTimer;

	private readonly ObservableCollection<ConfirmationItem> _embeddedConfirmations = new ObservableCollection<ConfirmationItem>();

	private readonly ICollectionView _embeddedConfirmationsView;

	private TaskCompletionSource<string?>? _inlineNewAccountPromptTcs;

	private string? _inlinePromptCopyValue;

	private FileSystemWatcher? _vaultWatcher;

	private bool _vaultReloadInProgress;

	private bool _vaultWatcherPaused;

	private bool _confirmationsAutoRefreshInProgress;

	private bool _updatingSettingsUi;

	private ConfirmationFilter _activeConfirmationFilter;

	private System.Windows.Controls.Button? _titleBarLanguageToggleButton;

	internal System.Windows.Controls.Button MinimizeButton;

	internal System.Windows.Controls.Button MaximizeRestoreButton;

	internal System.Windows.Controls.Button SidebarHomeButton;

	internal TextBlock SidebarHomeText;

	internal System.Windows.Controls.Button SidebarAccountsButton;

	internal TextBlock SidebarAccountsText;

	internal System.Windows.Controls.Button SidebarConfirmationsButton;

	internal TextBlock SidebarConfirmationsText;

	internal System.Windows.Controls.Button SidebarSettingsButton;

	internal TextBlock SidebarSettingsText;

	internal ScrollViewer HomeSection;

	internal System.Windows.Controls.TextBox AccountsSearchTextBox;

	internal System.Windows.Controls.ListBox AccountsList;

	internal Border AccountActionsPanel;

	internal TextBlock HomeConfirmationsSummaryText;

	internal TextBlock HomeProtectionSummaryText;

	internal System.Windows.Controls.CheckBox HomeMinimizeOnCloseCheckBox;

	internal ScrollViewer NewAccountSection;

	internal System.Windows.Controls.TextBox NewAccountUsernameTextBox;

	internal PasswordBox NewAccountPasswordBox;

	internal System.Windows.Controls.Button StartInlineNewAccountButton;

	internal Border InlineNewAccountPromptPanel;

	internal TextBlock InlinePromptTitleText;

	internal TextBlock InlinePromptMessageText;

	internal System.Windows.Controls.TextBox InlinePromptTextBox;

	internal PasswordBox InlinePromptPasswordBox;

	internal System.Windows.Controls.Button InlinePromptCancelButton;

	internal TextBlock InlinePromptCancelButtonText;

	internal System.Windows.Controls.Button InlinePromptConfirmButton;

	internal TextBlock InlinePromptConfirmButtonText;

	internal TextBlock NewAccountFlowStatusText;

	internal ScrollViewer ConfirmationsSection;

	internal System.Windows.Controls.Button ConfirmationsFilterAllButton;

	internal System.Windows.Controls.Button ConfirmationsFilterTradesButton;

	internal System.Windows.Controls.Button ConfirmationsFilterMarketButton;

	internal System.Windows.Controls.Button ConfirmationsFilterPendingButton;

	internal System.Windows.Controls.ComboBox ConfirmationsAccountSelector;

	internal TextBlock ConfirmationsAccountText;

	internal TextBlock ConfirmationsStatusText;

	internal System.Windows.Controls.ListBox ConfirmationsList;

	internal StackPanel ConfirmationsEmptyState;

	internal TextBlock ConfirmationsSummaryTotalText;

	internal TextBlock ConfirmationsSummaryTradeText;

	internal TextBlock ConfirmationsSummaryMarketText;

	internal ScrollViewer SettingsSection;

	internal TextBlock SettingsPageTitleText;

	internal TextBlock SettingsPageSubtitleText;

	internal System.Windows.Shapes.Rectangle SettingsSecurityIcon;

	internal TextBlock SettingsSecurityTitleText;

	internal TextBlock SettingsProtectionPasswordLabelText;

	internal Border SettingsProtectionModeBadge;

	internal TextBlock SettingsProtectionModeText;

	internal TextBlock SettingsProtectionDescriptionText;

	internal TextBlock SettingsLockOnMinimizeLabelText;

	internal System.Windows.Controls.CheckBox LockOnMinimizeSettingsCheckBox;

	internal TextBlock SettingsAutoLockTimeLabelText;

	internal System.Windows.Controls.Button SettingsDisableEncryptionButton;

	internal TextBlock SettingsAppTitleText;

	internal TextBlock SettingsMinimizeOnCloseLabelText;

	internal System.Windows.Controls.CheckBox MinimizeOnCloseSettingsCheckBox;

	internal TextBlock SettingsStartWithWindowsLabelText;

	internal TextBlock SettingsThemeLabelText;

	internal System.Windows.Controls.ComboBox ThemeSettingsComboBox;

	internal ComboBoxItem ThemeDarkComboBoxItem;

	internal ComboBoxItem ThemeLightComboBoxItem;

	internal TextBlock SettingsLanguageLabelText;

	internal System.Windows.Controls.ComboBox LanguageSettingsComboBox;

	internal ComboBoxItem LanguagePortugueseComboBoxItem;

	internal ComboBoxItem LanguageEnglishComboBoxItem;

	internal TextBlock SettingsVerificationsTitleText;

	internal TextBlock SettingsAutomaticVerificationsLabelText;

	internal System.Windows.Controls.CheckBox AutomaticConfirmationsSettingsCheckBox;

	internal TextBlock SettingsVerificationIntervalLabelText;

	internal System.Windows.Controls.ComboBox VerificationIntervalSettingsComboBox;

	internal TextBlock SettingsVerifyAllAccountsLabelText;

	internal System.Windows.Controls.CheckBox VerifyAllAccountsSettingsCheckBox;

	internal TextBlock SettingsBackupTitleText;

	internal TextBlock SettingsVaultPathLabelText;

	internal TextBlock SettingsVaultPathText;

	internal TextBlock SettingsExportBackupText;

	internal TextBlock SettingsImportBackupText;

	internal TextBlock SettingsOpenMaFilesText;

	internal TextBlock SettingsRestoreDefaultsText;

	internal TextBlock SettingsSaveChangesText;

	private bool _contentLoaded;

	private MainWindowViewModel ViewModel => (MainWindowViewModel)base.DataContext;

	public MainWindow()
	{
		_appSettings = _settingsService.Load();
		ApplyTheme();
		InitializeComponent();
		EnsureTitleBarLanguageHint();
		ApplyLanguage();
		_trayIcon = CreateTrayIcon();
		_embeddedConfirmationsView = CollectionViewSource.GetDefaultView(_embeddedConfirmations);
		_embeddedConfirmationsView.Filter = FilterEmbeddedConfirmation;
		ConfirmationsList.ItemsSource = _embeddedConfirmationsView;
		_vaultReloadDebounceTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(800L, 0L)
		};
		_vaultReloadDebounceTimer.Tick += OnVaultReloadDebounceTick;
		_confirmationsAutoRefreshTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(5L)
		};
		_confirmationsAutoRefreshTimer.Tick += ConfirmationsAutoRefreshTimer_Tick;
		ShowHomeSection();
		UpdateSettingsSection();
	}

	private bool IsEnglishLanguageSelected()
	{
		return string.Equals(_appSettings.Language, "en-US", StringComparison.OrdinalIgnoreCase);
	}

	private string L(string ptBr, string english)
	{
		if (!IsEnglishLanguageSelected())
		{
			return ptBr;
		}
		return english;
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		await ViewModel.InitializeAsync(RequestPassphrase);
		ConfigureVaultWatcher(ViewModel.VaultFolderPath);
		UpdateSettingsSection();
		if (!_welcomeHandled && ViewModel.ShouldShowWelcomeExperience)
		{
			_welcomeHandled = true;
			await RunWelcomeExperienceAsync();
			UpdateSettingsSection();
		}
		ApplyUnboundUiLanguage();
	}

	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton != MouseButton.Left)
		{
			return;
		}
		if (e.ClickCount == 2)
		{
			ToggleMaximizeRestore();
			return;
		}
		try
		{
			DragMove();
		}
		catch
		{
		}
	}

	private void MinimizeButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
	{
		ToggleMaximizeRestore();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void Window_StateChanged(object? sender, EventArgs e)
	{
		if (MaximizeRestoreButton != null)
		{
			MaximizeRestoreButton.Content = ((base.WindowState == WindowState.Maximized) ? "\ue923" : "\ue922");
		}
	}

	private void ToggleMaximizeRestore()
	{
		base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	private void Window_Activated(object? sender, EventArgs e)
	{
		if (_vaultWatcher != null && !_vaultReloadInProgress)
		{
			ScheduleVaultReload();
		}
	}

	private void AccountsArea_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (TryScrollListBox(AccountsList, e.Delta))
		{
			e.Handled = true;
			return;
		}
		if (e.Delta > 0)
		{
			HomeSection.LineUp();
		}
		else if (e.Delta < 0)
		{
			HomeSection.LineDown();
		}
		e.Handled = true;
	}

	private void SectionScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is ScrollViewer scrollViewer)
		{
			if (e.Delta > 0)
			{
				scrollViewer.LineUp();
			}
			else if (e.Delta < 0)
			{
				scrollViewer.LineDown();
			}
			e.Handled = true;
		}
	}

	private async void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (AccountsList.SelectedItem != null)
		{
			await base.Dispatcher.InvokeAsync(delegate
			{
			}, DispatcherPriority.Background);
			AccountsList.UpdateLayout();
			if (AccountsList.ItemContainerGenerator.ContainerFromItem(AccountsList.SelectedItem) is ListBoxItem listBoxItem)
			{
				listBoxItem.BringIntoView();
			}
		}
	}

	private void ToggleAccountActions_Click(object sender, RoutedEventArgs e)
	{
		if (!((sender as FrameworkElement)?.Tag is AccountItemViewModel accountItemViewModel))
		{
			return;
		}
		bool isActionsExpanded = !accountItemViewModel.IsActionsExpanded;
		foreach (AccountItemViewModel account in ViewModel.Accounts)
		{
			account.IsActionsExpanded = false;
		}
		accountItemViewModel.IsActionsExpanded = isActionsExpanded;
		ViewModel.SelectedAccount = accountItemViewModel;
		e.Handled = true;
	}

	public Rect? GetNewAccountPromptPlacement()
	{
		if (NewAccountSection.Visibility != Visibility.Visible || !NewAccountPasswordBox.IsLoaded || !StartInlineNewAccountButton.IsLoaded)
		{
			return null;
		}
		System.Windows.Point point = NewAccountPasswordBox.TranslatePoint(new System.Windows.Point(0.0, 0.0), this);
		System.Windows.Point point2 = StartInlineNewAccountButton.TranslatePoint(new System.Windows.Point(0.0, StartInlineNewAccountButton.ActualHeight), this);
		return new Rect(point.X, point2.Y + 10.0, StartInlineNewAccountButton.ActualWidth, 0.0);
	}

	private string? RequestPassphrase(string prompt)
	{
		PassphraseDialog passphraseDialog = new PassphraseDialog
		{
			Owner = this,
			Prompt = prompt
		};
		if (passphraseDialog.ShowDialog() != true)
		{
			return null;
		}
		return passphraseDialog.Passphrase;
	}

	private async void ImportMaFile_Click(object sender, RoutedEventArgs e)
	{
		switch (AppMessageDialog.Show(this, "Clique em Sim para importar a pasta maFiles inteira.\nClique em Nao para importar um arquivo .maFile ou um .zip exportado.", "Steam Authenticator Next", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
		{
		case MessageBoxResult.Yes:
			await ImportVaultFolderAsync();
			break;
		case MessageBoxResult.No:
		{
			Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
			{
				Title = "Importar maFile",
				Filter = "Arquivos suportados (*.maFile;*.zip)|*.maFile;*.zip|Arquivos maFile (*.maFile)|*.maFile|Arquivos ZIP (*.zip)|*.zip|Todos os arquivos (*.*)|*.*",
				CheckFileExists = true,
				Multiselect = false
			};
			if (dialog.ShowDialog(this) != true)
			{
				break;
			}
			if (string.Equals(System.IO.Path.GetExtension(dialog.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
			{
				await ImportVaultArchiveAsync(dialog.FileName);
				break;
			}
			await RunWithVaultWatcherPausedAsync(() => ViewModel.ImportAccountAsync(dialog.FileName, RequestPassphrase));
			break;
		}
		}
	}

	private async Task ImportVaultFolderAsync()
	{
		FolderBrowserDialog dialog = new FolderBrowserDialog
		{
			Description = "Selecione a pasta maFiles que voce quer importar para o app portatil.",
			UseDescriptionForTitle = true,
			ShowNewFolderButton = false
		};
		try
		{
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
			{
				await RunWithVaultWatcherPausedAsync(() => ViewModel.ImportVaultFolderAsync(dialog.SelectedPath, RequestPassphrase));
			}
		}
		finally
		{
			if (dialog != null)
			{
				((IDisposable)dialog).Dispose();
			}
		}
	}

	private async Task ImportVaultArchiveAsync(string archivePath)
	{
		string tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SteamAuthenticatorNext-Import", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempRoot);
		try
		{
			ZipFile.ExtractToDirectory(archivePath, tempRoot);
			string extractedVaultRoot = FindExtractedVaultRoot(tempRoot) ?? throw new InvalidOperationException("O .zip selecionado nao contem uma pasta maFiles valida.");
			await RunWithVaultWatcherPausedAsync(() => ViewModel.ImportVaultFolderAsync(extractedVaultRoot, RequestPassphrase));
		}
		finally
		{
			TryDeleteDirectory(tempRoot);
		}
	}

	private async void NewAccount_Click(object sender, RoutedEventArgs e)
	{
		LoginDialogResult loginDialogResult = LoginDialog.Request(this, "Entre na sua conta Steam para vincular um novo autenticador.");
		if (!(loginDialogResult == null))
		{
			await AddNewAccountAsync(loginDialogResult.Username, loginDialogResult.Password, showInlineStatus: false);
		}
	}

	private async void StartInlineNewAccount_Click(object sender, RoutedEventArgs e)
	{
		await AddNewAccountAsync(NewAccountUsernameTextBox.Text, NewAccountPasswordBox.Password, showInlineStatus: true);
	}

	private void ConfirmInlineCode_Click(object sender, RoutedEventArgs e)
	{
		NewAccountFlowStatusText.Text = L("Quando a Steam pedir codigo de e-mail, SMS ou autenticador, siga as janelas guiadas do fluxo.", "When Steam asks for email, SMS, or authenticator code, follow the guided flow windows.");
		AppMessageDialog.Show(this, L("Os codigos extras aparecem nas janelas guiadas da Steam durante o fluxo.", "Extra codes appear in Steam guided windows during the flow."), L("Nova conta", "New account"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	public async Task<string?> RequestInlineNewAccountPromptAsync(string title, string prompt, string hint = "", bool isPassword = false, string confirmText = "Confirmar", string cancelText = "Cancelar")
	{
		await base.Dispatcher.InvokeAsync(delegate
		{
			ShowNewAccountSection();
			_inlineNewAccountPromptTcs?.TrySetResult(null);
			_inlineNewAccountPromptTcs = new TaskCompletionSource<string>();
			_inlinePromptCopyValue = null;
			InlinePromptTitleText.Text = title;
			InlinePromptMessageText.Text = (string.IsNullOrWhiteSpace(hint) ? prompt : (prompt + "\n" + hint));
			InlinePromptCancelButtonText.Text = cancelText;
			InlinePromptConfirmButtonText.Text = confirmText;
			InlinePromptTextBox.Text = string.Empty;
			InlinePromptPasswordBox.Password = string.Empty;
			InlinePromptCancelButton.Visibility = Visibility.Visible;
			InlinePromptConfirmButton.Width = 118.0;
			InlinePromptTextBox.Visibility = (isPassword ? Visibility.Collapsed : Visibility.Visible);
			InlinePromptPasswordBox.Visibility = ((!isPassword) ? Visibility.Collapsed : Visibility.Visible);
			InlineNewAccountPromptPanel.Visibility = Visibility.Visible;
			if (isPassword)
			{
				InlinePromptPasswordBox.Focus();
			}
			else
			{
				InlinePromptTextBox.Focus();
				InlinePromptTextBox.SelectAll();
			}
		});
		string result = await _inlineNewAccountPromptTcs.Task;
		await base.Dispatcher.InvokeAsync(HideInlineNewAccountPrompt);
		return result;
	}

	public async Task ShowInlineNewAccountMessageAsync(string title, string message, string confirmText = "OK", string? copyButtonText = null, string? textToCopy = null)
	{
		await base.Dispatcher.InvokeAsync(delegate
		{
			ShowNewAccountSection();
			_inlineNewAccountPromptTcs?.TrySetResult(null);
			_inlineNewAccountPromptTcs = new TaskCompletionSource<string>();
			bool flag = !string.IsNullOrWhiteSpace(copyButtonText) && !string.IsNullOrWhiteSpace(textToCopy);
			_inlinePromptCopyValue = (flag ? textToCopy : null);
			InlinePromptTitleText.Text = title;
			InlinePromptMessageText.Text = message;
			InlinePromptCancelButton.Visibility = (flag ? Visibility.Visible : Visibility.Collapsed);
			if (flag)
			{
				InlinePromptCancelButtonText.Text = TranslateStaticUiText(copyButtonText, IsEnglishLanguageSelected());
			}
			InlinePromptConfirmButtonText.Text = confirmText;
			InlinePromptConfirmButton.Width = 96.0;
			InlinePromptTextBox.Text = string.Empty;
			InlinePromptPasswordBox.Password = string.Empty;
			InlinePromptTextBox.Visibility = Visibility.Collapsed;
			InlinePromptPasswordBox.Visibility = Visibility.Collapsed;
			InlineNewAccountPromptPanel.Visibility = Visibility.Visible;
			InlinePromptConfirmButton.Focus();
		});
		TaskCompletionSource<string> inlineNewAccountPromptTcs = _inlineNewAccountPromptTcs;
		if (inlineNewAccountPromptTcs != null)
		{
			await inlineNewAccountPromptTcs.Task;
		}
		await base.Dispatcher.InvokeAsync(HideInlineNewAccountPrompt);
	}

	private void ConfirmInlinePrompt_Click(object sender, RoutedEventArgs e)
	{
		string result = ((InlinePromptPasswordBox.Visibility == Visibility.Visible) ? InlinePromptPasswordBox.Password : InlinePromptTextBox.Text);
		_inlineNewAccountPromptTcs?.TrySetResult(result);
	}

	private void CancelInlinePrompt_Click(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(_inlinePromptCopyValue))
		{
			System.Windows.Clipboard.SetText(_inlinePromptCopyValue);
			NewAccountFlowStatusText.Text = L("Codigo de revogacao copiado.", "Revocation code copied.");
			return;
		}
		_inlineNewAccountPromptTcs?.TrySetResult(null);
	}

	private void HideInlineNewAccountPrompt()
	{
		InlineNewAccountPromptPanel.Visibility = Visibility.Collapsed;
		InlinePromptTextBox.Text = string.Empty;
		InlinePromptPasswordBox.Password = string.Empty;
		_inlinePromptCopyValue = null;
		InlinePromptCancelButton.Visibility = Visibility.Visible;
		InlinePromptConfirmButton.Width = 118.0;
	}

	private async Task AddNewAccountAsync(string username, string password, bool showInlineStatus)
	{
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			if (showInlineStatus)
			{
				NewAccountFlowStatusText.Text = L("Preencha usuario e senha para continuar.", "Fill in username and password to continue.");
				await ShowInlineNewAccountMessageAsync("Steam Authenticator Next", L("Usuario e senha sao obrigatorios.", "Username and password are required."));
			}
			else
			{
				AppMessageDialog.Show(this, L("Usuario e senha sao obrigatorios.", "Username and password are required."), "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			return;
		}
		if (showInlineStatus)
		{
			ShowNewAccountSection();
			NewAccountFlowStatusText.Text = L("Fazendo login na Steam...", "Logging in to Steam...");
		}
		SessionData session = await _steamWorkflow.LoginAsync(this, username, password, null);
		if (session == null)
		{
			if (showInlineStatus)
			{
				NewAccountFlowStatusText.Text = _steamWorkflow.LastLoginFailureSummary ?? L("Login cancelado ou nao concluido.", "Login canceled or not completed.");
			}
			return;
		}
		if (showInlineStatus)
		{
			NewAccountFlowStatusText.Text = L("Login feito. Continue o fluxo guiado da Steam para vincular o autenticador.", "Login done. Continue the Steam guided flow to link the authenticator.");
		}
		MessageBoxResult messageBoxResult = MessageBoxResult.OK;
		if (!showInlineStatus)
		{
			messageBoxResult = AppMessageDialog.Show(this, L("Login feito com sucesso. Clique em OK para continuar e adicionar o autenticador.", "Login completed successfully. Click OK to continue and add the authenticator."), "Steam Authenticator Next", MessageBoxButton.OKCancel, MessageBoxImage.Asterisk);
		}
		if (messageBoxResult != MessageBoxResult.OK)
		{
			if (showInlineStatus)
			{
				NewAccountFlowStatusText.Text = L("Vinculacao interrompida antes da confirmacao.", "Linking stopped before confirmation.");
			}
			return;
		}
		if (showInlineStatus)
		{
			NewAccountFlowStatusText.Text = L("Conectando o autenticador e aguardando as confirmacoes da Steam...", "Connecting authenticator and waiting for Steam confirmations...");
		}
		SteamGuardAccount linkedAccount = await _steamWorkflow.LinkNewAuthenticatorAsync(this, session);
		if (linkedAccount == null)
		{
			if (showInlineStatus)
			{
				NewAccountFlowStatusText.Text = L("Nao foi possivel concluir a vinculacao da conta.", "Could not finish account linking.");
			}
			return;
		}
		SteamGuardAccount steamGuardAccount = linkedAccount;
		if (steamGuardAccount.AccountName == null)
		{
			steamGuardAccount.AccountName = username;
		}
		steamGuardAccount = linkedAccount;
		if (steamGuardAccount.Session == null)
		{
			steamGuardAccount.Session = session;
		}
		try
		{
			await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(linkedAccount, RequestPassphrase));
			ViewModel.SetStatusMessage(L("Conta adicionada ao cofre do app novo.", "Account added to the new app vault."));
			if (showInlineStatus)
			{
				NewAccountUsernameTextBox.Clear();
				NewAccountPasswordBox.Clear();
				NewAccountFlowStatusText.Text = L("Conta adicionada ao cofre com sucesso.", "Account added to the vault successfully.");
				ShowHomeSection(SidebarHomeButton);
			}
			if (!showInlineStatus)
			{
				AppMessageDialog.Show(this, L("Conta adicionada ao cofre do app novo.", "Account added to the new app vault."), "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
		}
		catch (Exception ex)
		{
			if (showInlineStatus)
			{
				NewAccountFlowStatusText.Text = ex.Message;
				await ShowInlineNewAccountMessageAsync("Steam Authenticator Next", ex.Message);
			}
			else
			{
				AppMessageDialog.Show(this, ex.Message, "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private void ExportVault_Click(object sender, RoutedEventArgs e)
	{
		string text = DateTime.Now.ToString("yyyyMMdd-HHmm");
		Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
		{
			Title = "Exportar cofre",
			Filter = "Arquivo ZIP (*.zip)|*.zip",
			FileName = "SteamAuthenticatorNext-maFiles-" + text + ".zip",
			AddExtension = true,
			DefaultExt = ".zip",
			OverwritePrompt = true
		};
		if (saveFileDialog.ShowDialog(this) != true)
		{
			return;
		}
		try
		{
			string text2 = ViewModel.ExportVaultArchive(saveFileDialog.FileName);
			ViewModel.SetStatusMessage("Cofre exportado com sucesso.");
			AppMessageDialog.Show(this, "Exportacao concluida com sucesso.\n\nArquivo: " + text2 + "\n\nQuem receber pode importar esse .zip pelo botao Importar conta.", "Exportar cofre", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		catch (Exception ex)
		{
			AppMessageDialog.Show(this, ex.Message, "Exportar cofre", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void OpenSettings_Click(object sender, RoutedEventArgs e)
	{
		ShowSettingsSection();
	}

	private void Exit_Click(object sender, RoutedEventArgs e)
	{
		_allowClose = true;
		if (System.Windows.Application.Current is App app)
		{
			app.ForceExitRequested = true;
		}
		Close();
	}

	private async void LoginAgain_Click(object sender, RoutedEventArgs e)
	{
		SteamGuardAccount selectedAccount = ViewModel.SelectedSteamAccount;
		if (selectedAccount == null)
		{
			AppMessageDialog.Show(this, "Selecione uma conta primeiro.", "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		LoginDialogResult credentials = LoginDialog.Request(this, "Entre novamente para atualizar a sessão da conta selecionada.", selectedAccount.AccountName ?? string.Empty);
		if (credentials == null)
		{
			return;
		}
		SessionData sessionData = await _steamWorkflow.LoginAsync(this, credentials.Username, credentials.Password, selectedAccount);
		if (sessionData != null)
		{
			SteamGuardAccount steamGuardAccount = selectedAccount;
			if (steamGuardAccount.AccountName == null)
			{
				steamGuardAccount.AccountName = credentials.Username;
			}
			selectedAccount.Session = sessionData;
			await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(selectedAccount, RequestPassphrase));
			AppMessageDialog.Show(this, "Sessão atualizada com sucesso.", "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	private async void RemoveFromManifest_Click(object sender, RoutedEventArgs e)
	{
		AccountItemViewModel selectedAccount = ViewModel.SelectedAccount;
		if (selectedAccount == null)
		{
			AppMessageDialog.Show(this, "Selecione uma conta primeiro.", "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		else
		{
			if (AppMessageDialog.Show(this, "Isso remove a conta selecionada do manifest do app novo.\nO arquivo .maFile não será apagado.\nUse esta opção para mover a conta para outro computador.", "Remover do manifest", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
			{
				return;
			}
			try
			{
				await RunWithVaultWatcherPausedAsync(() => ViewModel.RemoveAccountAsync(selectedAccount, deleteMaFile: false, RequestPassphrase));
				AppMessageDialog.Show(this, "Conta removida do manifest. Agora você pode mover o maFile e importar em outro lugar.", "Remover do manifest", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			catch (Exception ex)
			{
				AppMessageDialog.Show(this, ex.Message, "Remover do manifest", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private async void DeactivateAuthenticator_Click(object sender, RoutedEventArgs e)
	{
		AccountItemViewModel selectedAccount = ViewModel.SelectedAccount;
		SteamGuardAccount selectedAccount2 = selectedAccount?.Account;
		if (selectedAccount2 == null)
		{
			AppMessageDialog.Show(this, "Selecione uma conta primeiro.", "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		AccountItemViewModel selectedViewModel = selectedAccount;
		if (!(await EnsureSessionForConfirmationsAsync(selectedAccount2)))
		{
			return;
		}
		int scheme = AppMessageDialog.Show(this, "Deseja remover o Steam Guard completamente?\nSim - remove o Steam Guard completamente.\nNão - volta para autenticação por e-mail.", "Desativar autenticador: " + selectedViewModel.DisplayName, MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) switch
		{
			MessageBoxResult.Yes => 2, 
			MessageBoxResult.No => 1, 
			_ => 0, 
		};
		if (scheme == 0)
		{
			AppMessageDialog.Show(this, "O Steam Guard não foi removido. Nenhuma ação foi tomada.", "Desativar autenticador", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		string text = selectedAccount2.GenerateSteamGuardCode();
		string text2 = TextPromptDialog.Request(this, "Confirmação", "Removendo o Steam Guard de " + selectedViewModel.DisplayName + ". Digite este código de confirmação: " + text);
		if (text2 == null)
		{
			return;
		}
		if (!string.Equals(text2.Trim().ToUpperInvariant(), text, StringComparison.Ordinal))
		{
			AppMessageDialog.Show(this, "Os códigos de confirmação não coincidem. O Steam Guard não foi removido.", "Desativar autenticador", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		try
		{
			if (!(await selectedAccount2.DeactivateAuthenticator(scheme)))
			{
				AppMessageDialog.Show(this, "Falha ao desativar o Steam Guard.", "Desativar autenticador", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
			AppMessageDialog.Show(this, (scheme == 2) ? "Steam Guard removido completamente. O maFile local será excluído agora." : "Steam Guard voltou para e-mail. O maFile local será excluído agora.", "Desativar autenticador", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			await RunWithVaultWatcherPausedAsync(() => ViewModel.RemoveAccountAsync(selectedViewModel, deleteMaFile: true, RequestPassphrase));
		}
		catch (Exception ex)
		{
			AppMessageDialog.Show(this, ex.Message, "Desativar autenticador", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private async void OpenConfirmations_Click(object sender, RoutedEventArgs e)
	{
		await ShowConfirmationsSectionAsync(forceRefresh: true);
	}

	private void ShowHomeSection_Click(object sender, RoutedEventArgs e)
	{
		ShowHomeSection(SidebarHomeButton);
	}

	private void ShowAccountsSection_Click(object sender, RoutedEventArgs e)
	{
		ShowNewAccountSection();
	}

	private async void RefreshConfirmations_Click(object sender, RoutedEventArgs e)
	{
		await ShowConfirmationsSectionAsync(forceRefresh: true);
	}

	private async void ConfirmationsAccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && ConfirmationsSection.Visibility == Visibility.Visible && sender == ConfirmationsAccountSelector && e.AddedItems.Count != 0)
		{
			await ShowConfirmationsSectionAsync(forceRefresh: true);
		}
	}

	private void ConfirmationsFilterAll_Click(object sender, RoutedEventArgs e)
	{
		SetConfirmationFilter(ConfirmationFilter.All);
	}

	private void ConfirmationsFilterTrades_Click(object sender, RoutedEventArgs e)
	{
		SetConfirmationFilter(ConfirmationFilter.Trade);
	}

	private void ConfirmationsFilterMarket_Click(object sender, RoutedEventArgs e)
	{
		SetConfirmationFilter(ConfirmationFilter.Market);
	}

	private void ConfirmationsFilterPending_Click(object sender, RoutedEventArgs e)
	{
		SetConfirmationFilter(ConfirmationFilter.Pending);
	}

	private async void AcceptConfirmation_Click(object sender, RoutedEventArgs e)
	{
		object obj = (sender as FrameworkElement)?.Tag;
		if (!(obj is ConfirmationItem item))
		{
			return;
		}
		SteamGuardAccount account = item.OwnerAccount ?? ViewModel.SelectedSteamAccount;
		if (account == null)
		{
			return;
		}
		try
		{
			ConfirmationsStatusText.Text = "Aceitando confirmacao...";
			if (await EnsureSessionForConfirmationsAsync(account))
			{
				await account.AcceptConfirmation(item.Confirmation);
				await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(account, RequestPassphrase));
				await ShowConfirmationsSectionAsync(forceRefresh: true);
			}
		}
		catch (Exception ex)
		{
			ShowEmbeddedConfirmationsError(ex);
		}
	}

	private async void CancelConfirmation_Click(object sender, RoutedEventArgs e)
	{
		object obj = (sender as FrameworkElement)?.Tag;
		if (!(obj is ConfirmationItem item))
		{
			return;
		}
		SteamGuardAccount account = item.OwnerAccount ?? ViewModel.SelectedSteamAccount;
		if (account == null)
		{
			return;
		}
		try
		{
			ConfirmationsStatusText.Text = "Cancelando confirmacao...";
			if (await EnsureSessionForConfirmationsAsync(account))
			{
				await account.DenyConfirmation(item.Confirmation);
				await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(account, RequestPassphrase));
				await ShowConfirmationsSectionAsync(forceRefresh: true);
			}
		}
		catch (Exception ex)
		{
			ShowEmbeddedConfirmationsError(ex);
		}
	}

	private void ShowHomeSection(System.Windows.Controls.Button? activeButton = null)
	{
		HomeSection.Visibility = Visibility.Visible;
		NewAccountSection.Visibility = Visibility.Collapsed;
		ConfirmationsSection.Visibility = Visibility.Collapsed;
		SettingsSection.Visibility = Visibility.Collapsed;
		UpdateSettingsSection();
		ConfigureConfirmationsAutoRefresh();
		ActivateSidebarButton(activeButton ?? SidebarHomeButton);
	}

	private void ShowNewAccountSection()
	{
		HomeSection.Visibility = Visibility.Collapsed;
		NewAccountSection.Visibility = Visibility.Visible;
		ConfirmationsSection.Visibility = Visibility.Collapsed;
		SettingsSection.Visibility = Visibility.Collapsed;
		ConfigureConfirmationsAutoRefresh();
		ActivateSidebarButton(SidebarAccountsButton);
		if (string.IsNullOrWhiteSpace(NewAccountFlowStatusText.Text))
		{
			NewAccountFlowStatusText.Text = L("Preencha login e senha para iniciar a conta nova.", "Fill in username and password to start a new account.");
		}
	}

	private async Task ShowConfirmationsSectionAsync(bool forceRefresh)
	{
		HomeSection.Visibility = Visibility.Collapsed;
		NewAccountSection.Visibility = Visibility.Collapsed;
		ConfirmationsSection.Visibility = Visibility.Visible;
		SettingsSection.Visibility = Visibility.Collapsed;
		ConfigureConfirmationsAutoRefresh();
		ActivateSidebarButton(SidebarConfirmationsButton);
		bool verifyAllAccounts = _appSettings.VerifyAllAccounts;
		SteamGuardAccount selectedSteamAccount = ViewModel.SelectedSteamAccount;
		List<SteamGuardAccount> list = new List<SteamGuardAccount>();
		if (verifyAllAccounts)
		{
			foreach (AccountItemViewModel account2 in ViewModel.Accounts)
			{
				if (account2.Account != null)
				{
					list.Add(account2.Account);
				}
			}
		}
		else if (selectedSteamAccount != null)
		{
			list.Add(selectedSteamAccount);
		}
		if (list.Count == 0)
		{
			_embeddedConfirmations.Clear();
			ConfirmationsAccountText.Text = (verifyAllAccounts ? L("Nenhuma conta encontrada para verificar.", "No account found to check.") : L("Selecione uma conta para ver as confirmacoes.", "Select an account to see confirmations."));
			ConfirmationsStatusText.Text = L("Nenhuma conta selecionada.", "No account selected.");
			HomeConfirmationsSummaryText.Text = L("Nenhuma conta selecionada.", "No account selected.");
			ConfirmationsEmptyState.Visibility = Visibility.Visible;
			ConfirmationsList.Visibility = Visibility.Collapsed;
			UpdateConfirmationsSummary();
			return;
		}
		ConfirmationsAccountText.Text = (verifyAllAccounts ? (L("Contas monitoradas: ", "Monitored accounts: ") + list.Count) : (L("Conta atual: ", "Current account: ") + (selectedSteamAccount?.AccountName ?? L("sem nome", "unnamed"))));
		ConfirmationsStatusText.Text = (forceRefresh ? L("Atualizando confirmacoes...", "Updating confirmations...") : L("Abrindo confirmacoes...", "Opening confirmations..."));
		HomeConfirmationsSummaryText.Text = L("Atualizando confirmacoes...", "Updating confirmations...");
		try
		{
			_embeddedConfirmations.Clear();
			int checkedAccounts = 0;
			foreach (SteamGuardAccount account in list)
			{
				if (await EnsureSessionForConfirmationsAsync(account))
				{
					checkedAccounts++;
					Confirmation[] array = await account.FetchConfirmationsAsync();
					foreach (Confirmation confirmation in array)
					{
						_embeddedConfirmations.Add(new ConfirmationItem(confirmation, account));
					}
				}
			}
			bool flag = _embeddedConfirmations.Count > 0;
			ApplyConfirmationFilter();
			bool flag2 = _embeddedConfirmationsView.Cast<object>().Any();
			ConfirmationsEmptyState.Visibility = (flag2 ? Visibility.Collapsed : Visibility.Visible);
			ConfirmationsList.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			ConfirmationsStatusText.Text = ((checkedAccounts == 0) ? L("Login cancelado. As confirmacoes nao foram carregadas.", "Login canceled. Confirmations were not loaded.") : ((!flag) ? L("Nada pendente para confirmar.", "Nothing pending to confirm.") : (verifyAllAccounts ? $"{_embeddedConfirmations.Count} {L("confirmacoes carregadas em", "confirmations loaded in")} {checkedAccounts} {L("conta(s).", "account(s).")}" : $"{_embeddedConfirmations.Count} {L("confirmacoes carregadas.", "confirmations loaded.")}")));
			HomeConfirmationsSummaryText.Text = ConfirmationsStatusText.Text;
			UpdateConfirmationsSummary();
		}
		catch (Exception ex)
		{
			ShowEmbeddedConfirmationsError(ex);
		}
	}

	private void ShowEmbeddedConfirmationsError(Exception ex)
	{
		ConfirmationsEmptyState.Visibility = ((_embeddedConfirmations.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		ConfirmationsList.Visibility = ((_embeddedConfirmations.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		ConfirmationsStatusText.Text = L("Nao foi possivel carregar as confirmacoes: ", "Unable to load confirmations: ") + ex.Message;
		HomeConfirmationsSummaryText.Text = ConfirmationsStatusText.Text;
		UpdateConfirmationsSummary();
		AppMessageDialog.Show(this, ex.Message, L("Confirmacoes", "Confirmations"), MessageBoxButton.OK, MessageBoxImage.Hand);
	}

	private void ShowSettingsSection()
	{
		HomeSection.Visibility = Visibility.Collapsed;
		NewAccountSection.Visibility = Visibility.Collapsed;
		ConfirmationsSection.Visibility = Visibility.Collapsed;
		SettingsSection.Visibility = Visibility.Visible;
		UpdateSettingsSection();
		ConfigureConfirmationsAutoRefresh();
		ActivateSidebarButton(SidebarSettingsButton);
	}

	private void ConfigureConfirmationsAutoRefresh()
	{
		_confirmationsAutoRefreshTimer.Interval = TimeSpan.FromMinutes(1L);
		if (_appSettings.AutomaticConfirmationsEnabled && ConfirmationsSection.Visibility == Visibility.Visible)
		{
			_confirmationsAutoRefreshTimer.Start();
		}
		else
		{
			_confirmationsAutoRefreshTimer.Stop();
		}
	}

	private async void ConfirmationsAutoRefreshTimer_Tick(object? sender, EventArgs e)
	{
		if (_confirmationsAutoRefreshInProgress || !_appSettings.AutomaticConfirmationsEnabled || ConfirmationsSection.Visibility != Visibility.Visible)
		{
			return;
		}
		try
		{
			_confirmationsAutoRefreshInProgress = true;
			await ShowConfirmationsSectionAsync(forceRefresh: true);
		}
		finally
		{
			_confirmationsAutoRefreshInProgress = false;
		}
	}

	private void UpdateConfirmationsSummary()
	{
		int count = _embeddedConfirmations.Count;
		int num = 0;
		int num2 = 0;
		foreach (ConfirmationItem embeddedConfirmation in _embeddedConfirmations)
		{
			if (embeddedConfirmation.Kind == ConfirmationKind.Trade)
			{
				num++;
			}
			if (embeddedConfirmation.Kind == ConfirmationKind.Market)
			{
				num2++;
			}
		}
		ConfirmationsSummaryTotalText.Text = count.ToString();
		ConfirmationsSummaryTradeText.Text = num.ToString();
		ConfirmationsSummaryMarketText.Text = num2.ToString();
	}

	private bool FilterEmbeddedConfirmation(object item)
	{
		if (!(item is ConfirmationItem confirmationItem))
		{
			return false;
		}
		return _activeConfirmationFilter switch
		{
			ConfirmationFilter.All => true, 
			ConfirmationFilter.Trade => confirmationItem.Kind == ConfirmationKind.Trade, 
			ConfirmationFilter.Market => confirmationItem.Kind == ConfirmationKind.Market, 
			ConfirmationFilter.Pending => true, 
			_ => true, 
		};
	}

	private void SetConfirmationFilter(ConfirmationFilter filter)
	{
		if (_activeConfirmationFilter != filter)
		{
			_activeConfirmationFilter = filter;
			ApplyConfirmationFilter();
		}
	}

	private void ApplyConfirmationFilter()
	{
		_embeddedConfirmationsView.Refresh();
		UpdateConfirmationFilterButtons();
		bool flag = _embeddedConfirmationsView.Cast<object>().Any();
		ConfirmationsEmptyState.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
		ConfirmationsList.Visibility = ((_embeddedConfirmations.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void UpdateConfirmationFilterButtons()
	{
		UpdateConfirmationFilterButtonState(ConfirmationsFilterAllButton, _activeConfirmationFilter == ConfirmationFilter.All);
		UpdateConfirmationFilterButtonState(ConfirmationsFilterTradesButton, _activeConfirmationFilter == ConfirmationFilter.Trade);
		UpdateConfirmationFilterButtonState(ConfirmationsFilterMarketButton, _activeConfirmationFilter == ConfirmationFilter.Market);
		UpdateConfirmationFilterButtonState(ConfirmationsFilterPendingButton, _activeConfirmationFilter == ConfirmationFilter.Pending);
	}

	private void UpdateConfirmationFilterButtonState(System.Windows.Controls.Button? button, bool isActive)
	{
		if (button != null)
		{
			button.Background = (isActive ? ((System.Windows.Media.Brush)FindResource("AccentBrush")) : ((System.Windows.Media.Brush)FindResource("SurfaceAltBrush")));
			button.BorderBrush = (isActive ? ((System.Windows.Media.Brush)FindResource("AccentBrush")) : ((System.Windows.Media.Brush)FindResource("BorderBrush")));
			button.Foreground = (System.Windows.Media.Brush)FindResource("TextBrush");
		}
	}

	private void UpdateSettingsSection()
	{
		if (!base.IsLoaded)
		{
			return;
		}
		_updatingSettingsUi = true;
		try
		{
			ApplyLanguage();
			bool flag = !string.IsNullOrWhiteSpace(ViewModel.VaultPassphrase);
			UpdateSecurityIcon(flag);
			SettingsProtectionModeText.Text = (flag ? L("Ativada", "Enabled") : L("Sem senha", "No password"));
			SettingsProtectionModeBadge.Background = (flag ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(19, 76, 57)) : ((System.Windows.Media.Brush)FindResource("NeutralBadgeBrush")));
			SettingsProtectionModeText.Foreground = (flag ? ((System.Windows.Media.Brush)FindResource("SuccessBrush")) : ((System.Windows.Media.Brush)FindResource("TextBrush")));
			SettingsProtectionDescriptionText.Text = (flag ? L("Seu cofre portátil está protegido por senha. Você pode trocar a senha atual ou remover a criptografia.", "Your portable vault is protected by password. You can change the current password or remove encryption.") : L("Seu cofre portátil está sem senha. Ative uma senha para deixar os maFiles protegidos no pendrive.", "Your portable vault has no password. Turn on a password to protect the maFiles on your USB drive."));
			SettingsDisableEncryptionButton.IsEnabled = flag;
			SettingsDisableEncryptionButton.Content = L("Desativar", "Disable");
			SettingsVaultPathText.Text = ViewModel.VaultFolderPath;
			HomeProtectionSummaryText.Text = (flag ? L("Seu cofre está protegido por senha e pronto para levar no pendrive.", "Your vault is protected with a password and ready to use on a USB drive.") : L("Seu cofre está sem senha. Ative uma senha para proteger os maFiles.", "Your vault has no password. Turn on a password to protect the maFiles."));
			bool minimizeOnClose = _appSettings.MinimizeOnClose;
			if (MinimizeOnCloseSettingsCheckBox.IsChecked != minimizeOnClose)
			{
				MinimizeOnCloseSettingsCheckBox.IsChecked = minimizeOnClose;
			}
			if (HomeMinimizeOnCloseCheckBox.IsChecked != minimizeOnClose)
			{
				HomeMinimizeOnCloseCheckBox.IsChecked = minimizeOnClose;
			}
			bool automaticConfirmationsEnabled = _appSettings.AutomaticConfirmationsEnabled;
			if (AutomaticConfirmationsSettingsCheckBox.IsChecked != automaticConfirmationsEnabled)
			{
				AutomaticConfirmationsSettingsCheckBox.IsChecked = automaticConfirmationsEnabled;
			}
			int intervalSeconds = ((_appSettings.VerificationIntervalSeconds <= 0) ? 5 : _appSettings.VerificationIntervalSeconds);
			SelectVerificationInterval(intervalSeconds);
			bool verifyAllAccounts = _appSettings.VerifyAllAccounts;
			if (VerifyAllAccountsSettingsCheckBox.IsChecked != verifyAllAccounts)
			{
				VerifyAllAccountsSettingsCheckBox.IsChecked = verifyAllAccounts;
			}
			SelectTheme(_appSettings.Theme);
			SelectLanguage(_appSettings.Language);
			ConfigureConfirmationsAutoRefresh();
			if (_embeddedConfirmations.Count == 0 && string.IsNullOrWhiteSpace(HomeConfirmationsSummaryText.Text))
			{
				HomeConfirmationsSummaryText.Text = L("Nada pendente para confirmar.", "Nothing pending to confirm.");
			}
		}
		finally
		{
			_updatingSettingsUi = false;
		}
	}

	private void UpdateSecurityIcon(bool vaultIsEncrypted)
	{
		if (SettingsSecurityIcon != null)
		{
			SettingsSecurityIcon.OpacityMask = new ImageBrush(new BitmapImage(vaultIsEncrypted ? ClosedPadlockIconUri : OpenPadlockIconUri))
			{
				Stretch = Stretch.Uniform
			};
		}
	}

	private void SelectVerificationInterval(int intervalSeconds)
	{
		foreach (object item in (IEnumerable)VerificationIntervalSettingsComboBox.Items)
		{
			if (item is ComboBoxItem { Tag: var tag } comboBoxItem && int.TryParse(tag?.ToString(), out var result) && result == intervalSeconds)
			{
				VerificationIntervalSettingsComboBox.SelectedItem = comboBoxItem;
				return;
			}
		}
		VerificationIntervalSettingsComboBox.SelectedIndex = 0;
	}

	private void SelectTheme(string theme)
	{
		string b = (string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ? "light" : "dark");
		foreach (object item in (IEnumerable)ThemeSettingsComboBox.Items)
		{
			if (item is ComboBoxItem { Tag: var tag } comboBoxItem && string.Equals(tag?.ToString(), b, StringComparison.OrdinalIgnoreCase))
			{
				ThemeSettingsComboBox.SelectedItem = comboBoxItem;
				return;
			}
		}
		ThemeSettingsComboBox.SelectedIndex = 0;
	}

	private void SelectLanguage(string language)
	{
		string b = (string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "pt-BR");
		foreach (object item in (IEnumerable)LanguageSettingsComboBox.Items)
		{
			if (item is ComboBoxItem { Tag: var tag } comboBoxItem && string.Equals(tag?.ToString(), b, StringComparison.OrdinalIgnoreCase))
			{
				LanguageSettingsComboBox.SelectedItem = comboBoxItem;
				return;
			}
		}
		LanguageSettingsComboBox.SelectedIndex = 0;
	}

	private string GetSelectedTheme()
	{
		if (!(ThemeSettingsComboBox.SelectedItem is ComboBoxItem { Tag: var tag }) || !string.Equals(tag?.ToString(), "light", StringComparison.OrdinalIgnoreCase))
		{
			return "dark";
		}
		return "light";
	}

	private string GetSelectedLanguage()
	{
		if (!(LanguageSettingsComboBox.SelectedItem is ComboBoxItem { Tag: var tag }) || !string.Equals(tag?.ToString(), "en-US", StringComparison.OrdinalIgnoreCase))
		{
			return "pt-BR";
		}
		return "en-US";
	}

	private void ApplyLanguage()
	{
		SidebarHomeText.Text = L("Contas Steam", "Steam Accounts");
		SidebarAccountsText.Text = L("Nova Conta", "New Account");
		SidebarConfirmationsText.Text = L("Confirmacoes", "Confirmations");
		SidebarSettingsText.Text = L("Configuracoes", "Settings");
		SettingsPageTitleText.Text = L("Configurações", "Settings");
		SettingsPageSubtitleText.Text = L("Ajustes do aplicativo e do cofre portátil.", "App and portable vault settings.");
		SettingsSecurityTitleText.Text = L("Segurança do cofre", "Vault security");
		SettingsProtectionPasswordLabelText.Text = L("Proteção por senha", "Password protection");
		SettingsLockOnMinimizeLabelText.Text = L("Bloquear ao minimizar", "Lock on minimize");
		SettingsAutoLockTimeLabelText.Text = L("Tempo para bloqueio automático", "Auto lock timeout");
		SettingsAppTitleText.Text = L("Aplicativo", "Application");
		SettingsMinimizeOnCloseLabelText.Text = L("Minimizar para a bandeja ao fechar", "Minimize to tray on close");
		SettingsStartWithWindowsLabelText.Text = L("Iniciar com o Windows", "Start with Windows");
		SettingsThemeLabelText.Text = L("Tema", "Theme");
		SettingsLanguageLabelText.Text = L("Idioma", "Language");
		SettingsVerificationsTitleText.Text = L("Verificações e confirmações", "Checks and confirmations");
		SettingsAutomaticVerificationsLabelText.Text = L("Verificações automáticas", "Automatic checks");
		SettingsVerificationIntervalLabelText.Text = L("Intervalo entre verificações", "Check interval");
		SettingsVerifyAllAccountsLabelText.Text = L("Verificar todas as contas", "Check all accounts");
		SettingsBackupTitleText.Text = L("Backup e dados", "Backup and data");
		SettingsVaultPathLabelText.Text = L("Pasta maFiles", "maFiles folder");
		SettingsExportBackupText.Text = L("Exportar backup", "Export backup");
		SettingsImportBackupText.Text = L("Importar backup", "Import backup");
		SettingsOpenMaFilesText.Text = L("Abrir maFiles", "Open maFiles");
		SettingsRestoreDefaultsText.Text = L("Restaurar padrão", "Restore defaults");
		SettingsSaveChangesText.Text = L("Salvar alterações", "Save changes");
		ThemeDarkComboBoxItem.Content = L("Escuro", "Dark");
		ThemeLightComboBoxItem.Content = L("Claro", "Light");
		LanguagePortugueseComboBoxItem.Content = "Português (Brasil)";
		LanguageEnglishComboBoxItem.Content = "English";
		UpdateTitleBarLanguageToggleText();
		ViewModel.SetLanguage(_appSettings.Language);
		ApplyUnboundUiLanguage();
	}

	private void EnsureTitleBarLanguageHint()
	{
		if (MinimizeButton == null || _titleBarLanguageToggleButton != null || !(VisualTreeHelper.GetParent(MinimizeButton) is System.Windows.Controls.Panel panel))
		{
			return;
		}
		System.Windows.Controls.Button button = new System.Windows.Controls.Button
		{
			Content = "PT - EN",
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
			MinWidth = 58.0,
			Height = 26.0,
			Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
			FontSize = 10.5,
			FontWeight = FontWeights.SemiBold,
			Opacity = 0.95
		};
		button.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "TextBrush");
		button.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "InputBackgroundBrush");
		button.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "BorderBrush");
		button.Click += TitleBarLanguageToggleButton_Click;
		int num = panel.Children.IndexOf(MinimizeButton);
		if (num >= 0)
		{
			panel.Children.Insert(num, button);
		}
		else
		{
			panel.Children.Add(button);
		}
		_titleBarLanguageToggleButton = button;
		UpdateTitleBarLanguageToggleText();
	}

	private void TitleBarLanguageToggleButton_Click(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		string text = (IsEnglishLanguageSelected() ? "pt-BR" : "en-US");
		if (string.Equals(_appSettings.Language, text, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		_appSettings.Language = text;
		_settingsService.Save(_appSettings);
		ApplyLanguage();
		UpdateSettingsSection();
		ViewModel.SetStatusMessage(L("Idioma atualizado com sucesso.", "Language updated successfully."));
	}

	private void UpdateTitleBarLanguageToggleText()
	{
		if (_titleBarLanguageToggleButton != null)
		{
			_titleBarLanguageToggleButton.Content = (IsEnglishLanguageSelected() ? "EN - PT" : "PT - EN");
		}
	}

	private void ApplyUnboundUiLanguage()
	{
		bool flag = IsEnglishLanguageSelected();
		foreach (DependencyObject item in EnumerateVisualTree(this))
		{
			if (item is Run run)
			{
				string text = TranslateStaticUiText(run.Text, flag);
				if (!string.Equals(run.Text, text, StringComparison.Ordinal))
				{
					run.Text = text;
				}
			}
			else if (item is TextBlock textBlock)
			{
				string text = TranslateStaticUiText(textBlock.Text, flag);
				if (!string.Equals(textBlock.Text, text, StringComparison.Ordinal))
				{
					textBlock.Text = text;
				}
			}
			else if (item is HeaderedContentControl headeredContentControl && headeredContentControl.Header is string text2)
			{
				string text3 = TranslateStaticUiText(text2, flag);
				if (!string.Equals(text2, text3, StringComparison.Ordinal))
				{
					headeredContentControl.Header = text3;
				}
			}
			else if (item is ContentControl contentControl && contentControl.Content is string text4)
			{
				string text5 = TranslateStaticUiText(text4, flag);
				if (!string.Equals(text4, text5, StringComparison.Ordinal))
				{
					contentControl.Content = text5;
				}
			}
		}
		if (AccountsSearchTextBox != null)
		{
			string text6 = L("Buscar conta", "Search account");
			AccountsSearchTextBox.Tag = text6;
			AccountsSearchTextBox.ToolTip = text6;
		}
	}

	private static string TranslateStaticUiText(string text, bool toEnglish)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string key = text.Trim();
		if (TryTranslateCompositeStaticUiText(key, toEnglish, out var translatedCompositeText))
		{
			return translatedCompositeText;
		}
		string normalizedStaticUiText = NormalizeStaticUiText(key);
		if (toEnglish)
		{
			if (PtToEnStaticUiText.TryGetValue(key, out var value))
			{
				return value;
			}
			if (PtToEnStaticUiTextNormalized.TryGetValue(normalizedStaticUiText, out var value2))
			{
				return value2;
			}
			return ReplaceContainedStaticUiText(text, PtToEnStaticUiText);
		}
		if (EnToPtStaticUiText.TryGetValue(key, out var value3))
		{
			return value3;
		}
		if (EnToPtStaticUiTextNormalized.TryGetValue(normalizedStaticUiText, out var value4))
		{
			return value4;
		}
		return ReplaceContainedStaticUiText(text, EnToPtStaticUiText);
	}

	private static string ReplaceContainedStaticUiText(string text, Dictionary<string, string> map)
	{
		string text2 = text;
		foreach (KeyValuePair<string, string> item in map.OrderByDescending((KeyValuePair<string, string> x) => x.Key.Length))
		{
			if (string.IsNullOrWhiteSpace(item.Key))
			{
				continue;
			}
			if (item.Key.IndexOf(' ') < 0)
			{
				continue;
			}
			if (text2.IndexOf(item.Key, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				text2 = text2.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
			}
		}
		return text2;
	}

	private static bool TryTranslateCompositeStaticUiText(string text, bool toEnglish, out string translated)
	{
		if (toEnglish)
		{
			if (text.StartsWith("Conta atual:", StringComparison.OrdinalIgnoreCase))
			{
				string text3 = text.Substring("Conta atual:".Length).Trim();
				if (string.Equals(NormalizeStaticUiText(text3), "sem nome", StringComparison.Ordinal))
				{
					text3 = "unnamed";
				}
				translated = "Current account: " + text3;
				return true;
			}
			if (text.StartsWith("Contas monitoradas:", StringComparison.OrdinalIgnoreCase))
			{
				string text4 = text.Substring("Contas monitoradas:".Length).Trim();
				translated = "Monitored accounts: " + text4;
				return true;
			}
			translated = string.Empty;
			return false;
		}
		if (text.StartsWith("Current account:", StringComparison.OrdinalIgnoreCase))
		{
			string text5 = text.Substring("Current account:".Length).Trim();
			if (string.Equals(NormalizeStaticUiText(text5), "unnamed", StringComparison.Ordinal))
			{
				text5 = "sem nome";
			}
			translated = "Conta atual: " + text5;
			return true;
		}
		if (text.StartsWith("Monitored accounts:", StringComparison.OrdinalIgnoreCase))
		{
			string text6 = text.Substring("Monitored accounts:".Length).Trim();
			translated = "Contas monitoradas: " + text6;
			return true;
		}
		translated = string.Empty;
		return false;
	}

	private static Dictionary<string, string> CreateNormalizedLookup(Dictionary<string, string> source)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> item in source)
		{
			string text = NormalizeStaticUiText(item.Key);
			if (text.Length != 0 && !dictionary.ContainsKey(text))
			{
				dictionary[text] = item.Value;
			}
		}
		return dictionary;
	}

	private static string NormalizeStaticUiText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string text2 = text.Trim().Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		bool flag = false;
		foreach (char c in text2)
		{
			UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
			if (unicodeCategory == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}
			if (char.IsWhiteSpace(c))
			{
				if (!flag)
				{
					stringBuilder.Append(' ');
					flag = true;
				}
				continue;
			}
			flag = false;
			stringBuilder.Append(char.ToLowerInvariant(c));
		}
		return stringBuilder.ToString().Trim();
	}

	private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
	{
		Stack<DependencyObject> stack = new Stack<DependencyObject>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			DependencyObject dependencyObject = stack.Pop();
			yield return dependencyObject;
			int childrenCount = VisualTreeHelper.GetChildrenCount(dependencyObject);
			for (int num = childrenCount - 1; num >= 0; num--)
			{
				stack.Push(VisualTreeHelper.GetChild(dependencyObject, num));
			}
		}
	}

	private void ApplyTheme()
	{
		if (string.Equals(_appSettings.Theme, "light", StringComparison.OrdinalIgnoreCase))
		{
			ApplyLightTheme();
		}
		else
		{
			ApplyDarkTheme();
		}
		RefreshThemeState();
	}

	private void ApplyDarkTheme()
	{
		SetBrushColor("BackgroundBrush", 2, 12, 28);
		SetBrushColor("ShellSurfaceBrush", 3, 21, 39);
		SetBrushColor("SidebarBrush", 3, 17, 32);
		SetBrushColor("CardBrush", 7, 26, 44);
		SetBrushColor("SidebarStatusBrush", 7, 28, 48);
		SetBrushColor("SettingsRowBrush", 10, 31, 52);
		SetBrushColor("ValueBoxBrush", 8, 27, 46);
		SetBrushColor("AccentCircleBrush", 14, 53, 87);
		SetBrushColor("StepBadgeBrush", 10, 39, 66);
		SetBrushColor("ToggleTrackBrush", 35, 58, 80);
		SetBrushColor("ToggleBorderBrush", 45, 77, 104);
		SetBrushColor("ToggleThumbBrush", 245, 247, 250);
		SetBrushColor("NeutralBadgeBrush", 21, 50, 75);
		SetBrushColor("PurpleBadgeBrush", 19, 44, 70);
		SetBrushColor("PurpleTextBrush", 141, 187, 228);
		SetBrushColor("WarningBadgeBrush", 70, 54, 29);
		SetBrushColor("WarningTextBrush", 246, 193, 92);
		SetBrushColor("SummaryPrimaryBrush", 15, 52, 86);
		SetBrushColor("SummarySecondaryBrush", 14, 40, 66);
		SetBrushColor("SummarySuccessBackgroundBrush", 18, 51, 42);
		SetBrushColor("SurfaceBrush", 12, 31, 51);
		SetBrushColor("SurfaceAltBrush", 7, 25, 43);
		SetBrushColor("AccentBrush", 30, 143, 239);
		SetBrushColor("AccentPressedBrush", 26, 114, 192);
		SetBrushColor("BorderBrush", 18, 54, 81);
		SetBrushColor("ShellBorderBrush", 23, 70, 102);
		SetBrushColor("CardBorderBrush", 25, 82, 118);
		SetBrushColor("TextBrush", 244, 248, 252);
		SetBrushColor("MutedBrush", 143, 174, 203);
		SetBrushColor("SuccessBrush", 38, 208, 124);
		SetBrushColor("MenuBarBrush", 3, 21, 39);
		SetBrushColor("MenuHoverBrush", 13, 42, 66);
		SetBrushColor("MenuHoverBorderBrush", 31, 98, 141);
		SetBrushColor("MenuPressedBrush", 15, 58, 93);
		SetBrushColor("MenuPopupBrush", 7, 25, 43);
		SetBrushColor("MenuPopupBorderBrush", 26, 74, 109);
		SetBrushColor("MenuSeparatorBrush", 31, 63, 90);
		SetBrushColor("DisabledBrush", 95, 115, 136);
	}

	private void ApplyLightTheme()
	{
		SetBrushColor("BackgroundBrush", 232, 241, 249);
		SetBrushColor("ShellSurfaceBrush", 241, 247, 252);
		SetBrushColor("SidebarBrush", 247, 251, byte.MaxValue);
		SetBrushColor("CardBrush", byte.MaxValue, byte.MaxValue, byte.MaxValue);
		SetBrushColor("SidebarStatusBrush", 245, 248, 252);
		SetBrushColor("SettingsRowBrush", 248, 251, byte.MaxValue);
		SetBrushColor("ValueBoxBrush", byte.MaxValue, byte.MaxValue, byte.MaxValue);
		SetBrushColor("AccentCircleBrush", 215, 231, 249);
		SetBrushColor("StepBadgeBrush", 230, 240, 251);
		SetBrushColor("ToggleTrackBrush", 199, 212, 226);
		SetBrushColor("ToggleBorderBrush", 182, 197, 213);
		SetBrushColor("ToggleThumbBrush", byte.MaxValue, byte.MaxValue, byte.MaxValue);
		SetBrushColor("NeutralBadgeBrush", 229, 237, 246);
		SetBrushColor("PurpleBadgeBrush", 238, 232, byte.MaxValue);
		SetBrushColor("PurpleTextBrush", 114, 77, 216);
		SetBrushColor("WarningBadgeBrush", byte.MaxValue, 242, 204);
		SetBrushColor("WarningTextBrush", 154, 103, 0);
		SetBrushColor("SummaryPrimaryBrush", 222, 237, 253);
		SetBrushColor("SummarySecondaryBrush", 240, 231, byte.MaxValue);
		SetBrushColor("SummarySuccessBackgroundBrush", 224, 246, 234);
		SetBrushColor("SurfaceBrush", 236, 243, 250);
		SetBrushColor("SurfaceAltBrush", byte.MaxValue, byte.MaxValue, byte.MaxValue);
		SetBrushColor("AccentBrush", 21, 156, byte.MaxValue);
		SetBrushColor("AccentPressedBrush", 14, 127, 212);
		SetBrushColor("BorderBrush", 210, 224, 238);
		SetBrushColor("ShellBorderBrush", 185, 208, 231);
		SetBrushColor("CardBorderBrush", 213, 227, 240);
		SetBrushColor("TextBrush", 8, 11, 18);
		SetBrushColor("MutedBrush", 36, 49, 64);
		SetBrushColor("SuccessBrush", 29, 186, 107);
		SetBrushColor("MenuBarBrush", 242, 247, 252);
		SetBrushColor("MenuHoverBrush", 226, 238, 249);
		SetBrushColor("MenuHoverBorderBrush", 140, 183, 226);
		SetBrushColor("MenuPressedBrush", 215, 232, 248);
		SetBrushColor("MenuPopupBrush", byte.MaxValue, byte.MaxValue, byte.MaxValue);
		SetBrushColor("MenuPopupBorderBrush", 201, 220, 236);
		SetBrushColor("MenuSeparatorBrush", 217, 228, 238);
		SetBrushColor("DisabledBrush", 107, 120, 136);
	}

	private static void SetBrushColor(string resourceKey, byte red, byte green, byte blue)
	{
		System.Windows.Media.Color color = System.Windows.Media.Color.FromRgb(red, green, blue);
		if (System.Windows.Application.Current.Resources[resourceKey] is SolidColorBrush solidColorBrush)
		{
			if (solidColorBrush.IsFrozen)
			{
				System.Windows.Application.Current.Resources[resourceKey] = new SolidColorBrush(color);
			}
			else
			{
				solidColorBrush.Color = color;
			}
		}
		else
		{
			System.Windows.Application.Current.Resources[resourceKey] = new SolidColorBrush(color);
		}
	}

	private void RefreshThemeState()
	{
		if (base.IsInitialized)
		{
			if (HomeSection.Visibility == Visibility.Visible)
			{
				ActivateSidebarButton(SidebarHomeButton);
			}
			else if (NewAccountSection.Visibility == Visibility.Visible)
			{
				ActivateSidebarButton(SidebarAccountsButton);
			}
			else if (ConfirmationsSection.Visibility == Visibility.Visible)
			{
				ActivateSidebarButton(SidebarConfirmationsButton);
			}
			else if (SettingsSection.Visibility == Visibility.Visible)
			{
				ActivateSidebarButton(SidebarSettingsButton);
			}
			if (SettingsProtectionModeBadge != null)
			{
				UpdateSettingsSection();
			}
			InvalidateVisual();
			UpdateLayout();
		}
	}

	private void ActivateSidebarButton(System.Windows.Controls.Button activeButton)
	{
		System.Windows.Media.Brush brush = (System.Windows.Media.Brush)FindResource("AccentBrush");
		System.Windows.Media.Brush brush2 = (System.Windows.Media.Brush)FindResource("SurfaceBrush");
		System.Windows.Media.Brush brush3 = (System.Windows.Media.Brush)FindResource("MutedBrush");
		System.Windows.Media.Brush brush4 = (System.Windows.Media.Brush)FindResource("TextBrush");
		System.Windows.Media.Brush brush5 = (string.Equals(_appSettings.Theme, "light", StringComparison.OrdinalIgnoreCase) ? brush4 : brush3);
		System.Windows.Controls.Button[] array = new System.Windows.Controls.Button[4] { SidebarHomeButton, SidebarAccountsButton, SidebarConfirmationsButton, SidebarSettingsButton };
		foreach (System.Windows.Controls.Button obj in array)
		{
			bool flag = obj == activeButton;
			obj.Background = (flag ? brush2 : System.Windows.Media.Brushes.Transparent);
			obj.BorderBrush = (flag ? brush : System.Windows.Media.Brushes.Transparent);
			obj.BorderThickness = (flag ? new Thickness(1.0) : new Thickness(0.0));
			obj.Foreground = (flag ? brush4 : brush5);
			obj.FontWeight = (flag ? FontWeights.SemiBold : FontWeights.Normal);
		}
	}

	private async void Protection_Click(object sender, RoutedEventArgs e)
	{
		bool flag = !string.IsNullOrWhiteSpace(ViewModel.VaultPassphrase);
		string title = (flag ? L("Trocar senha do cofre", "Change vault password") : L("Ativar senha do cofre", "Enable vault password"));
		string prompt = (flag ? L("Digite a nova senha do cofre portatil.", "Enter the new vault password.") : L("Digite a senha que vai proteger o cofre portatil.", "Enter the password that will protect the portable vault."));
		string newPassphrase = TextPromptDialog.Request(this, title, prompt, L("Use pelo menos 8 caracteres para conseguir levar o cofre com seguranca no pendrive.", "Use at least 8 characters to safely carry the vault on a USB drive."), isPassword: true, L("Continuar", "Continue"), L("Cancelar", "Cancel"));
		if (newPassphrase == null)
		{
			return;
		}
		newPassphrase = newPassphrase.Trim();
		if (newPassphrase.Length < 8)
		{
			AppMessageDialog.Show(this, L("Use uma senha com pelo menos 8 caracteres.", "Use a password with at least 8 characters."), "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		string text = TextPromptDialog.Request(this, title, L("Confirme a nova senha do cofre.", "Confirm the new vault password."), string.Empty, isPassword: true, flag ? L("Trocar senha", "Change password") : L("Ativar senha", "Enable password"), L("Cancelar", "Cancel"));
		if (text == null)
		{
			return;
		}
		if (!string.Equals(newPassphrase, text, StringComparison.Ordinal))
		{
			AppMessageDialog.Show(this, L("As senhas nao conferem.", "Passwords do not match."), "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		try
		{
			await RunWithVaultWatcherPausedAsync(() => ViewModel.UpdateProtectionAsync(newPassphrase, RequestPassphrase));
			UpdateSettingsSection();
			AppMessageDialog.Show(this, L("O cofre portatil foi atualizado com sucesso.", "The portable vault was updated successfully."), "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		catch (Exception ex)
		{
			AppMessageDialog.Show(this, ex.Message, "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private async void DisableEncryptionButton_Click(object sender, RoutedEventArgs e)
	{
		await DisableEncryptionFromSettingsAsync();
		UpdateSettingsSection();
	}

	private async Task DisableEncryptionFromSettingsAsync()
	{
		if (string.IsNullOrWhiteSpace(ViewModel.VaultPassphrase))
		{
			AppMessageDialog.Show(this, L("O cofre ja esta sem senha.", "The vault already has no password."), "Steam Authenticator Next", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		else
		{
			if (AppMessageDialog.Show(this, L("Isso vai remover a senha do cofre portatil e deixar os maFiles sem criptografia.\nDeseja continuar?", "This will remove the portable vault password and leave maFiles unencrypted.\nDo you want to continue?"), L("Remover senha do cofre", "Remove vault password"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
			{
				return;
			}
			try
			{
				await RunWithVaultWatcherPausedAsync(() => ViewModel.UpdateProtectionAsync(null, RequestPassphrase));
				UpdateSettingsSection();
				AppMessageDialog.Show(this, L("A senha do cofre foi removida com sucesso.", "Vault password removed successfully."), L("Remover senha do cofre", "Remove vault password"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			catch (Exception ex)
			{
				AppMessageDialog.Show(this, ex.Message, L("Remover senha do cofre", "Remove vault password"), MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private void MinimizeOnCloseSettingsCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (!base.IsLoaded || _updatingSettingsUi)
		{
			return;
		}
		bool flag = (sender as System.Windows.Controls.CheckBox)?.IsChecked == true;
		if (_appSettings.MinimizeOnClose != flag)
		{
			_appSettings.MinimizeOnClose = flag;
			_settingsService.Save(_appSettings);
			if (MinimizeOnCloseSettingsCheckBox.IsChecked != flag)
			{
				MinimizeOnCloseSettingsCheckBox.IsChecked = flag;
			}
			if (HomeMinimizeOnCloseCheckBox.IsChecked != flag)
			{
				HomeMinimizeOnCloseCheckBox.IsChecked = flag;
			}
			ViewModel.SetStatusMessage(flag ? L("Agora o X da janela envia o app para a bandeja.", "The window close button now sends the app to the tray.") : L("Agora o X da janela fecha o app normalmente.", "The window close button now closes the app normally."));
		}
	}

	private void AutomaticConfirmationsSettingsCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (base.IsLoaded && !_updatingSettingsUi)
		{
			bool flag = (sender as System.Windows.Controls.CheckBox)?.IsChecked == true;
			if (_appSettings.AutomaticConfirmationsEnabled != flag)
			{
				_appSettings.AutomaticConfirmationsEnabled = flag;
				_settingsService.Save(_appSettings);
				ConfigureConfirmationsAutoRefresh();
				ViewModel.SetStatusMessage(flag ? L("Verificações automáticas ativadas.", "Automatic checks enabled.") : L("Verificações automáticas desativadas.", "Automatic checks disabled."));
			}
		}
	}

	private void VerificationIntervalSettingsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && !_updatingSettingsUi)
		{
			int selectedVerificationIntervalSeconds = GetSelectedVerificationIntervalSeconds();
			if (_appSettings.VerificationIntervalSeconds != selectedVerificationIntervalSeconds)
			{
				_appSettings.VerificationIntervalSeconds = selectedVerificationIntervalSeconds;
				_settingsService.Save(_appSettings);
				ConfigureConfirmationsAutoRefresh();
				ViewModel.SetStatusMessage(L($"Intervalo entre verificações definido para {selectedVerificationIntervalSeconds} s.", $"Check interval set to {selectedVerificationIntervalSeconds} s."));
			}
		}
	}

	private void VerifyAllAccountsSettingsCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		if (base.IsLoaded && !_updatingSettingsUi)
		{
			bool flag = (sender as System.Windows.Controls.CheckBox)?.IsChecked == true;
			if (_appSettings.VerifyAllAccounts != flag)
			{
				_appSettings.VerifyAllAccounts = flag;
				_settingsService.Save(_appSettings);
				ViewModel.SetStatusMessage(flag ? L("Agora o app verifica confirmações em todas as contas.", "The app now checks confirmations for all accounts.") : L("Agora o app verifica confirmações só na conta atual.", "The app now checks confirmations only for the current account."));
			}
		}
	}

	private void ThemeSettingsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && !_updatingSettingsUi)
		{
			string selectedTheme = GetSelectedTheme();
			if (!string.Equals(_appSettings.Theme, selectedTheme, StringComparison.OrdinalIgnoreCase))
			{
				_appSettings.Theme = selectedTheme;
				_settingsService.Save(_appSettings);
				ApplyTheme();
				UpdateSettingsSection();
				ViewModel.SetStatusMessage(L("Tema atualizado com sucesso.", "Theme updated successfully."));
			}
		}
	}

	private void LanguageSettingsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (base.IsLoaded && !_updatingSettingsUi)
		{
			string selectedLanguage = GetSelectedLanguage();
			if (!string.Equals(_appSettings.Language, selectedLanguage, StringComparison.OrdinalIgnoreCase))
			{
				_appSettings.Language = selectedLanguage;
				_settingsService.Save(_appSettings);
				ApplyLanguage();
				UpdateSettingsSection();
				ViewModel.SetStatusMessage(L("Idioma atualizado com sucesso.", "Language updated successfully."));
			}
		}
	}

	private int GetSelectedVerificationIntervalSeconds()
	{
		if (VerificationIntervalSettingsComboBox.SelectedItem is ComboBoxItem { Tag: var tag } && int.TryParse(tag?.ToString(), out var result) && result > 0)
		{
			return result;
		}
		return 5;
	}

	private void RestoreDefaultSettings_Click(object sender, RoutedEventArgs e)
	{
		_appSettings.MinimizeOnClose = false;
		_appSettings.AutomaticConfirmationsEnabled = false;
		_appSettings.VerificationIntervalSeconds = 5;
		_appSettings.VerifyAllAccounts = false;
		_appSettings.Theme = "dark";
		_appSettings.Language = "pt-BR";
		_settingsService.Save(_appSettings);
		ApplyTheme();
		ApplyLanguage();
		UpdateSettingsSection();
		ViewModel.SetStatusMessage(L("Configurações restauradas para o padrão.", "Settings restored to default."));
	}

	private void SaveSettings_Click(object sender, RoutedEventArgs e)
	{
		_settingsService.Save(_appSettings);
		UpdateSettingsSection();
		ViewModel.SetStatusMessage(L("Configurações salvas no cofre portátil.", "Settings saved to the portable vault."));
	}

	private async Task RunWelcomeExperienceAsync()
	{
		WelcomeSetupDialog welcomeSetupDialog = new WelcomeSetupDialog
		{
			Owner = this
		};
		if (welcomeSetupDialog.ShowDialog() == true)
		{
			if (welcomeSetupDialog.Choice == WelcomeSetupChoice.ImportVaultFolder)
			{
				await ImportVaultFolderAsync();
			}
			else if (welcomeSetupDialog.Choice == WelcomeSetupChoice.FirstTimeSetup)
			{
				ShowNewAccountSection();
				NewAccountFlowStatusText.Text = L("Preencha login e senha para começar a primeira conta.", "Fill in username and password to start your first account.");
			}
		}
	}

	private void OpenVaultFolder_Click(object sender, RoutedEventArgs e)
	{
		string vaultFolderPath = ViewModel.VaultFolderPath;
		Directory.CreateDirectory(vaultFolderPath);
		Process.Start(new ProcessStartInfo
		{
			FileName = "explorer.exe",
			Arguments = "\"" + vaultFolderPath + "\"",
			UseShellExecute = true
		});
	}

	private void Window_Closing(object? sender, CancelEventArgs e)
	{
		_inlineNewAccountPromptTcs?.TrySetResult(null);
		if (_allowClose || System.Windows.Application.Current is App { ForceExitRequested: not false })
		{
			StopVaultWatcher();
			if (_trayIcon != null)
			{
				_trayIcon.Visible = false;
				_trayIcon.Dispose();
			}
		}
		else if (_appSettings.MinimizeOnClose)
		{
			e.Cancel = true;
			MinimizeToTray();
		}
	}

	private NotifyIcon CreateTrayIcon()
	{
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Items.Add("Abrir", null, delegate
		{
			RestoreFromTray();
		});
		contextMenuStrip.Items.Add("Sair", null, delegate
		{
			ExitFromTray();
		});
		Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty) ?? SystemIcons.Application;
		NotifyIcon notifyIcon = new NotifyIcon();
		notifyIcon.Text = "Steam Authenticator Next";
		notifyIcon.Icon = icon;
		notifyIcon.Visible = false;
		notifyIcon.ContextMenuStrip = contextMenuStrip;
		notifyIcon.DoubleClick += delegate
		{
			RestoreFromTray();
		};
		return notifyIcon;
	}

	private void MinimizeToTray()
	{
		Hide();
		base.ShowInTaskbar = false;
		base.WindowState = WindowState.Minimized;
		if (_trayIcon != null)
		{
			_trayIcon.Visible = true;
		}
		ViewModel.SetStatusMessage(L("App minimizado para a bandeja.", "App minimized to tray."));
		if (!_trayHintShown && _trayIcon != null)
		{
			_trayHintShown = true;
			_trayIcon.BalloonTipTitle = "Steam Authenticator Next";
			_trayIcon.BalloonTipText = L("O app ficou na bandeja do sistema. Dê dois cliques no ícone para abrir de novo.", "The app is in the system tray. Double-click the icon to open it again.");
			_trayIcon.ShowBalloonTip(2500);
		}
	}

	private void RestoreFromTray()
	{
		Show();
		base.ShowInTaskbar = true;
		base.WindowState = WindowState.Normal;
		Activate();
		if (_trayIcon != null)
		{
			_trayIcon.Visible = false;
		}
		ViewModel.SetStatusMessage(L("App restaurado da bandeja.", "App restored from tray."));
	}

	private void ExitFromTray()
	{
		_allowClose = true;
		if (System.Windows.Application.Current is App app)
		{
			app.ForceExitRequested = true;
		}
		Close();
	}

	private async Task<bool> EnsureSessionForConfirmationsAsync(SteamGuardAccount account)
	{
		if (account.Session == null || account.Session.SteamID == 0L || account.Session.IsRefreshTokenExpired())
		{
			LoginDialogResult loginDialogResult = LoginDialog.Request(this, "Sua sessao expirou. Entre novamente para abrir as confirmacoes.", account.AccountName ?? string.Empty);
			if (loginDialogResult == null)
			{
				return false;
			}
			SessionData sessionData = await _steamWorkflow.LoginAsync(this, loginDialogResult.Username, loginDialogResult.Password, account);
			if (sessionData == null)
			{
				return false;
			}
			account.Session = sessionData;
			await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(account, RequestPassphrase));
			return true;
		}
		if (account.Session.IsAccessTokenExpired())
		{
			try
			{
				await account.Session.RefreshAccessToken();
				await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(account, RequestPassphrase));
			}
			catch (Exception)
			{
				LoginDialogResult loginDialogResult2 = LoginDialog.Request(this, "Nao foi possivel atualizar o token automaticamente. Entre novamente para abrir as confirmacoes.", account.AccountName ?? string.Empty);
				if (loginDialogResult2 == null)
				{
					return false;
				}
				SessionData sessionData2 = await _steamWorkflow.LoginAsync(this, loginDialogResult2.Username, loginDialogResult2.Password, account);
				if (sessionData2 == null)
				{
					return false;
				}
				account.Session = sessionData2;
				await RunWithVaultWatcherPausedAsync(() => ViewModel.SaveAccountAsync(account, RequestPassphrase));
			}
		}
		return true;
	}

	private void ConfigureVaultWatcher(string vaultPath)
	{
		StopVaultWatcher();
		Directory.CreateDirectory(vaultPath);
		_vaultWatcher = new FileSystemWatcher(vaultPath)
		{
			Filter = "*.*",
			IncludeSubdirectories = false,
			NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime),
			EnableRaisingEvents = true
		};
		_vaultWatcher.Changed += OnVaultFilesChanged;
		_vaultWatcher.Created += OnVaultFilesChanged;
		_vaultWatcher.Deleted += OnVaultFilesChanged;
		_vaultWatcher.Renamed += OnVaultFilesChanged;
	}

	private void StopVaultWatcher()
	{
		_vaultReloadDebounceTimer?.Stop();
		if (_vaultWatcher != null)
		{
			_vaultWatcher.EnableRaisingEvents = false;
			_vaultWatcher.Changed -= OnVaultFilesChanged;
			_vaultWatcher.Created -= OnVaultFilesChanged;
			_vaultWatcher.Deleted -= OnVaultFilesChanged;
			_vaultWatcher.Renamed -= OnVaultFilesChanged;
			_vaultWatcher.Dispose();
			_vaultWatcher = null;
		}
	}

	private void OnVaultFilesChanged(object sender, FileSystemEventArgs e)
	{
		if (!_vaultWatcherPaused && IsVaultFileRelevant(e.FullPath))
		{
			base.Dispatcher.Invoke(delegate
			{
				ScheduleVaultReload();
			});
		}
	}

	private async void OnVaultReloadDebounceTick(object? sender, EventArgs e)
	{
		_vaultReloadDebounceTimer.Stop();
		if (_vaultReloadInProgress)
		{
			return;
		}
		_vaultReloadInProgress = true;
		try
		{
			if (await ViewModel.ReloadAccountsAsync(RequestPassphrase, triggeredByExternalChange: true) == VaultReloadOutcome.PendingCopy)
			{
				ScheduleVaultReload(1400);
			}
		}
		finally
		{
			_vaultReloadInProgress = false;
		}
	}

	private void ScheduleVaultReload(int delayMilliseconds = 800)
	{
		_vaultReloadDebounceTimer.Stop();
		_vaultReloadDebounceTimer.Interval = TimeSpan.FromMilliseconds(delayMilliseconds, 0L);
		_vaultReloadDebounceTimer.Start();
	}

	private async Task RunWithVaultWatcherPausedAsync(Func<Task> action)
	{
		_vaultWatcherPaused = true;
		_vaultReloadDebounceTimer.Stop();
		try
		{
			await action();
		}
		finally
		{
			_vaultWatcherPaused = false;
			ConfigureVaultWatcher(ViewModel.VaultFolderPath);
		}
	}

	private static string? FindExtractedVaultRoot(string extractionRoot)
	{
		string text = System.IO.Path.Combine(extractionRoot, "maFiles");
		if (File.Exists(System.IO.Path.Combine(text, "manifest.json")))
		{
			return text;
		}
		foreach (string item in Directory.EnumerateDirectories(extractionRoot, "*", SearchOption.AllDirectories))
		{
			if (string.Equals(System.IO.Path.GetFileName(item), "maFiles", StringComparison.OrdinalIgnoreCase) && File.Exists(System.IO.Path.Combine(item, "manifest.json")))
			{
				return item;
			}
		}
		return null;
	}

	private static void TryDeleteDirectory(string directoryPath)
	{
		try
		{
			if (Directory.Exists(directoryPath))
			{
				Directory.Delete(directoryPath, recursive: true);
			}
		}
		catch
		{
		}
	}

	private static bool IsVaultFileRelevant(string path)
	{
		if (System.IO.Path.GetFileName(path).Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return string.Equals(System.IO.Path.GetExtension(path), ".maFile", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryScrollListBox(System.Windows.Controls.ListBox? listBox, int delta)
	{
		if (listBox == null)
		{
			return false;
		}
		ScrollViewer scrollViewer = FindDescendant<ScrollViewer>(listBox);
		if (scrollViewer == null)
		{
			return false;
		}
		if (scrollViewer.ScrollableHeight <= 0.0)
		{
			return false;
		}
		if (delta > 0)
		{
			scrollViewer.LineUp();
		}
		else if (delta < 0)
		{
			scrollViewer.LineDown();
		}
		return true;
	}

	private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is T result)
			{
				return result;
			}
			T val = FindDescendant<T>(child);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.15.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Steam Authenticator Next;component/mainwindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			((MainWindow)target).Loaded += Window_Loaded;
			((MainWindow)target).StateChanged += Window_StateChanged;
			((MainWindow)target).Activated += Window_Activated;
			((MainWindow)target).Closing += Window_Closing;
			break;
		case 2:
			((Grid)target).MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
			break;
		case 3:
			MinimizeButton = (System.Windows.Controls.Button)target;
			MinimizeButton.Click += MinimizeButton_Click;
			break;
		case 4:
			MaximizeRestoreButton = (System.Windows.Controls.Button)target;
			MaximizeRestoreButton.Click += MaximizeRestoreButton_Click;
			break;
		case 5:
			((System.Windows.Controls.Button)target).Click += CloseButton_Click;
			break;
		case 6:
			SidebarHomeButton = (System.Windows.Controls.Button)target;
			SidebarHomeButton.Click += ShowHomeSection_Click;
			break;
		case 7:
			SidebarHomeText = (TextBlock)target;
			break;
		case 8:
			SidebarAccountsButton = (System.Windows.Controls.Button)target;
			SidebarAccountsButton.Click += ShowAccountsSection_Click;
			break;
		case 9:
			SidebarAccountsText = (TextBlock)target;
			break;
		case 10:
			SidebarConfirmationsButton = (System.Windows.Controls.Button)target;
			SidebarConfirmationsButton.Click += OpenConfirmations_Click;
			break;
		case 11:
			SidebarConfirmationsText = (TextBlock)target;
			break;
		case 12:
			SidebarSettingsButton = (System.Windows.Controls.Button)target;
			SidebarSettingsButton.Click += OpenSettings_Click;
			break;
		case 13:
			SidebarSettingsText = (TextBlock)target;
			break;
		case 14:
			HomeSection = (ScrollViewer)target;
			HomeSection.PreviewMouseWheel += SectionScrollViewer_PreviewMouseWheel;
			break;
		case 15:
			((Grid)target).PreviewMouseWheel += AccountsArea_PreviewMouseWheel;
			break;
		case 16:
			AccountsSearchTextBox = (System.Windows.Controls.TextBox)target;
			break;
		case 17:
			AccountsList = (System.Windows.Controls.ListBox)target;
			AccountsList.SelectionChanged += AccountsList_SelectionChanged;
			AccountsList.PreviewMouseWheel += AccountsArea_PreviewMouseWheel;
			break;
		case 22:
			AccountActionsPanel = (Border)target;
			break;
		case 23:
			((System.Windows.Controls.Button)target).Click += LoginAgain_Click;
			break;
		case 24:
			((System.Windows.Controls.Button)target).Click += RemoveFromManifest_Click;
			break;
		case 25:
			((System.Windows.Controls.Button)target).Click += DeactivateAuthenticator_Click;
			break;
		case 26:
			HomeConfirmationsSummaryText = (TextBlock)target;
			break;
		case 27:
			HomeProtectionSummaryText = (TextBlock)target;
			break;
		case 28:
			HomeMinimizeOnCloseCheckBox = (System.Windows.Controls.CheckBox)target;
			break;
		case 29:
			NewAccountSection = (ScrollViewer)target;
			NewAccountSection.PreviewMouseWheel += SectionScrollViewer_PreviewMouseWheel;
			break;
		case 30:
			NewAccountUsernameTextBox = (System.Windows.Controls.TextBox)target;
			break;
		case 31:
			NewAccountPasswordBox = (PasswordBox)target;
			break;
		case 32:
			StartInlineNewAccountButton = (System.Windows.Controls.Button)target;
			StartInlineNewAccountButton.Click += StartInlineNewAccount_Click;
			break;
		case 33:
			InlineNewAccountPromptPanel = (Border)target;
			break;
		case 34:
			InlinePromptTitleText = (TextBlock)target;
			break;
		case 35:
			InlinePromptMessageText = (TextBlock)target;
			break;
		case 36:
			InlinePromptTextBox = (System.Windows.Controls.TextBox)target;
			break;
		case 37:
			InlinePromptPasswordBox = (PasswordBox)target;
			break;
		case 38:
			InlinePromptCancelButton = (System.Windows.Controls.Button)target;
			InlinePromptCancelButton.Click += CancelInlinePrompt_Click;
			break;
		case 39:
			InlinePromptCancelButtonText = (TextBlock)target;
			break;
		case 40:
			InlinePromptConfirmButton = (System.Windows.Controls.Button)target;
			InlinePromptConfirmButton.Click += ConfirmInlinePrompt_Click;
			break;
		case 41:
			InlinePromptConfirmButtonText = (TextBlock)target;
			break;
		case 42:
			NewAccountFlowStatusText = (TextBlock)target;
			break;
		case 43:
			ConfirmationsSection = (ScrollViewer)target;
			ConfirmationsSection.PreviewMouseWheel += SectionScrollViewer_PreviewMouseWheel;
			break;
		case 44:
			((System.Windows.Controls.Button)target).Click += RefreshConfirmations_Click;
			break;
		case 45:
			ConfirmationsFilterAllButton = (System.Windows.Controls.Button)target;
			ConfirmationsFilterAllButton.Click += ConfirmationsFilterAll_Click;
			break;
		case 46:
			ConfirmationsFilterTradesButton = (System.Windows.Controls.Button)target;
			ConfirmationsFilterTradesButton.Click += ConfirmationsFilterTrades_Click;
			break;
		case 47:
			ConfirmationsFilterMarketButton = (System.Windows.Controls.Button)target;
			ConfirmationsFilterMarketButton.Click += ConfirmationsFilterMarket_Click;
			break;
		case 48:
			ConfirmationsFilterPendingButton = (System.Windows.Controls.Button)target;
			ConfirmationsFilterPendingButton.Click += ConfirmationsFilterPending_Click;
			break;
		case 49:
			ConfirmationsAccountSelector = (System.Windows.Controls.ComboBox)target;
			ConfirmationsAccountSelector.SelectionChanged += ConfirmationsAccountSelector_SelectionChanged;
			break;
		case 50:
			ConfirmationsAccountText = (TextBlock)target;
			break;
		case 51:
			ConfirmationsStatusText = (TextBlock)target;
			break;
		case 52:
			ConfirmationsList = (System.Windows.Controls.ListBox)target;
			break;
		case 55:
			ConfirmationsEmptyState = (StackPanel)target;
			break;
		case 56:
			ConfirmationsSummaryTotalText = (TextBlock)target;
			break;
		case 57:
			ConfirmationsSummaryTradeText = (TextBlock)target;
			break;
		case 58:
			ConfirmationsSummaryMarketText = (TextBlock)target;
			break;
		case 59:
			SettingsSection = (ScrollViewer)target;
			SettingsSection.PreviewMouseWheel += SectionScrollViewer_PreviewMouseWheel;
			break;
		case 60:
			SettingsPageTitleText = (TextBlock)target;
			break;
		case 61:
			SettingsPageSubtitleText = (TextBlock)target;
			break;
		case 62:
			SettingsSecurityIcon = (System.Windows.Shapes.Rectangle)target;
			break;
		case 63:
			SettingsSecurityTitleText = (TextBlock)target;
			break;
		case 64:
			SettingsProtectionPasswordLabelText = (TextBlock)target;
			break;
		case 65:
			SettingsProtectionModeBadge = (Border)target;
			break;
		case 66:
			SettingsProtectionModeText = (TextBlock)target;
			break;
		case 67:
			((System.Windows.Controls.Button)target).Click += Protection_Click;
			break;
		case 68:
			SettingsProtectionDescriptionText = (TextBlock)target;
			break;
		case 69:
			SettingsLockOnMinimizeLabelText = (TextBlock)target;
			break;
		case 70:
			LockOnMinimizeSettingsCheckBox = (System.Windows.Controls.CheckBox)target;
			break;
		case 71:
			SettingsAutoLockTimeLabelText = (TextBlock)target;
			break;
		case 72:
			SettingsDisableEncryptionButton = (System.Windows.Controls.Button)target;
			SettingsDisableEncryptionButton.Click += DisableEncryptionButton_Click;
			break;
		case 73:
			SettingsAppTitleText = (TextBlock)target;
			break;
		case 74:
			SettingsMinimizeOnCloseLabelText = (TextBlock)target;
			break;
		case 75:
			MinimizeOnCloseSettingsCheckBox = (System.Windows.Controls.CheckBox)target;
			MinimizeOnCloseSettingsCheckBox.Checked += MinimizeOnCloseSettingsCheckBox_Changed;
			MinimizeOnCloseSettingsCheckBox.Unchecked += MinimizeOnCloseSettingsCheckBox_Changed;
			break;
		case 76:
			SettingsStartWithWindowsLabelText = (TextBlock)target;
			break;
		case 77:
			SettingsThemeLabelText = (TextBlock)target;
			break;
		case 78:
			ThemeSettingsComboBox = (System.Windows.Controls.ComboBox)target;
			ThemeSettingsComboBox.SelectionChanged += ThemeSettingsComboBox_SelectionChanged;
			break;
		case 79:
			ThemeDarkComboBoxItem = (ComboBoxItem)target;
			break;
		case 80:
			ThemeLightComboBoxItem = (ComboBoxItem)target;
			break;
		case 81:
			SettingsLanguageLabelText = (TextBlock)target;
			break;
		case 82:
			LanguageSettingsComboBox = (System.Windows.Controls.ComboBox)target;
			LanguageSettingsComboBox.SelectionChanged += LanguageSettingsComboBox_SelectionChanged;
			break;
		case 83:
			LanguagePortugueseComboBoxItem = (ComboBoxItem)target;
			break;
		case 84:
			LanguageEnglishComboBoxItem = (ComboBoxItem)target;
			break;
		case 85:
			SettingsVerificationsTitleText = (TextBlock)target;
			break;
		case 86:
			SettingsAutomaticVerificationsLabelText = (TextBlock)target;
			break;
		case 87:
			AutomaticConfirmationsSettingsCheckBox = (System.Windows.Controls.CheckBox)target;
			AutomaticConfirmationsSettingsCheckBox.Checked += AutomaticConfirmationsSettingsCheckBox_Changed;
			AutomaticConfirmationsSettingsCheckBox.Unchecked += AutomaticConfirmationsSettingsCheckBox_Changed;
			break;
		case 88:
			SettingsVerificationIntervalLabelText = (TextBlock)target;
			break;
		case 89:
			VerificationIntervalSettingsComboBox = (System.Windows.Controls.ComboBox)target;
			VerificationIntervalSettingsComboBox.SelectionChanged += VerificationIntervalSettingsComboBox_SelectionChanged;
			break;
		case 90:
			SettingsVerifyAllAccountsLabelText = (TextBlock)target;
			break;
		case 91:
			VerifyAllAccountsSettingsCheckBox = (System.Windows.Controls.CheckBox)target;
			VerifyAllAccountsSettingsCheckBox.Checked += VerifyAllAccountsSettingsCheckBox_Changed;
			VerifyAllAccountsSettingsCheckBox.Unchecked += VerifyAllAccountsSettingsCheckBox_Changed;
			break;
		case 92:
			SettingsBackupTitleText = (TextBlock)target;
			break;
		case 93:
			SettingsVaultPathLabelText = (TextBlock)target;
			break;
		case 94:
			SettingsVaultPathText = (TextBlock)target;
			break;
		case 95:
			((System.Windows.Controls.Button)target).Click += ExportVault_Click;
			break;
		case 96:
			SettingsExportBackupText = (TextBlock)target;
			break;
		case 97:
			((System.Windows.Controls.Button)target).Click += ImportMaFile_Click;
			break;
		case 98:
			SettingsImportBackupText = (TextBlock)target;
			break;
		case 99:
			((System.Windows.Controls.Button)target).Click += OpenVaultFolder_Click;
			break;
		case 100:
			SettingsOpenMaFilesText = (TextBlock)target;
			break;
		case 101:
			((System.Windows.Controls.Button)target).Click += RestoreDefaultSettings_Click;
			break;
		case 102:
			SettingsRestoreDefaultsText = (TextBlock)target;
			break;
		case 103:
			((System.Windows.Controls.Button)target).Click += SaveSettings_Click;
			break;
		case 104:
			SettingsSaveChangesText = (TextBlock)target;
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
		case 18:
			((System.Windows.Controls.Button)target).Click += ToggleAccountActions_Click;
			break;
		case 19:
			((System.Windows.Controls.Button)target).Click += LoginAgain_Click;
			break;
		case 20:
			((System.Windows.Controls.Button)target).Click += RemoveFromManifest_Click;
			break;
		case 21:
			((System.Windows.Controls.Button)target).Click += DeactivateAuthenticator_Click;
			break;
		case 53:
			((System.Windows.Controls.Button)target).Click += CancelConfirmation_Click;
			break;
		case 54:
			((System.Windows.Controls.Button)target).Click += AcceptConfirmation_Click;
			break;
		}
	}
}


