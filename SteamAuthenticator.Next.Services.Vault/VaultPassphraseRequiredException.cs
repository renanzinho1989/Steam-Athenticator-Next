using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultPassphraseRequiredException : Exception
{
	public VaultPassphraseRequiredException()
		: base("Este cofre esta protegido por senha. Digite a senha para continuar.")
	{
	}
}
