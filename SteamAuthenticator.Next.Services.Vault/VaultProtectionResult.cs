namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultProtectionResult
{
	public bool IsEncrypted { get; init; }

	public string VaultRoot { get; init; } = string.Empty;

	public string? ActivePassphrase { get; init; }
}
