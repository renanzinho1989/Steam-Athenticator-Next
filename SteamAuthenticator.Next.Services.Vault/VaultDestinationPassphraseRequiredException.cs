using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultDestinationPassphraseRequiredException : Exception
{
	public VaultDestinationPassphraseRequiredException()
		: base("Digite a senha do cofre atual para continuar.")
	{
	}
}
