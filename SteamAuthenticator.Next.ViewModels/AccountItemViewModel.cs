using SteamAuth;

namespace SteamAuthenticator.Next.ViewModels;

public sealed class AccountItemViewModel : ObservableObject
{
	private bool _isActionsExpanded;

	public required SteamGuardAccount Account { get; init; }

	public required string DisplayName { get; init; }

	public required string SourceFile { get; init; }

	public required ulong SteamId { get; init; }

	public bool IsActionsExpanded
	{
		get
		{
			return _isActionsExpanded;
		}
		set
		{
			SetProperty(ref _isActionsExpanded, value, "IsActionsExpanded");
		}
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
