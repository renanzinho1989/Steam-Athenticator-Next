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

	public string CategoryLabel { get; }

	private bool UsePortuguese { get; }

	public string CreatorLabel => UsePortuguese ? "Criador" : "Creator";

	public string CategoryLabelText => CategoryLabel;

	public string CategoryLabelLegacy => CategoryLabel;

	private string BuildCategoryLabel() => Kind switch
	{
		ConfirmationKind.Trade => UsePortuguese ? "Troca" : "Trade", 
		ConfirmationKind.Market => UsePortuguese ? "Mercado" : "Market", 
		_ => UsePortuguese ? "Pendente" : "Pending", 
	};

	public ConfirmationItem(Confirmation confirmation, SteamGuardAccount? ownerAccount = null, IEnumerable<string>? detailLines = null, bool usePortuguese = true)
	{
		Confirmation = confirmation;
		OwnerAccount = ownerAccount;
		UsePortuguese = usePortuguese;
		Headline = TranslateHeadline(string.IsNullOrWhiteSpace(confirmation.Headline) ? "Confirmacao Steam" : confirmation.Headline, UsePortuguese);
		CreatorText = ((confirmation.Creator == 0L) ? (UsePortuguese ? "Criador nao informado" : "Creator not available") : $"{CreatorLabel}: {confirmation.Creator}");
		SummaryLines = BuildSummaryLines(confirmation, detailLines, UsePortuguese);
		IconUri = (Uri.TryCreate(confirmation.Icon, UriKind.Absolute, out Uri result) ? result : null);
		Kind = confirmation.ConfType switch
		{
			Confirmation.EMobileConfirmationType.Trade => ConfirmationKind.Trade,
			Confirmation.EMobileConfirmationType.MarketListing => ConfirmationKind.Market,
			_ => DetectKind(Headline, SummaryLines)
		};
		CategoryLabel = BuildCategoryLabel();
	}

	private static IReadOnlyList<string> BuildSummaryLines(Confirmation confirmation, IEnumerable<string>? detailLines, bool usePortuguese)
	{
		List<string> list = new List<string>();
		foreach (string summaryLine in confirmation.Summary?.Where((string x) => !string.IsNullOrWhiteSpace(x)) ?? Enumerable.Empty<string>())
		{
			string normalizedSummaryLine = NormalizePortugueseItemName(TranslateSummaryLine(summaryLine, usePortuguese), usePortuguese);
			if (!ContainsEquivalentLine(list, normalizedSummaryLine, usePortuguese))
			{
				ReplaceShorterEquivalentLine(list, normalizedSummaryLine, usePortuguese);
			}
		}
		if (detailLines == null)
		{
			return list;
		}
		foreach (string detailLine in detailLines)
		{
			string normalizedDetailLine = NormalizePortugueseItemName(detailLine, usePortuguese);
			if (!string.IsNullOrWhiteSpace(normalizedDetailLine) && !ContainsEquivalentLine(list, normalizedDetailLine, usePortuguese))
			{
				ReplaceShorterEquivalentLine(list, normalizedDetailLine, usePortuguese);
			}
		}
		return list;
	}

	private static bool ContainsEquivalentLine(List<string> lines, string line, bool usePortuguese)
	{
		string normalizedLine = NormalizePortugueseItemName(line, usePortuguese);
		return lines.Any((string x) => string.Equals(NormalizePortugueseItemName(x, usePortuguese), normalizedLine, StringComparison.OrdinalIgnoreCase) || NormalizePortugueseItemName(x, usePortuguese).StartsWith(normalizedLine + " (", StringComparison.OrdinalIgnoreCase));
	}

	private static void ReplaceShorterEquivalentLine(List<string> lines, string line, bool usePortuguese)
	{
		string normalizedLine = NormalizePortugueseItemName(line, usePortuguese);
		int shorterIndex = lines.FindIndex((string x) => normalizedLine.StartsWith(NormalizePortugueseItemName(x, usePortuguese) + " (", StringComparison.OrdinalIgnoreCase));
		if (shorterIndex >= 0)
		{
			lines[shorterIndex] = normalizedLine;
			return;
		}
		lines.Add(normalizedLine);
	}

	private static string NormalizePortugueseItemName(string line, bool usePortuguese)
	{
		if (!usePortuguese || string.IsNullOrWhiteSpace(line))
		{
			return line;
		}
		Dictionary<string, string> replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Field-Tested", "Testada em Campo" },
			{ "Minimal Wear", "Pouco Desgastada" },
			{ "Factory New", "Nova de Fabrica" },
			{ "Well-Worn", "Bem Desgastada" },
			{ "Battle-Scarred", "Veterana de Guerra" },
			{ "Kilowatt Case", "Caixa Kilowatt" },
			{ "Facility Sketch", "Esboco de Instalacao" },
			{ "Raw Ceramic", "Ceramica Bruta" },
			{ "Sleet", "Granizo" },
			{ "Sheet Lightning", "Raio em Folha" }
		};
		string translated = line.Trim();
		foreach (KeyValuePair<string, string> replacement in replacements)
		{
			translated = translated.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
		}
		if (translated.IndexOf("Kilowatt", StringComparison.OrdinalIgnoreCase) >= 0 && translated.IndexOf("Case", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			translated = translated.Replace("Kilowatt Case", "Caixa Kilowatt", StringComparison.OrdinalIgnoreCase);
		}
		return translated;
	}

	private static string TranslateSummaryLine(string line, bool usePortuguese)
	{
		if (!usePortuguese || string.IsNullOrWhiteSpace(line))
		{
			return line;
		}
		string trimmed = line.Trim();
		if (string.Equals(trimmed, "You will receive nothing", StringComparison.OrdinalIgnoreCase))
		{
			return "Voce nao recebera nada";
		}
		if (string.Equals(trimmed, "You will give up nothing", StringComparison.OrdinalIgnoreCase))
		{
			return "Voce nao entregara nada";
		}
		if (trimmed.StartsWith("You will give up ", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(" items", StringComparison.OrdinalIgnoreCase))
		{
			string count = trimmed.Substring("You will give up ".Length, trimmed.Length - "You will give up ".Length - " items".Length);
			return "Voce vai entregar " + count + " itens";
		}
		if (trimmed.StartsWith("You will receive ", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(" items", StringComparison.OrdinalIgnoreCase))
		{
			string count = trimmed.Substring("You will receive ".Length, trimmed.Length - "You will receive ".Length - " items".Length);
			return "Voce vai receber " + count + " itens";
		}
		if (trimmed.StartsWith("Selling for ", StringComparison.OrdinalIgnoreCase))
		{
			return "Vendendo por " + trimmed.Substring("Selling for ".Length);
		}
		return line;
	}

	private static string TranslateHeadline(string headline, bool usePortuguese)
	{
		if (!usePortuguese || string.IsNullOrWhiteSpace(headline))
		{
			return headline;
		}
		string trimmed = headline.Trim();
		if (trimmed.StartsWith("Selling for ", StringComparison.OrdinalIgnoreCase))
		{
			return "Vendendo por " + trimmed.Substring("Selling for ".Length);
		}
		return headline;
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
