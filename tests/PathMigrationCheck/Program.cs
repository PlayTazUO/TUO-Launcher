using TazUOLauncher;

string root = Directory.CreateTempSubdirectory("tazuo-path-migration-").FullName;
try
{
    string legacy = Path.Combine(root, "portable");
    string data = Path.Combine(root, "support");
    Directory.CreateDirectory(Path.Combine(legacy, "Profiles"));
    Directory.CreateDirectory(Path.Combine(legacy, "TazUO"));
    Directory.CreateDirectory(Path.Combine(data, "Profiles"));
    File.WriteAllText(Path.Combine(legacy, "launcherdata.json"), "legacy settings");
    File.WriteAllText(Path.Combine(legacy, "Profiles", "profile.json"), "legacy profile");
    File.WriteAllText(Path.Combine(legacy, "TazUO", "client.bin"), "client");
    File.WriteAllText(Path.Combine(data, "Profiles", "profile.json"), "existing profile");

    Require(PortableDataMigrator.TryMigrate(legacy, data), "migration should complete");
    Require(File.ReadAllText(Path.Combine(data, "launcherdata.json")) == "legacy settings", "launcher settings were not copied");
    Require(File.ReadAllText(Path.Combine(data, "Profiles", "profile.json")) == "existing profile", "existing data was overwritten");
    Require(File.ReadAllText(Path.Combine(data, "TazUO", "client.bin")) == "client", "client files were not copied");
    Require(File.Exists(Path.Combine(legacy, "Profiles", "profile.json")), "legacy data was removed");

    File.WriteAllText(Path.Combine(legacy, "Profiles", "later.json"), "later");
    Require(PortableDataMigrator.TryMigrate(legacy, data), "completed migration should be idempotent");
    Require(!File.Exists(Path.Combine(data, "Profiles", "later.json")), "completed migration unexpectedly recopied legacy files");

    CheckSingleDirectoryMigration(root, "Profiles");
    CheckSingleDirectoryMigration(root, CONSTANTS.CLIENT_DIRECTORY_NAME);
    Console.WriteLine("Portable data migration check passed.");
}
finally
{
    Directory.Delete(root, recursive: true);
}

static void CheckSingleDirectoryMigration(string root, string directoryName)
{
    string legacy = Path.Combine(root, $"portable-{directoryName}");
    string data = Path.Combine(root, $"support-{directoryName}");
    string sourceFile = Path.Combine(legacy, directoryName, "legacy.bin");
    Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
    File.WriteAllText(sourceFile, "legacy data");

    Require(PortableDataMigrator.TryMigrate(legacy, data), $"{directoryName}-only migration should complete");
    Require(File.Exists(Path.Combine(data, directoryName, "legacy.bin")), $"{directoryName} was not migrated without launcherdata.json");
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
