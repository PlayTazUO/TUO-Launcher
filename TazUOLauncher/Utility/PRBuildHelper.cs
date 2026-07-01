using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TazUOLauncher;

/// <summary>
/// Loads PRs from PlayTazUO/TazUO that have a build published by the TUO-PR-Build action and installs them,
/// allowing players to test features in open PRs before they are merged. The action publishes a GitHub
/// release whose name and tag match the PR title, so builds are matched to open PRs by that title.
/// </summary>
internal static class PRBuildHelper
{
    private static readonly HttpClient HttpClient = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        client.DefaultRequestHeaders.Add("User-Agent", "TazUOLauncher");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        return client;
    }

    /// <summary>
    /// Returns open PRs that have a published build (a release whose name or tag matches the PR title and
    /// contains an asset for the current platform). Returns an empty list when none are available.
    /// </summary>
    public static async Task<List<PRBuild>> GetPRBuildsAsync()
    {
        var result = new List<PRBuild>();

        try
        {
            var prsTask = HttpClient.GetStringAsync(CONSTANTS.PR_LIST_URL);
            var releasesTask = HttpClient.GetStringAsync(CONSTANTS.RELEASES_URL);
            await Task.WhenAll(prsTask, releasesTask);

            var prs = JsonSerializer.Deserialize<List<PullRequestInfo>>(prsTask.Result);
            var releases = JsonSerializer.Deserialize<List<GitHubReleaseData>>(releasesTask.Result);
            if (prs == null || releases == null) return result;

            // Index non-draft releases by their tag (e.g. "pr-123-test-build") for deterministic PR matching.
            var releasesByTag = new Dictionary<string, GitHubReleaseData>(StringComparer.OrdinalIgnoreCase);
            foreach (var release in releases)
            {
                if (release.draft) continue;
                if (!string.IsNullOrWhiteSpace(release.tag_name))
                    releasesByTag.TryAdd(release.tag_name.Trim(), release);
            }

            string platformZipName = PlatformHelper.GetPlatformZipName();

            foreach (var pr in prs)
            {
                string expectedTag = string.Format(CONSTANTS.PR_BUILD_TAG_FORMAT, pr.number);
                if (!releasesByTag.TryGetValue(expectedTag, out var release)) continue;

                string? downloadUrl = FindPlatformAssetUrl(release, platformZipName);
                if (downloadUrl == null) continue;

                result.Add(new PRBuild
                {
                    PrNumber = pr.number,
                    Title = string.IsNullOrWhiteSpace(pr.title) ? (release.name ?? expectedTag) : pr.title.Trim(),
                    Tag = release.tag_name ?? expectedTag,
                    DownloadUrl = downloadUrl,
                    HtmlUrl = pr.html_url
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load PR builds: {e}");
        }

        return result;
    }

    /// <summary>Picks the release asset matching the current platform, mirroring UpdateHelper's selection.</summary>
    private static string? FindPlatformAssetUrl(GitHubReleaseData release, string platformZipName)
    {
        if (release.assets == null) return null;

        // Prefer the platform-specific zip.
        foreach (var asset in release.assets)
        {
            if (asset.name != null && asset.name.EndsWith(platformZipName, StringComparison.OrdinalIgnoreCase) && asset.browser_download_url != null)
                return asset.browser_download_url;
        }

        // Fall back to the legacy single-zip naming (e.g. TazUO.zip).
        foreach (var asset in release.assets)
        {
            if (asset.name != null && asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                asset.name.StartsWith(CONSTANTS.ZIP_STARTS_WITH, StringComparison.OrdinalIgnoreCase) && asset.browser_download_url != null)
                return asset.browser_download_url;
        }

        return null;
    }

    /// <summary>
    /// Downloads the build's platform asset and installs it into the client folder. Returns false on failure.
    /// </summary>
    public static async Task<bool> DownloadAndInstallPRBuildAsync(PRBuild build, DownloadProgress progress)
    {
        if (string.IsNullOrEmpty(build.DownloadUrl)) return false;

        try
        {
            string tempZip = Path.GetTempFileName();
            using (var file = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await HttpClient.DownloadAsync(build.DownloadUrl, file, progress);
            }

            await Task.Run(() =>
            {
                // Match channel-switch behaviour: wipe the old client first so leftover files don't break the build.
                ClientHelper.CleanUpClientFiles();
                Directory.CreateDirectory(PathHelper.ClientPath);
                ZipFile.ExtractToDirectory(tempZip, PathHelper.ClientPath, true);
                try { File.Delete(tempZip); } catch { }
            });

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to install PR build: {e}");
            return false;
        }
    }

    // --- GitHub API models (property names mirror the JSON fields) ---

    private class PullRequestInfo
    {
        public int number { get; set; }
        public string? title { get; set; }
        public string? html_url { get; set; }
    }
}

/// <summary>A PR build entry surfaced in the launcher menu.</summary>
public class PRBuild
{
    public int PrNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? HtmlUrl { get; set; }

    public string DisplayName => $"#{PrNumber} {Title}";
}

/// <summary>A single entry in the dynamic PR-builds menu (refresh action, status text, or a build).</summary>
public class PRBuildMenuItem
{
    public string Header { get; set; } = string.Empty;
    public System.Windows.Input.ICommand? Command { get; set; }
}
