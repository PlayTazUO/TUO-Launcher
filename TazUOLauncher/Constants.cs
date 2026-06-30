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

    // PR test builds: the TUO-PR-Build action publishes a GitHub release for a PR (named after the PR title)
    // with the same per-platform zips that normal releases provide, tagged "pr-<number>-test-build". These
    // let players test features in open PRs before they are merged. Release assets have public download URLs,
    // so no token is needed. {0} is the PR number.
    public const string PR_LIST_URL = "https://api.github.com/repos/PlayTazUO/TazUO/pulls?state=open&per_page=100";
    public const string RELEASES_URL = "https://api.github.com/repos/PlayTazUO/TazUO/releases?per_page=100";
    public const string PR_BUILD_TAG_FORMAT = "pr-{0}-test-build";
}