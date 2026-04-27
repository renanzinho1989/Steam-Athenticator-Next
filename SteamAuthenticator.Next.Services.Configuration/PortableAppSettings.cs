using System;

namespace SteamAuthenticator.Next.Services.Configuration;

public sealed class PortableAppSettings
{
	public bool MinimizeOnClose { get; set; }

	public bool AutomaticConfirmationsEnabled { get; set; }

	public int VerificationIntervalSeconds { get; set; } = 5;

	public bool VerifyAllAccounts { get; set; }

	public string Theme { get; set; } = "dark";

	public string Language { get; set; } = "pt-BR";

	public bool IsDefault()
	{
		if (!MinimizeOnClose && !AutomaticConfirmationsEnabled && VerificationIntervalSeconds == 5 && !VerifyAllAccounts && string.Equals(Theme, "dark", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(Language, "pt-BR", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
}
