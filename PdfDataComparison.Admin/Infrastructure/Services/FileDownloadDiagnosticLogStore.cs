using System.Text.Json;
using PdfDataComparison.Admin.Application.Interfaces;
using PdfDataComparison.Admin.Application.ViewModels;

namespace PdfDataComparison.Admin.Infrastructure.Services;

public class FileDownloadDiagnosticLogStore(IWebHostEnvironment environment) : IDownloadDiagnosticLogStore
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly string logPath = Path.Combine(environment.ContentRootPath, "App_Data", "download-diagnostics.jsonl");

    public async Task WriteAsync(DownloadDiagnosticLogEntryVm entry)
    {
        entry.TimestampUtc = entry.TimestampUtc == default ? DateTime.UtcNow : entry.TimestampUtc;

        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(entry) + Environment.NewLine;

        await FileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(logPath, line);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<IReadOnlyList<DownloadDiagnosticLogEntryVm>> ReadLatestAsync(int take = 200)
    {
        if (!File.Exists(logPath)) return Array.Empty<DownloadDiagnosticLogEntryVm>();

        take = Math.Clamp(take, 1, 1000);
        await FileLock.WaitAsync();
        try
        {
            var queue = new Queue<string>(take);
            await foreach (var line in File.ReadLinesAsync(logPath))
            {
                if (queue.Count == take)
                {
                    queue.Dequeue();
                }

                queue.Enqueue(line);
            }

            return queue
                .Reverse()
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(TryDeserialize)
                .Where(entry => entry != null)
                .Cast<DownloadDiagnosticLogEntryVm>()
                .ToList();
        }
        finally
        {
            FileLock.Release();
        }
    }

    private static DownloadDiagnosticLogEntryVm? TryDeserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<DownloadDiagnosticLogEntryVm>(line);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
