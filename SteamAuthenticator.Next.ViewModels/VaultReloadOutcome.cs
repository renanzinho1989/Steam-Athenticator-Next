namespace SteamAuthenticator.Next.ViewModels;

public enum VaultReloadOutcome
{
	Loaded,
	PendingCopy,
	EmptyVault,
	Cancelled,
	Failed
}
