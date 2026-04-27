using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultPassphraseInvalidException : Exception
{
	public VaultPassphraseInvalidException()
		: base("A senha do cofre esta incorreta.")
	{
	}
}
