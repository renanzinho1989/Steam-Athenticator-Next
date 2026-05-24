using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultSourcePassphraseInvalidException : Exception
{
	public VaultSourcePassphraseInvalidException()
		: base("A senha informada para o maFile de origem esta incorreta.")
	{
	}
}
