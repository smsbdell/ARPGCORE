using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AssetUsageAuditor
{
    private static readonly string[] IgnoredPathFragments =
    {
        "/Editor/",
        "/Tests/",
        "/Gizmos/"
    };

    private static readonly string[] WhitelistedGuids = Array.Empty<string>();

    private const string MenuItemPath = "Tools/Asset Usage Audit/Run Audit";

    [MenuItem(MenuItemPath)]
    public static void RunAuditFromMenu()
    {
        RunAudit();
    }

    /// <summary>
    /// Entry point for running the audit via batchmode using -executeMethod AssetUsageAuditor.RunAuditFromBatch.
    /// </summary>
    public static void RunAuditFromBatch()
    {
        RunAudit();
    }

    private static void RunAudit()
    {
        var results = AnalyzeAssets();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var logDirectory = PrepareLogDirectory();

        var textReportPath = Path.Combine(logDirectory, $"asset_audit_{timestamp}.txt");
        var jsonReportPath = Path.Combine(logDirectory, $"asset_audit_{timestamp}.json");
        var csvReportPath = Path.Combine(logDirectory, $"asset_audit_{timestamp}.csv");

        File.WriteAllText(textReportPath, BuildTextReport(results));
        File.WriteAllText(jsonReportPath, BuildJsonReport(results));
        File.WriteAllText(csvReportPath, BuildCsvReport(results));

        Debug.Log($"Asset usage audit complete. Zero-reference assets: {results.ZeroReferenceAssets.Count}. Reports written to {logDirectory}");
    }

    private static string PrepareLogDirectory()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var logDirectory = Path.Combine(projectRoot, "Logs", "asset_audits");
        Directory.CreateDirectory(logDirectory);
        return logDirectory;
    }

    private static AuditResults AnalyzeAssets()
    {
        var allPaths = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsIgnored(path))
            .ToList();

        var assets = new Dictionary<string, AssetRecord>();

        foreach (var path in allPaths)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                continue;
            }

            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            assets[guid] = new AssetRecord
            {
                Guid = guid,
                Path = path,
                AssetTypeName = type?.Name ?? "Unknown",
                Category = CategorizeAsset(type),
                ReferencedBy = new HashSet<string>()
            };
        }

        foreach (var asset in assets.Values)
        {
            var dependencies = AssetDatabase.GetDependencies(asset.Path, true);
            foreach (var dependencyPath in dependencies)
            {
                if (dependencyPath == asset.Path || dependencyPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsIgnored(dependencyPath) || dependencyPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!dependencyPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dependencyGuid = AssetDatabase.AssetPathToGUID(dependencyPath);
                if (string.IsNullOrEmpty(dependencyGuid))
                {
                    continue;
                }

                if (assets.TryGetValue(dependencyGuid, out var dependencyRecord))
                {
                    dependencyRecord.ReferencedBy.Add(asset.Guid);
                }
            }
        }

        var zeroReferenceAssets = assets.Values
            .Where(a => a.ReferencedBy.Count == 0)
            .Where(a => !WhitelistedGuids.Contains(a.Guid))
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Path)
            .ToList();

        return new AuditResults
        {
            Assets = assets,
            ZeroReferenceAssets = zeroReferenceAssets
        };
    }

    private static string CategorizeAsset(Type type)
    {
        if (type == null)
        {
            return "Data";
        }

        if (typeof(MonoScript).IsAssignableFrom(type))
        {
            return "Scripts";
        }

        if (typeof(ScriptableObject).IsAssignableFrom(type) && !typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            return "ScriptableObjects";
        }

        if (typeof(Material).IsAssignableFrom(type) || typeof(Shader).IsAssignableFrom(type) || typeof(Texture).IsAssignableFrom(type))
        {
            return "Textures/Materials";
        }

        if (typeof(AnimationClip).IsAssignableFrom(type) || typeof(RuntimeAnimatorController).IsAssignableFrom(type))
        {
            return "Animations";
        }

        if (typeof(AudioClip).IsAssignableFrom(type))
        {
            return "Audio";
        }

        return "Data";
    }

    private static bool IsIgnored(string path)
    {
        return IgnoredPathFragments.Any(fragment => path.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string BuildTextReport(AuditResults results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Asset Usage Audit - {DateTime.UtcNow:u}");
        sb.AppendLine("Ignored path fragments: " + string.Join(", ", IgnoredPathFragments));
        sb.AppendLine("Whitelisted GUIDs: " + (WhitelistedGuids.Length == 0 ? "(none)" : string.Join(", ", WhitelistedGuids)));
        sb.AppendLine();

        var groupedZeroRefs = results.ZeroReferenceAssets
            .GroupBy(a => a.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groupedZeroRefs)
        {
            sb.AppendLine($"== {group.Key} ==");
            foreach (var asset in group)
            {
                sb.AppendLine($"- {asset.Path} ({asset.Guid}) [{asset.AssetTypeName}]");
            }

            if (!group.Any())
            {
                sb.AppendLine("(none)");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"Total assets scanned: {results.Assets.Count}");
        sb.AppendLine($"Zero-reference assets: {results.ZeroReferenceAssets.Count}");
        return sb.ToString();
    }

    private static string BuildJsonReport(AuditResults results)
    {
        var jsonPayload = new AuditJson
        {
            generatedAtUtc = DateTime.UtcNow.ToString("o"),
            ignoredPathFragments = IgnoredPathFragments,
            whitelistedGuids = WhitelistedGuids,
            assets = results.Assets.Values.Select(ToJsonRecord).OrderBy(r => r.path).ToArray(),
            zeroReferenceAssets = results.ZeroReferenceAssets.Select(ToJsonRecord).ToArray()
        };

        return JsonUtility.ToJson(jsonPayload, true);
    }

    private static AuditJsonRecord ToJsonRecord(AssetRecord record)
    {
        return new AuditJsonRecord
        {
            guid = record.Guid,
            path = record.Path,
            category = record.Category,
            type = record.AssetTypeName,
            referenceCount = record.ReferencedBy.Count,
            referencedBy = record.ReferencedBy.ToArray()
        };
    }

    private static string BuildCsvReport(AuditResults results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Category,GUID,Path,Type,ReferenceCount,ReferencedByGUIDs");
        foreach (var asset in results.Assets.Values.OrderBy(a => a.Path))
        {
            var escapedPath = asset.Path.Replace("\"", "\"\"");
            var referencedBy = asset.ReferencedBy.Count == 0 ? string.Empty : string.Join(";", asset.ReferencedBy.OrderBy(g => g));
            sb.AppendLine($"\"{asset.Category}\",\"{asset.Guid}\",\"{escapedPath}\",\"{asset.AssetTypeName}\",{asset.ReferencedBy.Count},\"{referencedBy}\"");
        }

        return sb.ToString();
    }

    private class AssetRecord
    {
        public string Guid;
        public string Path;
        public string Category;
        public string AssetTypeName;
        public HashSet<string> ReferencedBy;
    }

    private class AuditResults
    {
        public Dictionary<string, AssetRecord> Assets;
        public List<AssetRecord> ZeroReferenceAssets;
    }

    [Serializable]
    private class AuditJson
    {
        public string generatedAtUtc;
        public string[] ignoredPathFragments;
        public string[] whitelistedGuids;
        public AuditJsonRecord[] assets;
        public AuditJsonRecord[] zeroReferenceAssets;
    }

    [Serializable]
    private class AuditJsonRecord
    {
        public string guid;
        public string path;
        public string category;
        public string type;
        public int referenceCount;
        public string[] referencedBy;
    }
}
