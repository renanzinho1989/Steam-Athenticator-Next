using System;
using System.Collections.Generic;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class VaultLoadResult
{
	public bool IsEncrypted { get; init; }

	public string VaultRoot { get; init; } = string.Empty;

	public IReadOnlyList<VaultAccountRecord> Accounts { get; init; } = Array.Empty<VaultAccountRecord>();
}
