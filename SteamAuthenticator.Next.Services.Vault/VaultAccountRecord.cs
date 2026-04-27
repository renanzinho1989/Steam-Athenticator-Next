using SteamAuth;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultAccountRecord
{
	public required SteamGuardAccount Account { get; init; }

	public required string SourceFile { get; init; }

	public required ulong SteamId { get; init; }
}
