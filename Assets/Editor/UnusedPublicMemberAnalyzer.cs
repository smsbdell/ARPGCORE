#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UnityEditor;
using UnityEngine;

public static class UnusedPublicMemberAnalyzer
{
    private const string MenuItemPath = "Tools/Unused Public Member Analysis/Run";
    private const string AnalyzerScriptPath = "Tools/CI/analyze_csharp_usage.csx";
    private const string SummaryMarkdownName = "csharp_usage_summary.md";
    private const string SummaryJsonName = "csharp_usage_summary.json";

    [MenuItem(MenuItemPath)]
    public static void RunAnalysisFromMenu()
    {
        try
        {
            RunAnalysis();
        }
        catch (Exception exception)
        {
            Debug.LogError($"C# usage analysis failed: {exception.Message}\n{exception}");
        }
    }

    private static void RunAnalysis()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string scriptPath = Path.Combine(projectRoot, AnalyzerScriptPath);
        if (!File.Exists(scriptPath))
        {
            Debug.LogError($"C# usage analyzer script not found at {scriptPath}.");
            return;
        }

        string logDirectory = Path.Combine(projectRoot, "Logs", "asset_audits");
        Directory.CreateDirectory(logDirectory);

        string summaryMarkdownPath = Path.Combine(logDirectory, SummaryMarkdownName);
        string summaryJsonPath = Path.Combine(logDirectory, SummaryJsonName);

        var solutionPath = FindSolution(projectRoot);
        var startInfo = CreateProcessStartInfo(projectRoot, scriptPath, summaryMarkdownPath, summaryJsonPath, solutionPath);

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();

        using (var process = new Process { StartInfo = startInfo })
        {
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    standardOutput.AppendLine(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    standardError.AppendLine(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (standardOutput.Length > 0)
            {
                Debug.Log(standardOutput.ToString());
            }

            if (process.ExitCode != 0)
            {
                Debug.LogError($"C# usage analysis failed (exit code {process.ExitCode}).\n{standardError}");
                return;
            }

            if (standardError.Length > 0)
            {
                Debug.LogWarning($"C# usage analysis reported warnings:\n{standardError}");
            }
        }

        var summary = LoadSummary(summaryJsonPath);
        var summaryPreview = BuildSummaryPreview(summaryMarkdownPath);

        var statusDetails = string.IsNullOrWhiteSpace(summary.StatusMessage)
            ? string.Empty
            : $" Status: {summary.StatusMessage}.";

        Debug.Log(
            $"C# usage analysis complete. Source: {summary.SourceDescription ?? "Unknown"}. " +
            $"Unused members: {summary.UnusedCount}. New unused members: {summary.NewUnusedCount}.{statusDetails} " +
            $"Reports written to {logDirectory}.\n" +
            summaryPreview);
    }

    private static ProcessStartInfo CreateProcessStartInfo(
        string projectRoot,
        string scriptPath,
        string summaryMarkdownPath,
        string summaryJsonPath,
        string? solutionPath)
    {
        var arguments = new StringBuilder();
        arguments.Append(Quote(scriptPath)).Append(' ');
        arguments.Append("--project-root ").Append(Quote(projectRoot)).Append(' ');
        arguments.Append("--summary-markdown ").Append(Quote(summaryMarkdownPath)).Append(' ');
        arguments.Append("--summary-json ").Append(Quote(summaryJsonPath)).Append(' ');

        if (!string.IsNullOrEmpty(solutionPath))
        {
            arguments.Append("--solution ").Append(Quote(solutionPath)).Append(' ');
        }

        string runner = FindAnalyzerRunner();
        bool usesDotnet = runner.Equals("dotnet", StringComparison.OrdinalIgnoreCase);

        return new ProcessStartInfo
        {
            FileName = runner,
            Arguments = usesDotnet ? $"script {arguments}" : arguments.ToString(),
            WorkingDirectory = projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private static string FindAnalyzerRunner()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string dotnetScriptPath = Path.Combine(userProfile, ".dotnet", "tools", "dotnet-script");

        if (File.Exists(dotnetScriptPath))
        {
            return dotnetScriptPath;
        }

        if (File.Exists(dotnetScriptPath + ".exe"))
        {
            return dotnetScriptPath + ".exe";
        }

        return "dotnet";
    }

    private static string? FindSolution(string projectRoot)
    {
        return Directory.EnumerateFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
    }

    private static string BuildSummaryPreview(string summaryMarkdownPath)
    {
        if (!File.Exists(summaryMarkdownPath))
        {
            return "C# usage summary not found; verify dotnet-script is installed and the analyzer ran correctly.";
        }

        var lines = File.ReadLines(summaryMarkdownPath).Take(20);
        return string.Join("\n", lines);
    }

    private static UsageSummary LoadSummary(string summaryJsonPath)
    {
        if (!File.Exists(summaryJsonPath))
        {
            return new UsageSummary();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(summaryJsonPath));
            var root = document.RootElement;

            return new UsageSummary
            {
                SourceDescription = root.TryGetProperty("sourceDescription", out var sourceDescription)
                    ? sourceDescription.GetString()
                    : null,
                Status = root.TryGetProperty("status", out var status)
                    ? status.GetString()
                    : null,
                StatusMessage = root.TryGetProperty("statusMessage", out var statusMessage)
                    ? statusMessage.GetString()
                    : null,
                UnusedCount = root.TryGetProperty("unused_count", out var unusedCount)
                    ? unusedCount.GetInt32()
                    : root.TryGetProperty("unusedCount", out var legacyUnusedCount)
                        ? legacyUnusedCount.GetInt32()
                        : 0,
                NewUnusedCount = root.TryGetProperty("new_unused_count", out var newUnusedCount)
                    ? newUnusedCount.GetInt32()
                    : root.TryGetProperty("newUnusedCount", out var legacyNewUnusedCount)
                        ? legacyNewUnusedCount.GetInt32()
                        : 0
            };
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to parse C# usage summary JSON at {summaryJsonPath}: {exception.Message}");
            return new UsageSummary();
        }
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }

    private class UsageSummary
    {
        public string? SourceDescription;
        public string? Status;
        public string? StatusMessage;
        public int UnusedCount;
        public int NewUnusedCount;
    }
}
#endif
