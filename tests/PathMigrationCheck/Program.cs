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
    Console.WriteLine("Portable data migration check passed.");
}
finally
{
    Directory.Delete(root, recursive: true);
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
