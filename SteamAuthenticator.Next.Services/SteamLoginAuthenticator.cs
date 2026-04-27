using System;
using System.Threading.Tasks;
using SteamAuth;
using SteamKit2.Authentication;

namespace SteamAuthenticator.Next.Services;

internal sealed class SteamLoginAuthenticator : IAuthenticator
{
	private readonly SteamGuardAccount? _account;

	private readonly Func<string, bool, Task<string>> _requestEmailCodeAsync;

	private readonly Func<bool, Task<string>> _requestDeviceCodeAsync;

	private int _deviceCodesGenerated;

	public SteamLoginAuthenticator(SteamGuardAccount? account, Func<string, bool, Task<string>> requestEmailCodeAsync, Func<bool, Task<string>> requestDeviceCodeAsync)
	{
		_account = account;
		_requestEmailCodeAsync = requestEmailCodeAsync;
		_requestDeviceCodeAsync = requestDeviceCodeAsync;
	}

	public Task<bool> AcceptDeviceConfirmationAsync()
	{
		return Task.FromResult(result: false);
	}

	public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
	{
		if (previousCodeWasIncorrect)
		{
			await Task.Delay(30000);
		}
		if (_account == null)
		{
			return await _requestDeviceCodeAsync(previousCodeWasIncorrect);
		}
		string text = await _account.GenerateSteamGuardCodeAsync();
		_deviceCodesGenerated++;
		if (string.IsNullOrWhiteSpace(text))
		{
			return await _requestDeviceCodeAsync(previousCodeWasIncorrect);
		}
		if (_deviceCodesGenerated > 2 && previousCodeWasIncorrect)
		{
			return await _requestDeviceCodeAsync(previousCodeWasIncorrect);
		}
		return text;
	}

	public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
	{
		return _requestEmailCodeAsync(email, previousCodeWasIncorrect);
	}
}
