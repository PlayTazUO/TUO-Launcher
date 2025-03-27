namespace TazUOLauncher;

public enum ReleaseChannel
{
    INVALID,
    MAIN,
    DEV,
    LAUNCHER
}

public enum ClientStatus
{
    INITIALIZING,
    DOWNLOAD_IN_PROGRESS,
    NO_LOCAL_CLIENT,
    READY
}