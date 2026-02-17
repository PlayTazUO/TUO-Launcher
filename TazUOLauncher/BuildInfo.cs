namespace TazUOLauncher;

public static class BuildInfo
{
    public static bool IsDebug => 
#if DEBUG
        false; //Change to true to see more elements in the UI that would normally be hidden/dependent on release data
#else
        false;
#endif
}