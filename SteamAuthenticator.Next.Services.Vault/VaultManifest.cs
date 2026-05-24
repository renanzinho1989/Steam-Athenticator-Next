using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamAuthenticator.Next.Services.Vault;

internal sealed class VaultManifest
{
	[JsonProperty("encrypted")]
	public bool Encrypted { get; set; }

	[JsonProperty("entries")]
	public List<VaultManifestEntry> Entries { get; set; } = new List<VaultManifestEntry>();
}
