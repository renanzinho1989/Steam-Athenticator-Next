using Newtonsoft.Json;

namespace SteamAuthenticator.Next.Services.Vault;

internal sealed class VaultManifestEntry
{
	[JsonProperty("encryption_iv")]
	public string? IV { get; set; }

	[JsonProperty("encryption_salt")]
	public string? Salt { get; set; }

	[JsonProperty("filename")]
	public string Filename { get; set; } = string.Empty;

	[JsonProperty("steamid")]
	public ulong SteamID { get; set; }
}
