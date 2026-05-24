namespace SteamAuthenticator.Next.Services.Vault;

public sealed class SaveAccountResult
{
	public bool IsEncrypted { get; init; }

	public string VaultRoot { get; init; } = string.Empty;

	public ulong SavedSteamId { get; init; }

	public string? ActivePassphrase { get; init; }
}
