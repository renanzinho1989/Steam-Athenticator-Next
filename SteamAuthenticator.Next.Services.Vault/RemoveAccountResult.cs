namespace SteamAuthenticator.Next.Services.Vault;

public sealed class RemoveAccountResult
{
	public bool IsEncrypted { get; init; }

	public bool AccountRemoved { get; init; }

	public string VaultRoot { get; init; } = string.Empty;

	public ulong RemovedSteamId { get; init; }
}
