using System;
using System.Collections.Generic;
using System.Linq;
using SteamAuth;

namespace SteamAuthenticator.Next.Dialogs;

public sealed class ConfirmationItem
{
	public Confirmation Confirmation { get; }

	public SteamGuardAccount? OwnerAccount { get; }

	public string Headline { get; }

	public string CreatorText { get; }

	public IReadOnlyList<string> SummaryLines { get; }

	public Uri? IconUri { get; }

	public ConfirmationKind Kind { get; }

	public string CategoryLabel => Kind switch
	{
		ConfirmationKind.Trade => "Trade", 
		ConfirmationKind.Market => "Mercado", 
		_ => "Pendente", 
	};

	public ConfirmationItem(Confirmation confirmation, SteamGuardAccount? ownerAccount = null)
	{
		Confirmation = confirmation;
		OwnerAccount = ownerAccount;
		Headline = (string.IsNullOrWhiteSpace(confirmation.Headline) ? "Confirmacao Steam" : confirmation.Headline);
		CreatorText = ((confirmation.Creator == 0L) ? "Criador nao informado" : $"Criador: {confirmation.Creator}");
		SummaryLines = confirmation.Summary?.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
		IconUri = (Uri.TryCreate(confirmation.Icon, UriKind.Absolute, out Uri result) ? result : null);
		Kind = DetectKind(Headline, SummaryLines);
	}

	private static ConfirmationKind DetectKind(string headline, IReadOnlyList<string> summaryLines)
	{
		string text = string.Join(" ", summaryLines);
		string text2 = (headline + " " + text).ToLowerInvariant();
		if (text2.Contains("you will give up") || text2.Contains("you will receive") || text2.Contains("trade") || text2.Contains("troca") || text2.Contains("offer"))
		{
			return ConfirmationKind.Trade;
		}
		if (text2.Contains("selling for") || text2.Contains("listed for") || text2.Contains("market") || text2.Contains("mercado") || text2.Contains("venda") || text2.Contains("buy order") || text2.Contains("compra"))
		{
			return ConfirmationKind.Market;
		}
		return ConfirmationKind.Unknown;
	}
}
