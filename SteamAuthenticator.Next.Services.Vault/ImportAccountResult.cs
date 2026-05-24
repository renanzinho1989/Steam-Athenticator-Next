namespace SteamAuthenticator.Next.Services.Vault;

public sealed class ImportAccountResult
{
	public bool IsEncrypted { get; init; }

	public string VaultRoot { get; init; } = string.Empty;

	public ulong ImportedSteamId { get; init; }

	public string? ActivePassphrase { get; init; }
}
