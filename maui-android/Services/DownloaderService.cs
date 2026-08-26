using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace YTDownloaderXPro.Services;

public class DownloaderService
{
    private async Task<string> RunYtDlpAsync(string args)
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
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new Exception($"yt-dlp failed: {err}");
        }
        return output;
    }

    public async Task<JObject> FetchOverviewAsync(string url)
    {
        var output = await RunYtDlpAsync($"--dump-json --flat-playlist --skip-download \"{url}\"");
        return JObject.Parse(output);
    }

    public async Task<JObject> FetchFormatsAsync(string url)
    {
        var output = await RunYtDlpAsync($"--dump-json --skip-download \"{url}\"");
        return JObject.Parse(output);
    }

    public async Task<JObject> ResolveDirectAsync(string url, string formatId)
    {
        var output = await RunYtDlpAsync($"--dump-json --skip-download -f \"{formatId}\" \"{url}\"");
        return JObject.Parse(output);
    }

    public async Task<string> DownloadAsync(string url, string formatId, string outputTemplate,
        Action<double, double, double>? progressCallback = null, CancellationToken ct = default)
    {
        var args = $"-f \"{formatId}\" -o \"{outputTemplate}\" --no-playlist --continue --newline --extractor-args \"youtube:player_client=android,ios,web\" \"{url}\"";
        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = new Process { StartInfo = psi };
        proc.Start();

        string outputFile = "";
        while (!proc.StandardOutput.EndOfStream)
        {
            if (ct.IsCancellationRequested) { proc.Kill(); throw new OperationCanceledException(); }
            var line = await proc.StandardOutput.ReadLineAsync();
            if (line == null) break;
            // Parse progress
            if (line.Contains("[download]") && line.Contains('%'))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // [download] 45.2% of 100.00MiB at 5.00MiB/s ETA 00:12
                // progressCallback?.Invoke(percent, speed, eta);
            }
            if (line.Contains("Destination:"))
            {
                var idx = line.IndexOf("Destination:") + "Destination:".Length;
                outputFile = line.Substring(idx).Trim();
            }
        }
        await proc.WaitForExitAsync(ct);
        return outputFile;
    }
}
