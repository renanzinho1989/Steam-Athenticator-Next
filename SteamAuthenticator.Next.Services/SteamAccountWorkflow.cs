using System;
using System.Threading.Tasks;
using System.Windows;
using SteamAuth;
using SteamAuthenticator.Next.Dialogs;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace SteamAuthenticator.Next.Services;

public sealed class SteamAccountWorkflow
{
	public enum LoginFailureKind
	{
		None,
		InvalidCredentials,
		InvalidTwoFactorCode,
		InvalidEmailCode,
		CaptchaRequired,
		RateLimited,
		NetworkError,
		Cancelled,
		Unknown
	}

	public LoginFailureKind LastLoginFailure { get; private set; }

	public string? LastLoginFailureSummary { get; private set; }

	public string? LastLoginFailureMessage { get; private set; }

	private static async Task<string?> RequestInlineOrDialogAsync(Window owner, string title, string prompt, string hint = "", bool isPassword = false, string confirmText = "Confirmar", string cancelText = "Cancelar")
	{
		if (owner is MainWindow mainWindow)
		{
			return await mainWindow.RequestInlineNewAccountPromptAsync(title, prompt, hint, isPassword, confirmText, cancelText);
		}
		return TextPromptDialog.Request(owner, title, prompt, hint, isPassword, confirmText, cancelText);
	}

	private static async Task ShowInlineMessageOrDialogAsync(Window owner, string title, string message, MessageBoxImage image = MessageBoxImage.Asterisk, string? copyButtonText = null, string? textToCopy = null)
	{
		if (owner is MainWindow mainWindow)
		{
			await mainWindow.ShowInlineNewAccountMessageAsync(title, message, "OK", copyButtonText, textToCopy);
		}
		else
		{
			if (!string.IsNullOrWhiteSpace(textToCopy))
			{
				Clipboard.SetText(textToCopy);
			}
			AppMessageDialog.Show(owner, message, title, MessageBoxButton.OK, image);
		}
	}

	private void ClearLastLoginFailure()
	{
		LastLoginFailure = LoginFailureKind.None;
		LastLoginFailureSummary = null;
		LastLoginFailureMessage = null;
	}

	private void SetLastLoginFailure(LoginFailureKind kind, Exception exception)
	{
		LastLoginFailure = kind;
		LastLoginFailureSummary = BuildLoginFailureSummary(kind);
		LastLoginFailureMessage = BuildLoginFailureMessage(kind, exception);
	}

	private static LoginFailureKind ClassifyLoginException(Exception exception)
	{
		if (exception is OperationCanceledException)
		{
			return LoginFailureKind.Cancelled;
		}
		string text = exception.Message?.ToLowerInvariant() ?? string.Empty;
		if (text.Contains("captcha"))
		{
			return LoginFailureKind.CaptchaRequired;
		}
		if (text.Contains("too many") || text.Contains("rate limit") || text.Contains("try again later") || text.Contains("many attempts") || text.Contains("too frequent"))
		{
			return LoginFailureKind.RateLimited;
		}
		if (text.Contains("email"))
		{
			return LoginFailureKind.InvalidEmailCode;
		}
		if (text.Contains("steam guard") || text.Contains("two-factor") || text.Contains("two factor") || text.Contains("2fa") || text.Contains("authenticator") || text.Contains("device code"))
		{
			return LoginFailureKind.InvalidTwoFactorCode;
		}
		if (text.Contains("password") || text.Contains("credential") || text.Contains("username") || text.Contains("account name") || text.Contains("invalidpassword") || text.Contains("invalid password"))
		{
			return LoginFailureKind.InvalidCredentials;
		}
		if (text.Contains("network") || text.Contains("timeout") || text.Contains("timed out") || text.Contains("socket") || text.Contains("connection") || text.Contains("dns") || text.Contains("ssl") || text.Contains("host"))
		{
			return LoginFailureKind.NetworkError;
		}
		return LoginFailureKind.Unknown;
	}

	private static string BuildLoginFailureSummary(LoginFailureKind kind)
	{
		return kind switch
		{
			LoginFailureKind.InvalidCredentials => "Falha no login: usuario ou senha invalidos.", 
			LoginFailureKind.InvalidTwoFactorCode => "Falha no login: codigo do Steam Guard invalido.", 
			LoginFailureKind.InvalidEmailCode => "Falha no login: codigo de e-mail invalido.", 
			LoginFailureKind.CaptchaRequired => "Falha no login: Steam solicitou CAPTCHA.", 
			LoginFailureKind.RateLimited => "Falha no login: muitas tentativas. Aguarde e tente novamente.", 
			LoginFailureKind.NetworkError => "Falha no login: erro de conexao com a Steam.", 
			LoginFailureKind.Cancelled => "Login cancelado antes da conclusao.", 
			_ => "Nao foi possivel concluir o login na Steam.", 
		};
	}

	private static string BuildLoginFailureMessage(LoginFailureKind kind, Exception exception)
	{
		string text = BuildLoginFailureSummary(kind);
		string text2 = exception.Message?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text2) || string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		return text + "\n\nDetalhe tecnico: " + text2;
	}

	public async Task<SessionData?> LoginAsync(Window owner, string username, string password, SteamGuardAccount? existingAccount)
	{
		ClearLastLoginFailure();
		SteamClient steamClient = new SteamClient();
		steamClient.Connect();
		while (!steamClient.IsConnected)
		{
			await Task.Delay(500);
		}
		CredentialsAuthSession authSession;
		try
		{
			authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
			{
				Username = username,
				Password = password,
				IsPersistentSession = false,
				PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
				ClientOSType = EOSType.Android9,
				Authenticator = new SteamLoginAuthenticator(existingAccount, (string email, bool invalid) => RequestEmailCodeAsync(owner, email, invalid), (bool invalid) => RequestDeviceCodeAsync(owner, invalid))
			});
		}
		catch (Exception exception)
		{
			LoginFailureKind kind = ClassifyLoginException(exception);
			SetLastLoginFailure(kind, exception);
			await ShowInlineMessageOrDialogAsync(owner, "Login Steam", LastLoginFailureMessage ?? BuildLoginFailureSummary(kind), MessageBoxImage.Hand);
			return null;
		}
		AuthPollResult pollResponse;
		try
		{
			pollResponse = await authSession.PollingWaitForResultAsync();
		}
		catch (Exception exception2)
		{
			LoginFailureKind kind2 = ClassifyLoginException(exception2);
			SetLastLoginFailure(kind2, exception2);
			await ShowInlineMessageOrDialogAsync(owner, "Login Steam", LastLoginFailureMessage ?? BuildLoginFailureSummary(kind2), MessageBoxImage.Hand);
			return null;
		}
		return new SessionData
		{
			SteamID = authSession.SteamID.ConvertToUInt64(),
			AccessToken = pollResponse.AccessToken,
			RefreshToken = pollResponse.RefreshToken
		};
	}

	public async Task<SteamGuardAccount?> LinkNewAuthenticatorAsync(Window owner, SessionData sessionData)
	{
		AuthenticatorLinker linker = new AuthenticatorLinker(sessionData);
		while (true)
		{
			AuthenticatorLinker.LinkResult linkResponse;
			try
			{
				linkResponse = await linker.AddAuthenticator();
			}
			catch (Exception ex)
			{
				await ShowInlineMessageOrDialogAsync(owner, "Login Steam", "Erro ao adicionar seu autenticador: " + ex.Message, MessageBoxImage.Hand);
				return null;
			}
			switch (linkResponse)
			{
			case AuthenticatorLinker.LinkResult.AwaitingFinalization:
				return await FinalizeAuthenticatorAsync(owner, linker);
			case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
				if (!(await EnsurePhoneAddedAsync(owner, linker)))
				{
					return null;
				}
				break;
			case AuthenticatorLinker.LinkResult.AuthenticatorPresent:
				await ShowInlineMessageOrDialogAsync(owner, "Login Steam", "Esta conta ja possui um autenticador vinculado. Remova o autenticador atual antes de adicionar o app novo.", MessageBoxImage.Exclamation);
				return null;
			case AuthenticatorLinker.LinkResult.MustConfirmEmail:
				await ShowInlineMessageOrDialogAsync(owner, "Login Steam", "Confira seu e-mail e confirme o numero de telefone antes de continuar.");
				break;
			case AuthenticatorLinker.LinkResult.GeneralFailure:
			case AuthenticatorLinker.LinkResult.FailureAddingPhone:
				await ShowInlineMessageOrDialogAsync(owner, "Login Steam", "Nao foi possivel iniciar o vinculamento do autenticador.", MessageBoxImage.Hand);
				return null;
			default:
				await ShowInlineMessageOrDialogAsync(owner, "Login Steam", "Fluxo de vinculacao nao suportado nesta resposta da Steam.", MessageBoxImage.Hand);
				return null;
			}
		}
	}

	private static async Task<bool> EnsurePhoneAddedAsync(Window owner, AuthenticatorLinker linker)
	{
		string phoneNumber = await RequestInlineOrDialogAsync(owner, "Numero de telefone", "Digite o numero com codigo do pais.", "Exemplo: +5511999999999");
		if (string.IsNullOrWhiteSpace(phoneNumber))
		{
			return false;
		}
		string text = await RequestInlineOrDialogAsync(owner, "Codigo do pais", "Digite o codigo de pais em duas letras.", "Exemplo: BR");
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		linker.PhoneNumber = phoneNumber;
		linker.PhoneCountryCode = text.ToUpperInvariant();
		while (true)
		{
			AuthenticatorLinker.PhoneLinkResult phoneResult;
			try
			{
				phoneResult = await linker.AddPhoneNumber();
			}
			catch (Exception ex)
			{
				await ShowInlineMessageOrDialogAsync(owner, "Numero de telefone", ex.Message, MessageBoxImage.Hand);
				return false;
			}
			switch (phoneResult)
			{
			case AuthenticatorLinker.PhoneLinkResult.PhoneAdded:
				return true;
			case AuthenticatorLinker.PhoneLinkResult.MustConfirmEmail:
				await ShowInlineMessageOrDialogAsync(owner, "Numero de telefone", "A Steam enviou um link para o seu e-mail. Confirme o e-mail e clique em OK para continuar.");
				break;
			case AuthenticatorLinker.PhoneLinkResult.MustConfirmSMS:
			{
				string text2 = await RequestInlineOrDialogAsync(owner, "Codigo SMS", "Digite o codigo SMS enviado para o seu telefone.");
				if (string.IsNullOrWhiteSpace(text2))
				{
					return false;
				}
				linker.PhoneSMSCode = text2;
				break;
			}
			default:
				await ShowInlineMessageOrDialogAsync(owner, "Numero de telefone", "Nao foi possivel confirmar o numero de telefone.", MessageBoxImage.Hand);
				return false;
			}
		}
	}

	private static async Task<SteamGuardAccount?> FinalizeAuthenticatorAsync(Window owner, AuthenticatorLinker linker)
	{
		if (linker.LinkedAccount == null)
		{
			return null;
		}
		await ShowInlineMessageOrDialogAsync(owner, "Codigo de revogacao", "Guarde este codigo de revogacao em um lugar seguro:\n\n" + linker.LinkedAccount.RevocationCode, MessageBoxImage.Asterisk, "Copiar codigo", linker.LinkedAccount.RevocationCode);
		while (true)
		{
			string text = await RequestRequiredCodeAsync(owner, "Codigo SMS", "Digite o codigo SMS enviado para finalizar o autenticador.", "Nenhum codigo informado. Deseja cancelar a vinculacao do autenticador?");
			switch (await linker.FinalizeAddAuthenticator(text))
			{
			case AuthenticatorLinker.FinalizeResult.Success:
				await ShowInlineMessageOrDialogAsync(owner, "Steam Authenticator Next", "Autenticador vinculado com sucesso.\n\nCodigo de revogacao: " + linker.LinkedAccount.RevocationCode);
				return linker.LinkedAccount;
			case AuthenticatorLinker.FinalizeResult.BadSMSCode:
				await ShowInlineMessageOrDialogAsync(owner, "Steam Authenticator Next", "Codigo SMS invalido. Tente novamente.", MessageBoxImage.Exclamation);
				break;
			case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
				await ShowInlineMessageOrDialogAsync(owner, "Steam Authenticator Next", "Nao foi possivel gerar os codigos corretos para finalizar o autenticador.", MessageBoxImage.Hand);
				return null;
			default:
				await ShowInlineMessageOrDialogAsync(owner, "Steam Authenticator Next", "Nao foi possivel finalizar o autenticador.", MessageBoxImage.Hand);
				return null;
			}
		}
	}

	private static async Task<string> RequestEmailCodeAsync(Window owner, string email, bool previousCodeWasIncorrect)
	{
		string prompt = (previousCodeWasIncorrect ? "O codigo do e-mail ficou invalido. Digite novamente o codigo enviado pela Steam." : ("Digite o codigo enviado para " + email + "."));
		return await RequestRequiredCodeAsync(owner, "Codigo de e-mail", prompt, "Nenhum codigo de e-mail informado. Deseja cancelar o login?");
	}

	private static async Task<string> RequestDeviceCodeAsync(Window owner, bool previousCodeWasIncorrect)
	{
		string prompt = (previousCodeWasIncorrect ? "O codigo de autenticador ficou invalido. Digite o novo codigo de dois fatores." : "Digite o codigo atual do seu autenticador Steam.");
		return await RequestRequiredCodeAsync(owner, "Codigo do autenticador", prompt, "Nenhum codigo do autenticador informado. Deseja cancelar o login?");
	}

	private static async Task<string> RequestRequiredCodeAsync(Window owner, string title, string prompt, string cancelConfirmationMessage)
	{
		while (true)
		{
			string text = await RequestInlineOrDialogAsync(owner, title, prompt);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
			if (AppMessageDialog.Show(owner, cancelConfirmationMessage, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				throw new OperationCanceledException(cancelConfirmationMessage);
			}
		}
	}
}
