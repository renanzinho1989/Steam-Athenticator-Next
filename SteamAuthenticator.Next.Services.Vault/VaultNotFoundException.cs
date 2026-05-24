using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultNotFoundException : Exception
{
	public VaultNotFoundException(string message)
		: base(message)
	{
	}
}
