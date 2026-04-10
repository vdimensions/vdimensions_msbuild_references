using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

var directories = new List<string>();
var referenceDefs = new List<string>();
// TODO: parse from args
var options = new Options();

RecursivelyCollectDirectoryStructure(Directory.GetCurrentDirectory(), directories);
for (int i = 0, c = directories.Count; i < c; i++)
{
    referenceDefs.AddRange(Directory.GetFiles(directories[i], "*.versions.json"));
}
for (int i = 0, c = referenceDefs.Count; i < c; i++)
{
    ExpandDefinition(referenceDefs[i], options);
}

return;

#region Functions

static void RecursivelyCollectDirectoryStructure(string directory, ICollection<string> directories)
{
    directories.Add(directory);
    foreach (var d in Directory.GetDirectories(directory))
    {
        RecursivelyCollectDirectoryStructure(d, directories);
    }
}

static void ExpandDefinition(string definitionFile, Options options)
{
    try
    {
        using var inputStream = File.OpenRead(definitionFile);
        // var versionDefinitions = (VersionDefinition)serializer.ReadObject(stream);
        var outerDictionary = JsonDocument.Parse(inputStream).RootElement.EnumerateObject().ToDictionary(
            property => property.Name.Trim(),
            property => property.Value.EnumerateObject().ToDictionary(
                p => p.Name.Trim(),
                p => p.Value.ToString().Trim(),
                StringComparer.Ordinal
            ),
            StringComparer.OrdinalIgnoreCase
        );
        var versionDefinitions = new VersionDefinition(
            outerDictionary[nameof(VersionDefinition.Min)],
            outerDictionary[nameof(VersionDefinition.Max)]
        );
        var referenceDefinition = ReferenceDefinition.Create(
            Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(definitionFile)),
            versionDefinitions
        );
        var targetOuptuFile = Path.Combine(Path.GetDirectoryName(definitionFile)!, $"{referenceDefinition.Name}.targets");
        using var outputStream = File.Open(targetOuptuFile, FileMode.Create);
        WriteTargetFile(options, referenceDefinition, outputStream);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"> An error occurred: {ex.Message}\n{ex.StackTrace}");
    }
}

static void WriteTargetFile(Options options, ReferenceDefinition referenceDefinition, Stream outputStream)
{
    using var writer = new StreamWriter(outputStream, Encoding.UTF8);
    writer.WriteLine("<Project>");

    if (referenceDefinition.TargetFrameworkDefinitions.Count > 0)
    {
        writer.WriteLine("  <Choose>");
        foreach (var targetFrameworkDefinition in referenceDefinition.TargetFrameworkDefinitions)
        {
            writer.WriteLine("  <When Condition=\" '$(TargetFramework)' == '{0}' \">", targetFrameworkDefinition.TargetFramework);
            WritePackageVersion(options, referenceDefinition, targetFrameworkDefinition, writer);
            writer.WriteLine("  </When>");
        }
        if (referenceDefinition.TargetFrameworkDefinitionFallback != null)
        {
            writer.WriteLine("  <Otherwise>");
            WritePackageVersion(options, referenceDefinition, referenceDefinition.TargetFrameworkDefinitionFallback, writer);
            writer.WriteLine("  </Otherwise>");
        }
        writer.WriteLine("  </Choose>");
    }
    else if (referenceDefinition.TargetFrameworkDefinitionFallback != null)
    {
        WritePackageVersion(options, referenceDefinition, referenceDefinition.TargetFrameworkDefinitionFallback, writer);
    }

    writer.WriteLine("</Project>");
    writer.Flush();
}

static void WritePackageVersion(Options options, ReferenceDefinition referenceDefinition, TargetFrameworkDefinition targetFrameworkDefinition, TextWriter writer)
{
    WritePackageVersionItemGroup(referenceDefinition, targetFrameworkDefinition, writer);
    if (!options.OnlyEmitManagePackageVersionsCentrally)
    {
        WritePackageReferenceItemGroup(referenceDefinition, targetFrameworkDefinition, writer);
    }
}

static void WritePackageVersionItemGroup(ReferenceDefinition referenceDefinition, TargetFrameworkDefinition targetFrameworkDefinition, TextWriter writer)
{
    writer.WriteLine("    <ItemGroup Condition=\" '$(ManagePackageVersionsCentrally)' == 'true' \">");
    if (targetFrameworkDefinition.VersionRange != null)
    {
        if (StringComparer.Ordinal.Equals(targetFrameworkDefinition.VersionRange.MaxVersion, targetFrameworkDefinition.VersionRange.MinVersion))
        {
            writer.WriteLine("      <PackageVersion Include=\"{0}\" Version=\"{1}\" />", referenceDefinition.Name, targetFrameworkDefinition.VersionRange.MaxVersion);
        }
        else
        {
            writer.WriteLine("      <PackageVersion Include=\"{0}\" Version=\"{1}\" Condition=\" '$(OutputType)' != 'Library' \" />", referenceDefinition.Name, targetFrameworkDefinition.VersionRange.MaxVersion);
            writer.WriteLine("      <PackageVersion Include=\"{0}\" Version=\"{1}\" Condition=\" '$(OutputType)' == 'Library' \" />", referenceDefinition.Name, targetFrameworkDefinition.VersionRange.MinVersion);
        }
    }
    else
    {
        writer.WriteLine("      <PackageVersion Remove=\"{0}\" />", referenceDefinition.Name);
    }
    writer.WriteLine("    </ItemGroup>");
}

static void WritePackageReferenceItemGroup(ReferenceDefinition referenceDefinition, TargetFrameworkDefinition targetFrameworkDefinition, TextWriter writer)
{
    writer.WriteLine("    <ItemGroup Condition=\" '$(ManagePackageVersionsCentrally)' != 'true' \">");
    if (targetFrameworkDefinition.VersionRange != null)
    {
        if (StringComparer.Ordinal.Equals(targetFrameworkDefinition.VersionRange.MaxVersion, targetFrameworkDefinition.VersionRange.MinVersion))
        {
            writer.WriteLine("      <PackageReference Update=\"{0}\" Version=\"{1}\" />", referenceDefinition.Name, targetFrameworkDefinition.VersionRange.MaxVersion);
        }
        else
        {
            writer.WriteLine("      <PackageReference Update=\"{0}\" Version=\"{1}\" Condition=\" '$(OutputType)' != 'Library' \" />", referenceDefinition.Name, targetFrameworkDefinition.VersionRange.MaxVersion);
            writer.WriteLine("      <PackageReference Update=\"{0}\" Version=\"{1}\" Condition=\" '$(OutputType)' == 'Library' \" />", referenceDefinition.Name, targetFrameworkDefinition.VersionRange.MinVersion);
        }
    }
    else
    {
        writer.WriteLine("      <PackageReference Remove=\"{0}\" />", referenceDefinition.Name);
    }
    writer.WriteLine("    </ItemGroup>");
}
#endregion

#region Classes
public sealed class Options
{
    public bool OnlyEmitManagePackageVersionsCentrally { get; set; } = false;
    public string OutputTargetsFile { get; set; } = null;
}

public sealed class VersionDefinition
{
    public VersionDefinition(Dictionary<string, string> min, Dictionary<string, string> max)
    {
        Min = min;
        Max = max;
    }

    public Dictionary<string, string> Min { get;}

    public Dictionary<string, string> Max { get; }
}

public sealed class VersionRange
{
    public VersionRange(string minVersion, string maxVersion)
    {
        MinVersion = minVersion;
        MaxVersion = maxVersion;
    }

    public string MinVersion { get; }
    public string MaxVersion { get; }
}

public sealed class TargetFrameworkDefinition
{
    public TargetFrameworkDefinition(string targetFramework, VersionRange? versionRange)
    {
        TargetFramework = targetFramework;
        VersionRange = versionRange;
    }

    public string TargetFramework { get; }
    public VersionRange? VersionRange { get; }
}

public sealed class ReferenceDefinition
{
    private static readonly string[][] DotnetVersionGroups =
    [
        [
            "net10.0", "net9.0", "net8.0", "net7.0", "net6.0", "net5.0"
        ],
        [
            "netcoreapp3.1", "netcoreapp3.0",
            "netcoreapp2.2", "netcoreapp2.1","netcoreapp2.0",
            "netcoreapp1.1", "netcoreapp1.0"
        ],
        [
            "netstandard2.1", "netstandard2.0",
            "netstandard1.6", "netstandard1.5", "netstandard1.4", "netstandard1.3", "netstandard1.2", "netstandard1.1", "netstandard1.0"
        ],
        [
            "net481", "net48",
            "net472", "net471", "net47",
            "net463", "net462", "net461", "net46",
            "net452", "net451", "net45",
            "net403", "net40",
            "net35",
            "net20"
        ],
        [ string.Empty ] // fallback group
    ];


    public static ReferenceDefinition Create(string name, VersionDefinition versionDefinitions)
    {
        var targetFrameworkDefinitions = new List<TargetFrameworkDefinition>(
            Math.Max(versionDefinitions.Max.Count, versionDefinitions.Min.Count)
        );
        TargetFrameworkDefinition fallbackDefinition = null;
        var totalNonFallbacCount = 0;

        foreach (var versionGroup in DotnetVersionGroups)
        {
            if (versionGroup.Length == 0)
            {
                continue;
            }
            for (int i = 0, c = versionGroup.Length; i < c; i++)
            {
                string? minVersion, maxVersion;
                var tfm = versionGroup[i];
                if (!versionDefinitions.Min.TryGetValue(tfm, out minVersion))
                {
                    for (var j = i + 1; j < c; j++)
                    {
                        if (versionDefinitions.Min.TryGetValue(versionGroup[j], out minVersion))
                        {
                            break;
                        }
                    }
                }
                if (!versionDefinitions.Max.TryGetValue(tfm, out maxVersion))
                {
                    for (var j = i + 1; j < c; j++)
                    {
                        if (versionDefinitions.Max.TryGetValue(versionGroup[j], out maxVersion))
                        {
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(maxVersion))
                    {
                        maxVersion = minVersion;
                    }
                }

                if (string.IsNullOrEmpty(minVersion) || string.IsNullOrEmpty(maxVersion))
                {
                    if (string.IsNullOrEmpty(tfm))
                    {
                        Console.WriteLine($"> [FAIL] {name} tfm=FALLBACK, minVersion={minVersion}, maxVersion={maxVersion}");
                    }
                    else
                    {
                        Console.WriteLine($"> [FAIL] {name} tfm={tfm}, minVersion={minVersion}, maxVersion={maxVersion}");
                        targetFrameworkDefinitions.Add(new TargetFrameworkDefinition(tfm, null));
                    }
                }
                else
                {
                    var versionRange = new VersionRange(minVersion, maxVersion);
                    if (string.IsNullOrEmpty(tfm))
                    {
                        Console.WriteLine($"> [OK] {name} tfm=FALLBACK, minVersion={minVersion}, maxVersion={maxVersion}");
                        fallbackDefinition = new TargetFrameworkDefinition(tfm, versionRange);
                    }
                    else
                    {

                        totalNonFallbacCount++;
                        Console.WriteLine($"> [OK] {name} tfm={tfm}, minVersion={minVersion}, maxVersion={maxVersion}");
                        targetFrameworkDefinitions.Add(new TargetFrameworkDefinition(tfm, versionRange));
                    }
                }
            }
        }

        if (totalNonFallbacCount == 0)
        {
            targetFrameworkDefinitions.Clear();
        }

        return new ReferenceDefinition(name, targetFrameworkDefinitions, fallbackDefinition);
    }

    private ReferenceDefinition(string name, IReadOnlyList<TargetFrameworkDefinition> targetFrameworkDefinitions, TargetFrameworkDefinition? targetFrameworkDefinitionFallback = null)
    {
        Name = name;
        TargetFrameworkDefinitions = targetFrameworkDefinitions;
        TargetFrameworkDefinitionFallback = targetFrameworkDefinitionFallback;
    }

    public string Name { get; }
    public IReadOnlyList<TargetFrameworkDefinition> TargetFrameworkDefinitions { get; }
    public TargetFrameworkDefinition? TargetFrameworkDefinitionFallback { get; }
}
#endregion