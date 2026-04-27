using System;
using System.IO;
using System.Text.Json;

namespace SteamAuthenticator.Next.Services.Configuration;

public sealed class PortableAppSettingsService
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	public string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, "SteamAuthenticatorNext.settings.json");

	public PortableAppSettings Load()
	{
		try
		{
			if (!File.Exists(SettingsFilePath))
			{
				return new PortableAppSettings();
			}
			PortableAppSettings portableAppSettings = JsonSerializer.Deserialize<PortableAppSettings>(File.ReadAllText(SettingsFilePath), JsonOptions) ?? new PortableAppSettings();
			portableAppSettings.Theme = NormalizeTheme(portableAppSettings.Theme);
			portableAppSettings.Language = NormalizeLanguage(portableAppSettings.Language);
			portableAppSettings.VerificationIntervalSeconds = ((portableAppSettings.VerificationIntervalSeconds <= 0) ? 5 : portableAppSettings.VerificationIntervalSeconds);
			return portableAppSettings;
		}
		catch
		{
			return new PortableAppSettings();
		}
	}

	public void Save(PortableAppSettings settings)
	{
		if (settings.IsDefault())
		{
			if (File.Exists(SettingsFilePath))
			{
				File.Delete(SettingsFilePath);
			}
		}
		else
		{
			Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));
			string contents = JsonSerializer.Serialize(settings, JsonOptions);
			File.WriteAllText(SettingsFilePath, contents);
		}
	}

	private static string NormalizeTheme(string? theme)
	{
		if (!string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase))
		{
			return "dark";
		}
		return "light";
	}

	private static string NormalizeLanguage(string? language)
	{
		if (!string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase))
		{
			return "pt-BR";
		}
		return "en-US";
	}
}
