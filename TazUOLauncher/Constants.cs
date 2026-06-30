namespace TazUOLauncher;

internal static class CONSTANTS {
    public const string WEBSITE_URL = "https://tazuo.org";
    public const string GITHUB_URL = "https://github.com/PlayTazUO/TazUO";
    public const string DEV_CHANNEL_RELEASE_URL = "https://api.github.com/repos/PlayTazUO/TazUO/releases/tags/TazUO-BleedingEdge";
    public const string MAIN_CHANNEL_RELEASE_URL = "https://api.github.com/repos/PlayTazUO/TazUO/releases/latest";
    public const string LAUNCHER_RELEASE_URL = "https://api.github.com/repos/PlayTazUO/TUO-Launcher/releases/latest";
    public const string LAUNCHER_LATEST_URL = "https://github.com/PlayTazUO/TUO-Launcher/releases/latest";
    public const string NET472_CHANNEL_RELEASE_URL = "https://api.github.com/repos/PlayTazUO/TazUO/releases/tags/TazUO-Legacy";
    public const string CHANGE_LOG_URL = "https://raw.githubusercontent.com/PlayTazUO/TazUO/refs/heads/{0}/CHANGELOG.md";
    public const string REMOTE_VERSION_FORMAT = "Remote Version: {0}";
    public const string LOCAL_VERSION_FORMAT = "Local Version: {0}";
    public const string CLIENT_DIRECTORY_NAME = "TazUO";
    public const string CLIENT_UPDATE_AVAILABLE = "TazUO update available";
    public const string NO_CLIENT_AVAILABLE = "TazUO not installed";
    public const string EDIT_PROFILES = "[ Edit Profiles ]";
    public const string ZIP_STARTS_WITH = "TazUO";
    public const string CLASSIC_EXE_NAME = "ClassicUO";
    public const string NATIVE_EXECUTABLE_NAME = "TazUO";
    public const string PROCESS_NAME = "TazUO";

    // PR test builds: artifacts produced by the TUO-PR-Build action on PlayTazUO/TazUO.
    // These let players test features in open PRs before they are merged.
    public const string PR_BUILD_WORKFLOW_NAME = "TUO-PR-Build";
    public const string PR_BUILD_WORKFLOW_FILE = "tuo-pr-build.yml";
    public const string PR_BUILD_WORKFLOWS_URL = "https://api.github.com/repos/PlayTazUO/TazUO/actions/workflows?per_page=100";
    public const string PR_BUILD_WORKFLOW_RUNS_URL = "https://api.github.com/repos/PlayTazUO/TazUO/actions/workflows/{0}/runs?per_page=50";
    public const string PR_BUILD_RUN_ARTIFACTS_URL = "https://api.github.com/repos/PlayTazUO/TazUO/actions/runs/{0}/artifacts";
    public const string PR_LIST_URL = "https://api.github.com/repos/PlayTazUO/TazUO/pulls?state=open&per_page=100";
    // GitHub Actions artifacts cannot be downloaded anonymously through the API, so we use nightly.link,
    // a free public proxy that produces tokenless download links for any public repository's artifacts.
    public const string ARTIFACT_DOWNLOAD_URL = "https://nightly.link/PlayTazUO/TazUO/actions/artifacts/{0}.zip";
}