using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using SteamAuth;

namespace SteamAuthenticator.Next.Services.Vault;

public sealed class SteamVaultService
{
	private sealed class SourceAccountResult
	{
		public required SteamGuardAccount Account { get; init; }

		public required ulong SteamId { get; init; }
	}

	public string EnsurePortableVaultRootExists()
	{
		string defaultVaultRoot = GetDefaultVaultRoot();
		Directory.CreateDirectory(defaultVaultRoot);
		return defaultVaultRoot;
	}

	public string GetPortableVaultRoot()
	{
		return GetDefaultVaultRoot();
	}

	public VaultLoadResult LoadAccounts(string? passphrase, string? preferredVaultRoot = null)
	{
		string text = EnsurePortableVaultRootExists();
		string text2 = ResolveVaultRoot(preferredVaultRoot) ?? throw new VaultNotFoundException("Nenhum cofre encontrado. Use o botao Importar maFile para criar um cofre local em " + text + ".");
		VaultManifest vaultManifest = LoadManifest(Path.Combine(text2, "manifest.json"));
		IReadOnlyList<VaultAccountRecord> accounts = LoadAccountsFromManifest(text2, vaultManifest, passphrase);
		return new VaultLoadResult
		{
			VaultRoot = text2,
			IsEncrypted = vaultManifest.Encrypted,
			Accounts = accounts
		};
	}

	public ImportAccountResult ImportAccount(string sourceFilePath, string? sourcePassphrase, string? destinationPassphrase, string? preferredDestinationRoot = null)
	{
		if (!File.Exists(sourceFilePath))
		{
			throw new FileNotFoundException("O maFile selecionado nao foi encontrado.", sourceFilePath);
		}
		SourceAccountResult source = ReadSourceAccount(sourceFilePath, sourcePassphrase);
		string text = (string.IsNullOrWhiteSpace(preferredDestinationRoot) ? GetDefaultVaultRoot() : preferredDestinationRoot);
		string text2 = Path.Combine(text, "manifest.json");
		bool num = File.Exists(text2);
		VaultManifest vaultManifest = (num ? LoadManifest(text2) : new VaultManifest());
		if (num && vaultManifest.Encrypted && string.IsNullOrWhiteSpace(destinationPassphrase))
		{
			throw new VaultDestinationPassphraseRequiredException();
		}
		if (!num)
		{
			vaultManifest.Encrypted = !string.IsNullOrWhiteSpace(destinationPassphrase);
		}
		string text3 = JsonConvert.SerializeObject(source.Account);
		string salt = null;
		string text4 = null;
		if (vaultManifest.Encrypted)
		{
			salt = VaultCrypto.GetRandomSalt();
			text4 = VaultCrypto.GetInitializationVector();
			text3 = VaultCrypto.Encrypt(destinationPassphrase, salt, text4, text3);
		}
		Directory.CreateDirectory(text);
		string text5 = $"{source.SteamId}.maFile";
		File.WriteAllText(Path.Combine(text, text5), text3);
		VaultManifestEntry vaultManifestEntry = new VaultManifestEntry
		{
			Filename = text5,
			SteamID = source.SteamId,
			Salt = salt,
			IV = text4
		};
		int num2 = vaultManifest.Entries.FindIndex((VaultManifestEntry x) => x.SteamID == source.SteamId);
		if (num2 >= 0)
		{
			vaultManifest.Entries[num2] = vaultManifestEntry;
		}
		else
		{
			vaultManifest.Entries.Add(vaultManifestEntry);
		}
		SaveManifest(text2, vaultManifest);
		return new ImportAccountResult
		{
			VaultRoot = text,
			IsEncrypted = vaultManifest.Encrypted,
			ImportedSteamId = source.SteamId,
			ActivePassphrase = (vaultManifest.Encrypted ? destinationPassphrase : null)
		};
	}

	public ImportVaultFolderResult ImportVaultFolder(string sourceFolderPath, string? sourcePassphrase, string? destinationPassphrase, string? preferredDestinationRoot = null)
	{
		if (!Directory.Exists(sourceFolderPath))
		{
			throw new DirectoryNotFoundException("A pasta maFiles selecionada nao foi encontrada.");
		}
		string text = Path.Combine(sourceFolderPath, "manifest.json");
		if (!File.Exists(text))
		{
			throw new InvalidOperationException("A pasta selecionada nao contem um manifest.json valido de maFiles.");
		}
		VaultManifest vaultManifest = LoadManifest(text);
		IReadOnlyList<VaultAccountRecord> readOnlyList = LoadAccountsFromManifest(sourceFolderPath, vaultManifest, sourcePassphrase);
		string text2 = (string.IsNullOrWhiteSpace(preferredDestinationRoot) ? GetDefaultVaultRoot() : preferredDestinationRoot);
		Directory.CreateDirectory(text2);
		string text3 = Path.Combine(text2, "manifest.json");
		bool num = File.Exists(text3);
		VaultManifest vaultManifest2 = (num ? LoadManifest(text3) : new VaultManifest());
		if (num && vaultManifest2.Encrypted && string.IsNullOrWhiteSpace(destinationPassphrase))
		{
			throw new VaultDestinationPassphraseRequiredException();
		}
		string text4 = destinationPassphrase;
		if (!num && vaultManifest.Encrypted && string.IsNullOrWhiteSpace(text4))
		{
			text4 = sourcePassphrase;
		}
		if (!num)
		{
			vaultManifest2.Encrypted = !string.IsNullOrWhiteSpace(text4);
		}
		foreach (VaultAccountRecord record in readOnlyList)
		{
			string text5 = $"{record.SteamId}.maFile";
			string path = Path.Combine(text2, text5);
			string text6 = JsonConvert.SerializeObject(record.Account);
			string salt = null;
			string text7 = null;
			if (vaultManifest2.Encrypted)
			{
				salt = VaultCrypto.GetRandomSalt();
				text7 = VaultCrypto.GetInitializationVector();
				text6 = VaultCrypto.Encrypt(text4, salt, text7, text6);
			}
			File.WriteAllText(path, text6);
			VaultManifestEntry vaultManifestEntry = new VaultManifestEntry
			{
				Filename = text5,
				SteamID = record.SteamId,
				Salt = salt,
				IV = text7
			};
			int num2 = vaultManifest2.Entries.FindIndex((VaultManifestEntry x) => x.SteamID == record.SteamId);
			if (num2 >= 0)
			{
				vaultManifest2.Entries[num2] = vaultManifestEntry;
			}
			else
			{
				vaultManifest2.Entries.Add(vaultManifestEntry);
			}
		}
		SaveManifest(text3, vaultManifest2);
		return new ImportVaultFolderResult
		{
			VaultRoot = text2,
			IsEncrypted = vaultManifest2.Encrypted,
			ImportedCount = readOnlyList.Count,
			ActivePassphrase = (vaultManifest2.Encrypted ? text4 : null)
		};
	}

	public SaveAccountResult SaveAccount(SteamGuardAccount account, string? destinationPassphrase, string? preferredDestinationRoot = null)
	{
		ulong steamId = ResolveSteamId(account, 0uL);
		string text = (string.IsNullOrWhiteSpace(preferredDestinationRoot) ? GetDefaultVaultRoot() : preferredDestinationRoot);
		string text2 = Path.Combine(text, "manifest.json");
		bool num = File.Exists(text2);
		VaultManifest vaultManifest = (num ? LoadManifest(text2) : new VaultManifest());
		if (num && vaultManifest.Encrypted && string.IsNullOrWhiteSpace(destinationPassphrase))
		{
			throw new VaultDestinationPassphraseRequiredException();
		}
		if (!num)
		{
			vaultManifest.Encrypted = !string.IsNullOrWhiteSpace(destinationPassphrase);
		}
		string text3 = JsonConvert.SerializeObject(account);
		string salt = null;
		string text4 = null;
		if (vaultManifest.Encrypted)
		{
			salt = VaultCrypto.GetRandomSalt();
			text4 = VaultCrypto.GetInitializationVector();
			text3 = VaultCrypto.Encrypt(destinationPassphrase, salt, text4, text3);
		}
		Directory.CreateDirectory(text);
		string text5 = $"{steamId}.maFile";
		File.WriteAllText(Path.Combine(text, text5), text3);
		VaultManifestEntry vaultManifestEntry = new VaultManifestEntry
		{
			Filename = text5,
			SteamID = steamId,
			Salt = salt,
			IV = text4
		};
		int num2 = vaultManifest.Entries.FindIndex((VaultManifestEntry x) => x.SteamID == steamId);
		if (num2 >= 0)
		{
			vaultManifest.Entries[num2] = vaultManifestEntry;
		}
		else
		{
			vaultManifest.Entries.Add(vaultManifestEntry);
		}
		SaveManifest(text2, vaultManifest);
		return new SaveAccountResult
		{
			VaultRoot = text,
			IsEncrypted = vaultManifest.Encrypted,
			SavedSteamId = steamId,
			ActivePassphrase = (vaultManifest.Encrypted ? destinationPassphrase : null)
		};
	}

	public string ExportVaultArchive(string destinationArchivePath, string? preferredVaultRoot = null)
	{
		if (string.IsNullOrWhiteSpace(destinationArchivePath))
		{
			throw new InvalidOperationException("Escolha um arquivo .zip valido para exportar.");
		}
		string text = EnsurePortableVaultRootExists();
		string? obj = ResolveVaultRoot(preferredVaultRoot) ?? throw new VaultNotFoundException("Nenhum cofre encontrado. Use o botao Importar maFile para criar um cofre local em " + text + ".");
		if (!Directory.EnumerateFiles(obj, "*.maFile", SearchOption.TopDirectoryOnly).Any())
		{
			throw new InvalidOperationException("Nao ha contas no cofre atual para exportar.");
		}
		string? directoryName = Path.GetDirectoryName(destinationArchivePath);
		if (string.IsNullOrWhiteSpace(directoryName))
		{
			throw new InvalidOperationException("Nao foi possivel descobrir a pasta de destino do arquivo exportado.");
		}
		Directory.CreateDirectory(directoryName);
		if (File.Exists(destinationArchivePath))
		{
			File.Delete(destinationArchivePath);
		}
		ZipFile.CreateFromDirectory(obj, destinationArchivePath, CompressionLevel.Fastest, includeBaseDirectory: true);
		return destinationArchivePath;
	}

	public RemoveAccountResult RemoveAccount(ulong steamId, bool deleteMaFile = true, string? preferredVaultRoot = null)
	{
		string text = (string.IsNullOrWhiteSpace(preferredVaultRoot) ? GetDefaultVaultRoot() : preferredVaultRoot);
		string text2 = Path.Combine(text, "manifest.json");
		VaultManifest vaultManifest = (File.Exists(text2) ? LoadManifest(text2) : new VaultManifest());
		VaultManifestEntry vaultManifestEntry = vaultManifest.Entries.FirstOrDefault((VaultManifestEntry x) => x.SteamID == steamId);
		if (vaultManifestEntry == null)
		{
			return new RemoveAccountResult
			{
				VaultRoot = text,
				IsEncrypted = vaultManifest.Encrypted,
				RemovedSteamId = steamId,
				AccountRemoved = false
			};
		}
		string path = Path.Combine(text, vaultManifestEntry.Filename);
		vaultManifest.Entries.Remove(vaultManifestEntry);
		if (vaultManifest.Entries.Count == 0)
		{
			vaultManifest.Encrypted = false;
		}
		SaveManifest(text2, vaultManifest);
		if (deleteMaFile && File.Exists(path))
		{
			File.Delete(path);
		}
		return new RemoveAccountResult
		{
			VaultRoot = text,
			IsEncrypted = vaultManifest.Encrypted,
			RemovedSteamId = steamId,
			AccountRemoved = true
		};
	}

	public VaultProtectionResult UpdateProtection(string? currentPassphrase, string? newPassphrase, string? preferredVaultRoot = null)
	{
		string text = (string.IsNullOrWhiteSpace(preferredVaultRoot) ? GetDefaultVaultRoot() : preferredVaultRoot);
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, "manifest.json");
		VaultManifest vaultManifest = (File.Exists(text2) ? LoadManifest(text2) : new VaultManifest());
		IReadOnlyList<VaultAccountRecord> source;
		if (vaultManifest.Entries.Count != 0)
		{
			source = LoadAccountsFromManifest(text, vaultManifest, currentPassphrase);
		}
		else
		{
			IReadOnlyList<VaultAccountRecord> readOnlyList = Array.Empty<VaultAccountRecord>();
			source = readOnlyList;
		}
		VaultManifest vaultManifest2 = new VaultManifest
		{
			Encrypted = !string.IsNullOrWhiteSpace(newPassphrase)
		};
		foreach (VaultAccountRecord item in source.OrderBy((VaultAccountRecord x) => x.SteamId))
		{
			string text3 = $"{item.SteamId}.maFile";
			string path = Path.Combine(text, text3);
			string text4 = JsonConvert.SerializeObject(item.Account);
			string salt = null;
			string text5 = null;
			if (vaultManifest2.Encrypted)
			{
				salt = VaultCrypto.GetRandomSalt();
				text5 = VaultCrypto.GetInitializationVector();
				text4 = VaultCrypto.Encrypt(newPassphrase, salt, text5, text4);
			}
			File.WriteAllText(path, text4);
			vaultManifest2.Entries.Add(new VaultManifestEntry
			{
				Filename = text3,
				SteamID = item.SteamId,
				Salt = salt,
				IV = text5
			});
		}
		SaveManifest(text2, vaultManifest2);
		return new VaultProtectionResult
		{
			VaultRoot = text,
			IsEncrypted = vaultManifest2.Encrypted,
			ActivePassphrase = (vaultManifest2.Encrypted ? newPassphrase : null)
		};
	}

	private static IReadOnlyList<VaultAccountRecord> LoadAccountsFromManifest(string vaultRoot, VaultManifest manifest, string? passphrase)
	{
		if (manifest.Encrypted && string.IsNullOrWhiteSpace(passphrase))
		{
			throw new VaultPassphraseRequiredException();
		}
		if (manifest.Entries.Count == 0)
		{
			return Array.Empty<VaultAccountRecord>();
		}
		List<VaultAccountRecord> list = new List<VaultAccountRecord>(manifest.Entries.Count);
		foreach (VaultManifestEntry entry in manifest.Entries)
		{
			string text = Path.Combine(vaultRoot, entry.Filename);
			if (!File.Exists(text))
			{
				throw new VaultManifestIncompleteException("O arquivo " + entry.Filename + " ainda nao terminou de copiar para o cofre.");
			}
			string text2;
			try
			{
				text2 = File.ReadAllText(text);
			}
			catch (IOException innerException)
			{
				throw new VaultManifestIncompleteException("O arquivo " + entry.Filename + " ainda esta sendo gravado.", innerException);
			}
			catch (UnauthorizedAccessException innerException2)
			{
				throw new VaultManifestIncompleteException("O arquivo " + entry.Filename + " ainda esta indisponivel para leitura.", innerException2);
			}
			if (manifest.Encrypted)
			{
				text2 = VaultCrypto.Decrypt(passphrase, entry.Salt, entry.IV, text2) ?? throw new VaultPassphraseInvalidException();
			}
			SteamGuardAccount steamGuardAccount = TryDeserializeAccount(text2);
			if (steamGuardAccount == null)
			{
				throw new VaultManifestIncompleteException("O arquivo " + entry.Filename + " ainda nao ficou valido para leitura.");
			}
			list.Add(new VaultAccountRecord
			{
				Account = steamGuardAccount,
				SourceFile = text,
				SteamId = ResolveSteamId(steamGuardAccount, entry.SteamID)
			});
		}
		return list;
	}

	private static SourceAccountResult ReadSourceAccount(string sourceFilePath, string? sourcePassphrase)
	{
		string text = File.ReadAllText(sourceFilePath);
		SteamGuardAccount steamGuardAccount = TryDeserializeAccount(text);
		if (steamGuardAccount != null)
		{
			return new SourceAccountResult
			{
				Account = steamGuardAccount,
				SteamId = ResolveSteamId(steamGuardAccount, 0uL)
			};
		}
		FileInfo sourceFile = new FileInfo(sourceFilePath);
		string text2 = Path.Combine(sourceFile.DirectoryName ?? string.Empty, "manifest.json");
		if (!File.Exists(text2))
		{
			throw new InvalidOperationException("Nao foi possivel entender o maFile selecionado. Escolha um arquivo vindo de uma pasta maFiles valida.");
		}
		VaultManifestEntry vaultManifestEntry = LoadManifest(text2).Entries.FirstOrDefault((VaultManifestEntry entry) => string.Equals(entry.Filename, sourceFile.Name, StringComparison.OrdinalIgnoreCase));
		if (vaultManifestEntry == null || string.IsNullOrWhiteSpace(vaultManifestEntry.Salt) || string.IsNullOrWhiteSpace(vaultManifestEntry.IV))
		{
			throw new InvalidOperationException("O maFile selecionado nao possui metadados suficientes para importacao.");
		}
		if (string.IsNullOrWhiteSpace(sourcePassphrase))
		{
			throw new VaultSourcePassphraseRequiredException();
		}
		SteamGuardAccount steamGuardAccount2 = TryDeserializeAccount(VaultCrypto.Decrypt(sourcePassphrase, vaultManifestEntry.Salt, vaultManifestEntry.IV, text) ?? throw new VaultSourcePassphraseInvalidException());
		if (steamGuardAccount2 == null)
		{
			throw new InvalidOperationException("O maFile descriptografado ficou invalido.");
		}
		return new SourceAccountResult
		{
			Account = steamGuardAccount2,
			SteamId = ResolveSteamId(steamGuardAccount2, vaultManifestEntry.SteamID)
		};
	}

	private static VaultManifest LoadManifest(string manifestPath)
	{
		try
		{
			return JsonConvert.DeserializeObject<VaultManifest>(File.ReadAllText(manifestPath)) ?? new VaultManifest();
		}
		catch (IOException innerException)
		{
			throw new VaultManifestIncompleteException("O manifest do cofre ainda esta sendo gravado.", innerException);
		}
		catch (UnauthorizedAccessException innerException2)
		{
			throw new VaultManifestIncompleteException("O manifest do cofre ainda esta indisponivel para leitura.", innerException2);
		}
		catch (JsonException innerException3)
		{
			throw new VaultManifestIncompleteException("O manifest do cofre ainda nao terminou de copiar.", innerException3);
		}
	}

	private static void SaveManifest(string manifestPath, VaultManifest manifest)
	{
		string contents = JsonConvert.SerializeObject(manifest, Formatting.Indented);
		File.WriteAllText(manifestPath, contents);
	}

	private static SteamGuardAccount? TryDeserializeAccount(string rawText)
	{
		try
		{
			return JsonConvert.DeserializeObject<SteamGuardAccount>(rawText);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static ulong ResolveSteamId(SteamGuardAccount account, ulong fallbackSteamId)
	{
		ulong num = account.Session?.SteamID ?? 0;
		if (num != 0L)
		{
			return num;
		}
		if (fallbackSteamId != 0L)
		{
			return fallbackSteamId;
		}
		throw new InvalidOperationException("O maFile selecionado nao possui um SteamID valido.");
	}

	private static string? ResolveVaultRoot(string? preferredVaultRoot = null)
	{
		if (!string.IsNullOrWhiteSpace(preferredVaultRoot) && File.Exists(Path.Combine(preferredVaultRoot, "manifest.json")))
		{
			return preferredVaultRoot;
		}
		string defaultVaultRoot = GetDefaultVaultRoot();
		if (File.Exists(Path.Combine(defaultVaultRoot, "manifest.json")))
		{
			return defaultVaultRoot;
		}
		return null;
	}

	private static string GetDefaultVaultRoot()
	{
		return Path.Combine(AppContext.BaseDirectory, "maFiles");
	}
}
