#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.CodeAnalysis.Workspaces.MSBuild, 4.8.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp.Workspaces, 4.8.0"
#r "nuget: Microsoft.Build.Locator, 1.7.4"

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

#nullable enable

var options = Options.Parse(args);
var analysis = await UsageAnalyzer.RunAsync(options);
OutputWriter.Write(options, analysis);
Environment.Exit(analysis.ExitCode);

internal sealed class Options
{
    private const string DefaultSummaryMarkdown = "Logs/asset_audits/csharp_usage_summary.md";
    private const string DefaultSummaryJson = "Logs/asset_audits/csharp_usage_summary.json";

    public string ProjectRoot { get; init; } = Directory.GetCurrentDirectory();
    public string? SolutionPath { get; init; }
    public string SummaryMarkdown { get; init; } = DefaultSummaryMarkdown;
    public string SummaryJson { get; init; } = DefaultSummaryJson;
    public bool FailOnNewLegacy { get; init; }
    public int MaxListedMembers { get; init; } = 25;
    public IReadOnlyList<string> WhitelistFiles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InlineWhitelist { get; init; } = Array.Empty<string>();
    public string? GithubOutput { get; init; }
    public string? ChangedFilesPath { get; init; }

    public static Options Parse(string[] args)
    {
        var projectRoot = Directory.GetCurrentDirectory();
        string? solutionPath = null;
        string summaryMd = DefaultSummaryMarkdown;
        string summaryJson = DefaultSummaryJson;
        string? githubOutput = null;
        string? changedFiles = null;
        var whitelistFiles = new List<string>();
        var inlineWhitelist = new List<string>();
        var maxListedMembers = 25;
        var failOnNewLegacy = false;

        for (var i = 0; i < args.Length; i++)
        {
            string? NextOrNull()
            {
                if (i + 1 >= args.Length)
                {
                    return null;
                }

                i++;
                return args[i];
            }

            switch (args[i])
            {
                case "--project-root":
                    projectRoot = NextOrNull() ?? projectRoot;
                    break;
                case "--solution":
                    solutionPath = NextOrNull();
                    break;
                case "--summary-markdown":
                    summaryMd = NextOrNull() ?? summaryMd;
                    break;
                case "--summary-json":
                    summaryJson = NextOrNull() ?? summaryJson;
                    break;
                case "--github-output":
                    githubOutput = NextOrNull();
                    break;
                case "--changed-files":
                    changedFiles = NextOrNull();
                    break;
                case "--whitelist":
                    {
                        var value = NextOrNull();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            whitelistFiles.Add(value);
                        }
                    }
                    break;
                case "--whitelist-pattern":
                    {
                        var value = NextOrNull();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            inlineWhitelist.Add(value);
                        }
                    }
                    break;
                case "--max-listed-members":
                    {
                        var value = NextOrNull();
                        if (int.TryParse(value, out var parsed))
                        {
                            maxListedMembers = parsed;
                        }
                    }
                    break;
                case "--fail-on-new-legacy":
                    failOnNewLegacy = true;
                    break;
                default:
                    break;
            }
        }

        return new Options
        {
            ProjectRoot = Path.GetFullPath(projectRoot),
            SolutionPath = solutionPath,
            SummaryMarkdown = summaryMd,
            SummaryJson = summaryJson,
            GithubOutput = githubOutput,
            ChangedFilesPath = changedFiles,
            WhitelistFiles = whitelistFiles,
            InlineWhitelist = inlineWhitelist,
            MaxListedMembers = maxListedMembers,
            FailOnNewLegacy = failOnNewLegacy,
        };
    }
}

internal sealed record AnalysisResult(
    string SourceDescription,
    string Status,
    string StatusMessage,
    IReadOnlyList<MemberRecord> AllUnusedMembers,
    IReadOnlyList<MemberRecord> NewUnusedMembers,
    IReadOnlyList<string> WhitelistPatterns,
    int ExitCode);

internal sealed record MemberRecord(
    string DisplayName,
    string Kind,
    string Reason,
    string FilePath,
    int Line,
    int ReferenceCount);

internal sealed record MemberCandidate(ISymbol Symbol, string FilePath, int Line, string Kind, string Reason);

internal static class UsageAnalyzer
{
    public static async Task<AnalysisResult> RunAsync(Options options)
    {
        try
        {
            var whitelistPatterns = LoadPatterns(options.WhitelistFiles, options.InlineWhitelist);
            var changedFiles = LoadChangedFiles(options.ChangedFilesPath);
            var (workspace, solution, description) = await LoadSolutionAsync(options);
            _ = workspace;
            if (solution == null)
            {
                return new AnalysisResult(description, "skipped", "No solution or C# sources found.", Array.Empty<MemberRecord>(), Array.Empty<MemberRecord>(), whitelistPatterns, 0);
            }

            var candidates = await FindCandidatesAsync(solution);
            if (candidates.Count == 0)
            {
                return new AnalysisResult(description, "ok", "No public or serialized members detected.", Array.Empty<MemberRecord>(), Array.Empty<MemberRecord>(), whitelistPatterns, 0);
            }

            var referenceMap = await FindReferencesAsync(solution, candidates.Select(c => c.Symbol));
            var unused = new List<MemberRecord>();

            foreach (var candidate in candidates)
            {
                var key = SymbolKey.Create(candidate.Symbol).ToString();
                if (!referenceMap.TryGetValue(key, out var locations) || locations.Count == 0)
                {
                    var display = GetDisplayName(candidate.Symbol);
                    if (IsWhitelisted(display, candidate.FilePath, whitelistPatterns))
                    {
                        continue;
                    }

                    var relativePath = NormalizePath(Path.GetRelativePath(options.ProjectRoot, candidate.FilePath));
                    unused.Add(new MemberRecord(
                        display,
                        candidate.Kind,
                        candidate.Reason,
                        relativePath,
                        candidate.Line,
                        0));
                }
            }

            var newUnused = new List<MemberRecord>();
            if (changedFiles.Count > 0)
            {
                foreach (var entry in unused)
                {
                    if (changedFiles.Contains(entry.FilePath))
                    {
                        newUnused.Add(entry);
                    }
                }
            }

            var exitCode = options.FailOnNewLegacy && newUnused.Count > 0 ? 3 : 0;
            return new AnalysisResult(description, "ok", string.Empty, unused, newUnused, whitelistPatterns, exitCode);
        }
        catch (Exception ex)
        {
            return new AnalysisResult("<unknown>", "error", ex.Message, Array.Empty<MemberRecord>(), Array.Empty<MemberRecord>(), Array.Empty<string>(), 1);
        }
    }

    private static async Task<(Workspace Workspace, Solution? Solution, string Description)> LoadSolutionAsync(Options options)
    {
        var explicitPath = NormalizeToExisting(options.SolutionPath);
        if (explicitPath != null)
        {
            var description = NormalizePath(Path.GetRelativePath(options.ProjectRoot, explicitPath));
            return await LoadWorkspaceForPathAsync(explicitPath, description).ConfigureAwait(false);
        }

        var discovered = Directory.EnumerateFiles(options.ProjectRoot, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(options.ProjectRoot, "*.csproj", SearchOption.AllDirectories))
            .OrderBy(path => path.Length)
            .FirstOrDefault();

        if (discovered != null && File.Exists(discovered))
        {
            var description = NormalizePath(Path.GetRelativePath(options.ProjectRoot, discovered));
            return await LoadWorkspaceForPathAsync(discovered, description).ConfigureAwait(false);
        }

        var sourceFiles = DiscoverSourceFiles(options.ProjectRoot).ToList();
        if (sourceFiles.Count == 0)
        {
            return (new AdhocWorkspace(), null, "<no sources>");
        }

        var adhocWorkspace = new AdhocWorkspace();
        var solutionId = SolutionId.CreateNewId("AdhocSolution");
        var projectId = ProjectId.CreateNewId("AdhocProject");
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var references = BuildMetadataReferences();

        var solutionInfo = SolutionInfo.Create(solutionId, VersionStamp.Create());
        adhocWorkspace.AddSolution(solutionInfo);

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "AdhocProject",
            "AdhocAssembly",
            LanguageNames.CSharp,
            metadataReferences: references,
            parseOptions: parseOptions);
        adhocWorkspace.AddProject(projectInfo);

        foreach (var file in sourceFiles)
        {
            var documentId = DocumentId.CreateNewId(projectId);
            var loader = TextLoader.From(TextAndVersion.Create(SourceText.From(File.ReadAllText(file)), VersionStamp.Create(), file));
            adhocWorkspace.AddDocument(DocumentInfo.Create(documentId, Path.GetFileName(file), loader: loader, filePath: file));
        }

        return (adhocWorkspace, adhocWorkspace.CurrentSolution, "<ad-hoc>");
    }

    private static string? NormalizeToExisting(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var full = Path.GetFullPath(path);
        return File.Exists(full) ? full : null;
    }

    private static async Task<(Workspace Workspace, Solution? Solution, string Description)> LoadWorkspaceForPathAsync(string path, string description)
    {
        MSBuildLocator.RegisterDefaults();
        var workspace = MSBuildWorkspace.Create();

        if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(path).ConfigureAwait(false);
            return (workspace, project.Solution, description);
        }

        var solution = await workspace.OpenSolutionAsync(path).ConfigureAwait(false);
        return (workspace, solution, description);
    }

    private static IEnumerable<string> DiscoverSourceFiles(string root)
    {
        var ignoredSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            "Library",
            "Logs",
            "Temp",
            "obj",
            "PackagesCache",
        };

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = NormalizePath(Path.GetRelativePath(root, file));
            var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => ignoredSegments.Contains(segment)))
            {
                continue;
            }

            yield return file;
        }
    }

    private static IReadOnlyList<MetadataReference> BuildMetadataReferences()
    {
        var references = new List<MetadataReference>();
        var trustedAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var assemblyPath in trustedAssemblies)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(assemblyPath));
            }
            catch
            {
            }
        }

        return references;
    }

    private static async Task<List<MemberCandidate>> FindCandidatesAsync(Solution solution)
    {
        var candidates = new List<MemberCandidate>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (!document.SupportsSemanticModel)
                {
                    continue;
                }

                var model = await document.GetSemanticModelAsync().ConfigureAwait(false);
                var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                if (model == null || root == null)
                {
                    continue;
                }

                foreach (var methodNode in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
                {
                    if (model.GetDeclaredSymbol(methodNode) is IMethodSymbol symbol)
                    {
                        if (IsCandidateMethod(symbol) && document.FilePath != null)
                        {
                            candidates.Add(CreateCandidate(symbol, document.FilePath));
                        }
                    }
                }

                foreach (var fieldNode in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax>())
                {
                    if (model.GetDeclaredSymbol(fieldNode) is IFieldSymbol symbol)
                    {
                        if (IsCandidateField(symbol) && document.FilePath != null)
                        {
                            candidates.Add(CreateCandidate(symbol, document.FilePath));
                        }
                    }
                }
            }
        }

        return candidates;
    }

    private static MemberCandidate CreateCandidate(ISymbol symbol, string filePath)
    {
        var lineSpan = symbol.Locations.FirstOrDefault()?.GetLineSpan();
        var line = lineSpan?.StartLinePosition.Line + 1 ?? 0;
        var reason = symbol switch
        {
            IFieldSymbol field when IsSerializedField(field) => "Serialized",
            IFieldSymbol _ => "Public",
            _ => "Public",
        };

        return new MemberCandidate(symbol, NormalizePath(filePath), line, symbol.Kind.ToString(), reason);
    }

    private static bool IsCandidateMethod(IMethodSymbol symbol)
    {
        if (symbol.MethodKind != MethodKind.Ordinary)
        {
            return false;
        }

        if (symbol.IsImplicitlyDeclared || symbol.IsAbstract || symbol.IsOverride || symbol.IsVirtual)
        {
            return false;
        }

        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (symbol.IsAsync && symbol.Name == "Main")
        {
            return false;
        }

        if (IsUnityLifecycleMethod(symbol))
        {
            return false;
        }

        if (HasUnityEntryAttribute(symbol))
        {
            return false;
        }

        return true;
    }

    private static bool IsCandidateField(IFieldSymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
        {
            return false;
        }

        if (symbol.IsConst)
        {
            return false;
        }

        if (symbol.AssociatedSymbol != null)
        {
            return false;
        }

        if (symbol.IsReadOnly && symbol.IsStatic)
        {
            return false;
        }

        if (symbol.DeclaredAccessibility == Accessibility.Public || IsSerializedField(symbol))
        {
            return !HasUnityEntryAttribute(symbol);
        }

        return false;
    }

    private static bool IsSerializedField(IFieldSymbol symbol)
    {
        return symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name is "SerializeField" or "SerializeFieldAttribute" or "SerializeReference" or "SerializeReferenceAttribute");
    }

    private static bool IsUnityLifecycleMethod(IMethodSymbol symbol)
    {
        var knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Awake",
            "Start",
            "Update",
            "FixedUpdate",
            "LateUpdate",
            "OnEnable",
            "OnDisable",
            "OnDestroy",
            "OnGUI",
            "Reset",
            "OnValidate",
            "OnApplicationQuit",
            "OnApplicationFocus",
            "OnApplicationPause",
            "OnCollisionEnter",
            "OnCollisionExit",
            "OnCollisionStay",
            "OnTriggerEnter",
            "OnTriggerExit",
            "OnTriggerStay",
        };

        if (knownNames.Contains(symbol.Name))
        {
            return true;
        }

        return symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name is "RuntimeInitializeOnLoadMethodAttribute" or "InitializeOnLoadMethodAttribute");
    }

    private static bool HasUnityEntryAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name is "ContextMenu" or "ContextMenuAttribute" or "RuntimeInitializeOnLoadMethodAttribute" or "InitializeOnLoadMethodAttribute" or "MenuItemAttribute");
    }

    private static string GetDisplayName(ISymbol symbol)
    {
        var format = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        return symbol.ToDisplayString(format);
    }

    private static async Task<Dictionary<string, List<Location>>> FindReferencesAsync(Solution solution, IEnumerable<ISymbol> symbols)
    {
        var result = new Dictionary<string, List<Location>>();
        var referenced = await SymbolFinder.FindReferencesAsync(symbols, solution).ConfigureAwait(false);
        foreach (var reference in referenced)
        {
            var key = SymbolKey.Create(reference.Definition).ToString();
            if (!result.TryGetValue(key, out var list))
            {
                list = new List<Location>();
                result[key] = list;
            }

            foreach (var location in reference.Locations)
            {
                if (!location.IsImplicit)
                {
                    list.Add(location.Location);
                }
            }
        }

        return result;
    }

    private static List<string> LoadPatterns(IEnumerable<string> files, IEnumerable<string> inlinePatterns)
    {
        var patterns = new List<string>();
        foreach (var pattern in inlinePatterns)
        {
            var normalized = pattern.Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                patterns.Add(normalized);
            }
        }

        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            foreach (var rawLine in File.ReadAllLines(file))
            {
                var line = rawLine.Trim();
                if (!string.IsNullOrEmpty(line) && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    patterns.Add(line);
                }
            }
        }

        return patterns;
    }

    private static HashSet<string> LoadChangedFiles(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var normalized = NormalizePath(line.Trim());
            if (!string.IsNullOrEmpty(normalized))
            {
                set.Add(normalized);
            }
        }

        return set;
    }

    private static bool IsWhitelisted(string displayName, string filePath, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, displayName, ignoreCase: true) || FileSystemName.MatchesSimpleExpression(pattern, NormalizePath(filePath), ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

internal static class OutputWriter
{
    public static void Write(Options options, AnalysisResult analysis)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.SummaryMarkdown) ?? ".");
        Directory.CreateDirectory(Path.GetDirectoryName(options.SummaryJson) ?? ".");

        var summary = new
        {
            analysis.SourceDescription,
            analysis.Status,
            analysis.StatusMessage,
            unusedCount = analysis.AllUnusedMembers.Count,
            newUnusedCount = analysis.NewUnusedMembers.Count,
            whitelistedPatterns = analysis.WhitelistPatterns,
            listedMembers = analysis.AllUnusedMembers.Take(options.MaxListedMembers).ToArray(),
        };

        File.WriteAllText(options.SummaryJson, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));

        var builder = new StringBuilder();
        builder.AppendLine("## C# Usage Audit");
        builder.AppendLine($"- Source: {analysis.SourceDescription}");
        builder.AppendLine($"- Unreferenced public/serialized members: {analysis.AllUnusedMembers.Count}");
        builder.AppendLine($"- New unused members in this change: {analysis.NewUnusedMembers.Count}");

        if (!string.IsNullOrWhiteSpace(analysis.StatusMessage))
        {
            builder.AppendLine($"- Status: {analysis.StatusMessage}");
        }

        if (analysis.WhitelistPatterns.Count > 0)
        {
            builder.AppendLine("- Whitelisted patterns: " + string.Join(", ", analysis.WhitelistPatterns));
        }

        builder.AppendLine();

        if (analysis.AllUnusedMembers.Count == 0)
        {
            builder.AppendLine("No unused public or serialized members were detected.");
        }
        else
        {
            builder.AppendLine("Unused members:");
            foreach (var member in analysis.AllUnusedMembers.Take(options.MaxListedMembers))
            {
                builder.AppendLine($"- `{member.DisplayName}` ({member.Kind}, {member.Reason}) in {member.FilePath}:{member.Line}");
            }

            if (analysis.AllUnusedMembers.Count > options.MaxListedMembers)
            {
                builder.AppendLine($"- ...and {analysis.AllUnusedMembers.Count - options.MaxListedMembers} more");
            }
        }

        builder.AppendLine();

        if (analysis.NewUnusedMembers.Count == 0)
        {
            builder.AppendLine("No new unused members detected in touched files.");
        }
        else
        {
            builder.AppendLine("New unused members in this change:");
            foreach (var member in analysis.NewUnusedMembers.Take(options.MaxListedMembers))
            {
                builder.AppendLine($"- `{member.DisplayName}` ({member.Kind}, {member.Reason}) in {member.FilePath}:{member.Line}");
            }

            if (analysis.NewUnusedMembers.Count > options.MaxListedMembers)
            {
                builder.AppendLine($"- ...and {analysis.NewUnusedMembers.Count - options.MaxListedMembers} more");
            }
        }

        File.WriteAllText(options.SummaryMarkdown, builder.ToString());

        var outputs = new Dictionary<string, string>
        {
            ["summary_markdown"] = options.SummaryMarkdown,
            ["summary_json"] = options.SummaryJson,
            ["unused_count"] = analysis.AllUnusedMembers.Count.ToString(),
            ["new_unused_count"] = analysis.NewUnusedMembers.Count.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(options.GithubOutput))
        {
            using var handle = File.AppendText(options.GithubOutput);
            foreach (var kvp in outputs)
            {
                handle.WriteLine($"{kvp.Key}={kvp.Value}");
            }
        }
    }
}
