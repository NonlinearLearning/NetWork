using System.Reflection;
using Terraria.NetWork.Core.Protocol;

namespace Terraria.NetWork.Verification.ProtocolDemoStandalone;

internal static class SubmissionDependencyGuardTests
{
    private static readonly string[] PureWireSourceFiles =
    [
        "Prototypes/external-context-wire-submission/src/Protocol/Packets/PlayerControlsPacket13Submission.cs",
        "Prototypes/external-context-wire-submission/src/Protocol/Packets/PlayerControlsPacket13SubmissionEncoder.cs",
        "Prototypes/external-context-wire-submission/src/Protocol/Packets/AreaTileChangePacket20Submission.cs",
        "Prototypes/external-context-wire-submission/src/Protocol/Packets/AreaTileChangePacket20SubmissionEncoder.cs"
    ];

    private static readonly string[] SubmissionTypes =
    [
        "Terraria.NetWork.Core.Protocol.PlayerControlsPacket13Submission",
        "Terraria.NetWork.Core.Protocol.PreparedTile20",
        "Terraria.NetWork.Core.Protocol.PreparedPacket20"
    ];

    private static readonly string[] ForbiddenTokens =
    [
        "Main",
        "World",
        "Netplay",
        "SessionContext",
        "IServerSession",
        "SendTarget",
        "SendPipelineContext",
        "MessageTcpSession",
        "IBufferWriter",
        "cursor",
        "writer",
        "routing",
        "Func<",
        "Lazy<"
    ];

    public static void Run()
    {
        VerifyPureWireSourceDependencies();
        VerifySubmissionMemberTypes();
        VerifyTargetFilesStayInProtocolAssembly();
    }

    private static void VerifyPureWireSourceDependencies()
    {
        var root = FindWorkspaceRoot();
        var violations = new List<string>();

        foreach (var relativePath in PureWireSourceFiles)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                violations.Add($"missing source file: {relativePath}");
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var code = StripLineComment(lines[lineIndex]);
                foreach (var token in ForbiddenTokens)
                {
                    if (ContainsToken(code, token))
                    {
                        violations.Add($"{relativePath}:{lineIndex + 1} contains forbidden token '{token}'");
                    }
                }
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Submission dependency guard failed:\n" + string.Join('\n', violations));
        }
    }

    private static void VerifySubmissionMemberTypes()
    {
        var violations = new List<string>();
        foreach (var typeName in SubmissionTypes)
        {
            var type = typeof(BitsByte).Assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Guard target type is missing: {typeName}");

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    CheckMemberType(parameter.ParameterType, $"{typeName} constructor {parameter.Name}", violations);
                }
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                CheckMemberType(property.PropertyType, $"{typeName}.{property.Name}", violations);
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                CheckMemberType(field.FieldType, $"{typeName}.{field.Name}", violations);
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Submission member dependency guard failed:\n" + string.Join('\n', violations));
        }
    }

    private static void VerifyTargetFilesStayInProtocolAssembly()
    {
        var assembly = typeof(PlayerControlsPacket13Submission).Assembly;
        foreach (var typeName in SubmissionTypes)
        {
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Submission type is not in the production assembly: {typeName}");
            if (type.Namespace != "Terraria.NetWork.Core.Protocol")
            {
                throw new InvalidOperationException(
                    $"Submission type escaped the protocol boundary: {type.FullName}");
            }
        }
    }

    private static void CheckMemberType(Type memberType, string member, ICollection<string> violations)
    {
        if (memberType == typeof(string) || memberType.IsPrimitive || memberType.IsEnum)
        {
            return;
        }

        var text = memberType.FullName ?? memberType.Name;
        foreach (var token in ForbiddenTokens)
        {
            if (ContainsToken(text, token))
            {
                violations.Add($"{member} has forbidden type '{text}'");
            }
        }
    }

    private static string FindWorkspaceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Prototypes")) &&
                File.Exists(Path.Combine(directory.FullName, "README.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the workspace root for dependency guard.");
    }

    private static string StripLineComment(string line)
    {
        var commentStart = line.IndexOf("//", StringComparison.Ordinal);
        return commentStart < 0 ? line : line[..commentStart];
    }

    private static bool ContainsToken(string text, string token)
    {
        if (token.Contains('<'))
        {
            return text.Contains(token, StringComparison.Ordinal);
        }

        var index = text.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            var beforeIsIdentifier = index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_');
            var afterIndex = index + token.Length;
            var afterIsIdentifier = afterIndex < text.Length && (char.IsLetterOrDigit(text[afterIndex]) || text[afterIndex] == '_');
            if (!beforeIsIdentifier && !afterIsIdentifier)
            {
                return true;
            }

            var nextStart = afterIndex < text.Length ? afterIndex : text.Length;
            index = text.IndexOf(token, nextStart, StringComparison.Ordinal);
        }

        return false;
    }
}
