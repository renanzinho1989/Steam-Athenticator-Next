using System;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultManifestIncompleteException : Exception
{
	public VaultManifestIncompleteException(string message)
		: base(message)
	{
	}

	public VaultManifestIncompleteException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
