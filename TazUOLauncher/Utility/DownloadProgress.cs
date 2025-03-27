using System;

namespace TazUOLauncher;
public class DownloadProgress : IProgress<float>
{
    public event EventHandler? DownloadProgressChanged;

    public float ProgressPercentage { get; set; }

    public void Report(float value)
    {
        ProgressPercentage = value;
        DownloadProgressChanged?.Invoke(this, EventArgs.Empty);
    }
}