using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultSourcePassphraseRequiredException : Exception
{
	public VaultSourcePassphraseRequiredException()
		: base("O maFile de origem esta protegido por senha.")
	{
	}
}
