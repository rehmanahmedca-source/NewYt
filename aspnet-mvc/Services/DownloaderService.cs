using System.Diagnostics;
using System.Text.Json;

namespace YTDownloaderXPro.Services;

public class DownloaderService
{
    private string RunYtDlp(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = $"--extractor-args \"youtube:player_client=android,ios,web\" --quiet --no-warnings {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(120000);
        if (proc.ExitCode != 0)
            throw new Exception($"yt-dlp failed: {proc.StandardError.ReadToEnd()}");
        return output;
    }

    public JsonDocument FetchOverview(string url)
    {
        var output = RunYtDlp($"--dump-json --flat-playlist --skip-download \"{url}\"");
        return JsonDocument.Parse(output);
    }

    public JsonDocument FetchFormats(string url)
    {
        var output = RunYtDlp($"--dump-json --skip-download \"{url}\"");
        return JsonDocument.Parse(output);
    }

    public JsonDocument ResolveDirect(string url, string formatId)
    {
        var output = RunYtDlp($"--dump-json --skip-download -f \"{formatId}\" \"{url}\"");
        return JsonDocument.Parse(output);
    }
}
