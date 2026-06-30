using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TazUOLauncher;

/// <summary>
/// Loads PRs from PlayTazUO/TazUO that have build artifacts produced by the TUO-PR-Build action and
/// installs them, allowing players to test features in open PRs before they are merged.
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
    /// Returns the latest successful TUO-PR-Build run per branch. Returns an empty list when the
    /// workflow does not exist or no successful runs are available.
    /// </summary>
    public static async Task<List<PRBuild>> GetPRBuildsAsync()
    {
        var result = new List<PRBuild>();

        try
        {
            long? workflowId = await GetWorkflowIdAsync();
            if (workflowId == null) return result;

            string runsUrl = string.Format(CONSTANTS.PR_BUILD_WORKFLOW_RUNS_URL, workflowId.Value);
            string runsJson = await HttpClient.GetStringAsync(runsUrl);
            var runs = JsonSerializer.Deserialize<WorkflowRunsResponse>(runsJson);
            if (runs?.workflow_runs == null) return result;

            // Used to show friendly "#123 Title" labels instead of raw branch names.
            var prByBranch = await GetOpenPullRequestsByBranchAsync();

            // Runs come back newest first; keep only the most recent successful run per branch.
            var seenBranches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var run in runs.workflow_runs)
            {
                if (string.IsNullOrEmpty(run.head_branch)) continue;
                if (!string.Equals(run.conclusion, "success", StringComparison.OrdinalIgnoreCase)) continue;
                if (!seenBranches.Add(run.head_branch)) continue;

                var build = new PRBuild
                {
                    RunId = run.id,
                    Branch = run.head_branch,
                    HtmlUrl = run.html_url,
                    CreatedAt = run.created_at
                };

                if (prByBranch.TryGetValue(run.head_branch, out var pr))
                {
                    build.PrNumber = pr.number;
                    build.Title = pr.title ?? run.head_branch;
                }
                else
                {
                    build.Title = run.display_title ?? run.head_branch;
                }

                result.Add(build);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load PR builds: {e}");
        }

        return result;
    }

    private static async Task<long?> GetWorkflowIdAsync()
    {
        try
        {
            string json = await HttpClient.GetStringAsync(CONSTANTS.PR_BUILD_WORKFLOWS_URL);
            var response = JsonSerializer.Deserialize<WorkflowsResponse>(json);
            if (response?.workflows == null) return null;

            foreach (var workflow in response.workflows)
            {
                if (string.Equals(workflow.name, CONSTANTS.PR_BUILD_WORKFLOW_NAME, StringComparison.OrdinalIgnoreCase) ||
                    (workflow.path != null && workflow.path.EndsWith(CONSTANTS.PR_BUILD_WORKFLOW_FILE, StringComparison.OrdinalIgnoreCase)))
                {
                    return workflow.id;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to look up PR build workflow: {e}");
        }

        return null;
    }

    private static async Task<Dictionary<string, PullRequestInfo>> GetOpenPullRequestsByBranchAsync()
    {
        var map = new Dictionary<string, PullRequestInfo>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string json = await HttpClient.GetStringAsync(CONSTANTS.PR_LIST_URL);
            var prs = JsonSerializer.Deserialize<List<PullRequestInfo>>(json);
            if (prs != null)
            {
                foreach (var pr in prs)
                {
                    if (pr.head?.@ref != null)
                        map[pr.head.@ref] = pr;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load open PRs: {e}");
        }

        return map;
    }

    /// <summary>
    /// Finds the platform-specific artifact for the build, downloads it, and installs it into the client folder.
    /// Returns false when no suitable (non-expired) artifact is available or the download/install fails.
    /// </summary>
    public static async Task<bool> DownloadAndInstallPRBuildAsync(PRBuild build, DownloadProgress progress)
    {
        try
        {
            string artifactsUrl = string.Format(CONSTANTS.PR_BUILD_RUN_ARTIFACTS_URL, build.RunId);
            string json = await HttpClient.GetStringAsync(artifactsUrl);
            var response = JsonSerializer.Deserialize<ArtifactsResponse>(json);
            if (response?.artifacts == null) return false;

            string platformZipName = PlatformHelper.GetPlatformZipName();

            ArtifactInfo? selected = response.artifacts.FirstOrDefault(a =>
                !a.expired && a.name != null && a.name.EndsWith(platformZipName, StringComparison.OrdinalIgnoreCase));

            if (selected == null) return false;

            string downloadUrl = string.Format(CONSTANTS.ARTIFACT_DOWNLOAD_URL, selected.id);

            string outerZip = Path.GetTempFileName();
            using (var file = new FileStream(outerZip, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await HttpClient.DownloadAsync(downloadUrl, file, progress);
            }

            await Task.Run(() => InstallArtifactZip(outerZip, platformZipName));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to install PR build: {e}");
            return false;
        }
    }

    /// <summary>
    /// Installs the downloaded artifact. GitHub wraps uploaded files in an outer zip, so a release-style
    /// artifact arrives double-nested: outer artifact zip -> inner platform zip -> client files.
    /// </summary>
    private static void InstallArtifactZip(string outerZipPath, string platformZipName)
    {
        string extractTo = PathHelper.ClientPath;
        string tempDir = Directory.CreateTempSubdirectory().FullName;

        try
        {
            ZipFile.ExtractToDirectory(outerZipPath, tempDir, true);

            string? innerZip = Directory
                .EnumerateFiles(tempDir, "*.zip", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileName(f).EndsWith(platformZipName, StringComparison.OrdinalIgnoreCase))
                ?? Directory.EnumerateFiles(tempDir, "*.zip", SearchOption.AllDirectories).FirstOrDefault();

            // Match channel-switch behaviour: wipe the old client first so leftover files don't break the build.
            ClientHelper.CleanUpClientFiles();
            Directory.CreateDirectory(extractTo);

            if (innerZip != null)
            {
                ZipFile.ExtractToDirectory(innerZip, extractTo, true);
            }
            else
            {
                // No nested zip: the artifact already contains the raw client files.
                CopyDirectory(tempDir, extractTo);
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
            try { File.Delete(outerZipPath); } catch { }
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDir, file);
            string target = Path.Combine(destDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    // --- GitHub API models (property names mirror the JSON fields) ---

    private class WorkflowsResponse
    {
        public List<WorkflowInfo>? workflows { get; set; }
    }

    private class WorkflowInfo
    {
        public long id { get; set; }
        public string? name { get; set; }
        public string? path { get; set; }
    }

    private class WorkflowRunsResponse
    {
        public List<WorkflowRun>? workflow_runs { get; set; }
    }

    private class WorkflowRun
    {
        public long id { get; set; }
        public string? name { get; set; }
        public string? head_branch { get; set; }
        public string? display_title { get; set; }
        public string? status { get; set; }
        public string? conclusion { get; set; }
        public string? html_url { get; set; }
        public string? created_at { get; set; }
    }

    private class ArtifactsResponse
    {
        public List<ArtifactInfo>? artifacts { get; set; }
    }

    private class ArtifactInfo
    {
        public long id { get; set; }
        public string? name { get; set; }
        public bool expired { get; set; }
    }

    private class PullRequestInfo
    {
        public int number { get; set; }
        public string? title { get; set; }
        public PrHead? head { get; set; }
    }

    private class PrHead
    {
        public string? @ref { get; set; }
    }
}

/// <summary>A PR build entry surfaced in the launcher menu.</summary>
public class PRBuild
{
    public long RunId { get; set; }
    public string Branch { get; set; } = string.Empty;
    public int? PrNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? HtmlUrl { get; set; }
    public string? CreatedAt { get; set; }

    public string DisplayName => PrNumber.HasValue ? $"#{PrNumber} {Title}" : $"{Branch}: {Title}";
}

/// <summary>A single entry in the dynamic PR-builds menu (refresh action, status text, or a build).</summary>
public class PRBuildMenuItem
{
    public string Header { get; set; } = string.Empty;
    public System.Windows.Input.ICommand? Command { get; set; }
}
