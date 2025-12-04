#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        if (string.IsNullOrEmpty(solutionPath))
        {
            Debug.LogWarning("C# usage analysis canceled: no solution selected.");
            return;
        }
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

    private static string? FindSolutionOrProject(string projectRoot)
    {
        var solutions = Directory
            .EnumerateFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly)
            .ToList();

        if (solutions.Count == 1)
        {
            return solutions[0];
        }

        string promptTitle = "Select solution for C# usage analysis";
        string promptMessage = solutions.Count == 0
            ? "No solution found automatically; please choose one."
            : $"Multiple solutions found ({solutions.Count}); please choose one.";

        Debug.Log(promptMessage);

        string chosenSolution = EditorUtility.OpenFilePanel(promptTitle, projectRoot, "sln");
        return string.IsNullOrEmpty(chosenSolution) ? null : chosenSolution;
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
            var json = File.ReadAllText(summaryJsonPath);
            var parsed = JsonUtility.FromJson<UsageSummaryData>(json) ?? new UsageSummaryData();

            return new UsageSummary
            {
                SourceDescription = FirstNonEmpty(parsed.sourceDescription, parsed.SourceDescription),
                Status = FirstNonEmpty(parsed.status, parsed.Status),
                StatusMessage = FirstNonEmpty(parsed.statusMessage, parsed.StatusMessage),
                UnusedCount = parsed.unused_count != 0 ? parsed.unused_count : parsed.unusedCount,
                NewUnusedCount = parsed.new_unused_count != 0 ? parsed.new_unused_count : parsed.newUnusedCount
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

    [Serializable]
    private class UsageSummaryData
    {
        public string? sourceDescription;
        public string? SourceDescription;
        public string? status;
        public string? Status;
        public string? statusMessage;
        public string? StatusMessage;
        public int unused_count;
        public int unusedCount;
        public int new_unused_count;
        public int newUnusedCount;
    }

    private static string? FirstNonEmpty(string? first, string? second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }
}
#endif
