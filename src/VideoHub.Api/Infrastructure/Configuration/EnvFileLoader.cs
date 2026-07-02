using System.Text;

namespace VideoHub.Api.Infrastructure.Configuration;

public static class EnvFileLoader
{
    public static void Load(string? basePath = null)
    {
        var searchRoot = new DirectoryInfo(basePath ?? Directory.GetCurrentDirectory());
        var envFile = FindEnvFile(searchRoot);
        if (envFile is null)
        {
            return;
        }

        foreach (var line in File.ReadAllLines(envFile.FullName, Encoding.UTF8))
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

            if (!string.IsNullOrWhiteSpace(key) && Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static FileInfo? FindEnvFile(DirectoryInfo? directory)
    {
        var current = directory;

        while (current is not null)
        {
            var candidate = new FileInfo(Path.Combine(current.FullName, ".env"));
            if (candidate.Exists)
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
