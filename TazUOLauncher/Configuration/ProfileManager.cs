using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TazUOLauncher;

static class ProfileManager
{
    public static Profile[] AllProfiles = [];
    public static Task<Profile[]> GetAllProfiles()
    {
        return Task<Profile[]>.Factory.StartNew(() =>
        {
            if (Directory.Exists(PathHelper.ProfilesPath))
            {
                string[] profiles = Directory.GetFiles(PathHelper.ProfilesPath, "*.json", SearchOption.TopDirectoryOnly);

                List<Profile> list = new List<Profile>();

                foreach (string profile in profiles)
                {
                    try
                    {
                        var loadedProfile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(profile));
                        if (loadedProfile != null)
                        {
                            list.Add(loadedProfile);
                            if (loadedProfile.GetProfileFilePath() != profile)
                            {
                                File.Delete(profile); //Remove wrongly named files
                                loadedProfile.Save();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"---- Error loading profile from [ {profile} ] ---");
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine();
                    }
                }

                if (list.Count == 0)
                {
                    Profile blank = new Profile();
                    blank.Save();
                    list.Add(blank);
                }

                AllProfiles = list.ToArray();
                return list.ToArray();
            }
            else //Create new profile directory and a blank profile
            {
                try
                {
                    Directory.CreateDirectory(PathHelper.ProfilesPath);
                    Profile blank = new Profile();
                    blank.Save();
                    return new Profile[] { blank };

                }
                catch (Exception e)
                {
                    Console.WriteLine("---- Error creating profile directory ---");
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine();
                }
                return Array.Empty<Profile>();
            }
        });
    }
    public static bool TryFindProfile(string? name, out Profile? profile)
    {
        if (name == null)
        {
            profile = null;
            return false;
        }

        if (AllProfiles.Length == 0)
        {
            Task<Profile[]> task = GetAllProfiles();
            task.Wait();
        }

        foreach (Profile p in AllProfiles)
        {
            if (p.Name.Equals(name))
            {
                profile = p;
                return true;
            }
        }

        profile = null;
        return false;
    }
    public static void DeleteProfileFile(Profile profile, bool alsoDeleteSettingsFile)
    {
        try
        {
            if (File.Exists(profile.GetProfileFilePath()))
            {
                File.Delete(profile.GetProfileFilePath());
                profile.IsDeleted = true;
            }

            if (alsoDeleteSettingsFile && File.Exists(profile.GetSettingsFilePath()))
            {
                File.Delete(profile.GetSettingsFilePath());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("---- Error deleting profile ---");
            Console.WriteLine(e.ToString());
            Console.WriteLine();
        }
    }
    public static string[] GetProfileNames()
    {
        List<string> names = new List<string>();
        foreach (Profile p in AllProfiles)
        {
            names.Add(p.Name);
        }
        return names.ToArray();
    }
    public static string EnsureUniqueName(string fName)
    {
        HashSet<string> existingNames = new HashSet<string>(GetProfileNames());
        int it = 1;
        string newName = fName;
        while (existingNames.Contains(newName))
        {
            newName = fName + it;
            it++;
        }
        return newName;
    }
}
