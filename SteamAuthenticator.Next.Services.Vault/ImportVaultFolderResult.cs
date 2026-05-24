namespace SteamAuthenticator.Next.Services.Vault;

public sealed class ImportVaultFolderResult
{
	public bool IsEncrypted { get; init; }

	public string VaultRoot { get; init; } = string.Empty;

	public int ImportedCount { get; init; }

	public string? ActivePassphrase { get; init; }
}
