using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Terraria.NetWork.Generators;

[Generator]
public sealed class ConceptPacketCodecGenerator : IIncrementalGenerator
{
    private const string SourceAttributeMetadataName = "Terraria.NetWork.Core.Protocol.ConceptPacketSourceAttribute";
    private const string FieldAttributeMetadataName = "Terraria.NetWork.Core.Protocol.ConceptPacketFieldAttribute";
    private const string BitAttributeMetadataName = "Terraria.NetWork.Core.Protocol.ConceptBitAttribute";
    private const string ConditionAttributeMetadataName = "Terraria.NetWork.Core.Protocol.ConceptFlagConditionAttribute";
    private const string BitsByteMetadataName = "Terraria.NetWork.Core.Protocol.BitsByte";
    private const string Vector2MetadataName = "System.Numerics.Vector2";
    private const string PacketFieldKindMetadataName = "Terraria.NetWork.Core.Protocol.PacketFieldKind";

    private static readonly DiagnosticDescriptor InvalidSpecDescriptor = new(
        id: "CPG001",
        title: "Invalid concept packet specification",
        messageFormat: "{0}",
        category: "ConceptPacketGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedTypeDescriptor = new(
        id: "CPG002",
        title: "Unsupported concept packet field type",
        messageFormat: "Field '{0}' uses unsupported type '{1}'",
        category: "ConceptPacketGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var specs = context.SyntaxProvider.ForAttributeWithMetadataName(
            SourceAttributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (generatorContext, _) => CreateModel(generatorContext))
            .Where(static model => model is not null);

        context.RegisterSourceOutput(specs, static (productionContext, model) =>
        {
            if (model is null)
            {
                return;
            }

            foreach (var diagnostic in model.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }

            if (model.Diagnostics.Length > 0)
            {
                return;
            }

            productionContext.AddSource(
                $"{model.CodecTypeName}.g.cs",
                SourceText.From(GenerateSource(model), Encoding.UTF8));
        });
    }

    private static PacketSpecModel? CreateModel(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol specType)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var attribute = context.Attributes[0];
        if (attribute.ConstructorArguments.Length != 4)
        {
            diagnostics.Add(CreateInvalidSpecDiagnostic(specType, "ConceptPacketSourceAttribute must have four constructor arguments."));
            return new PacketSpecModel(specType.Name, specType.ContainingNamespace.ToDisplayString(), "Invalid", "Invalid", "global::System.Object", 0, [], diagnostics.ToImmutable());
        }

        var runtimePacketType = attribute.ConstructorArguments[1].Value as INamedTypeSymbol;
        var codecTypeName = attribute.ConstructorArguments[2].Value as string;
        var definitionTypeName = attribute.ConstructorArguments[3].Value as string;
        if (runtimePacketType is null || string.IsNullOrWhiteSpace(codecTypeName) || string.IsNullOrWhiteSpace(definitionTypeName))
        {
            diagnostics.Add(CreateInvalidSpecDiagnostic(specType, "ConceptPacketSourceAttribute arguments must include a runtime packet type, codec type name, and definition type name."));
            return new PacketSpecModel(specType.Name, specType.ContainingNamespace.ToDisplayString(), "Invalid", "Invalid", "global::System.Object", 0, [], diagnostics.ToImmutable());
        }

        var fields = ImmutableArray.CreateBuilder<FieldModel>();
        foreach (var property in specType.GetMembers().OfType<IPropertySymbol>())
        {
            var fieldAttribute = property.GetAttributes().FirstOrDefault(static attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == FieldAttributeMetadataName);
            if (fieldAttribute is null)
            {
                continue;
            }

            if (fieldAttribute.ConstructorArguments.Length == 0)
            {
                diagnostics.Add(CreateInvalidSpecDiagnostic(property, $"Field '{property.Name}' is missing an order."));
                continue;
            }

            var typeInfo = TypeModel.Create(property.Type);
            if (!typeInfo.IsSupported)
            {
                diagnostics.Add(CreateUnsupportedTypeDiagnostic(property, property.Name, property.Type.ToDisplayString()));
                continue;
            }

            var bits = property.GetAttributes()
                .Where(static attributeData => attributeData.AttributeClass?.ToDisplayString() == BitAttributeMetadataName)
                .Select(static attributeData => new BitModel(
                    (int)attributeData.ConstructorArguments[0].Value!,
                    (string)attributeData.ConstructorArguments[1].Value!))
                .OrderBy(static bit => bit.Index)
                .ToImmutableArray();

            ConditionModel? condition = null;
            var conditionAttribute = property.GetAttributes().FirstOrDefault(static attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == ConditionAttributeMetadataName);
            if (conditionAttribute is not null)
            {
                condition = new ConditionModel(
                    (string)conditionAttribute.ConstructorArguments[0].Value!,
                    (int)conditionAttribute.ConstructorArguments[1].Value!);
            }

            var kindArgument = fieldAttribute.ConstructorArguments.Length > 1 ? fieldAttribute.ConstructorArguments[1] : default;
            var isVariable = kindArgument.Type?.ToDisplayString() == PacketFieldKindMetadataName &&
                kindArgument.Value is int kindValue &&
                kindValue != 0;

            fields.Add(new FieldModel(
                property.Name,
                (int)fieldAttribute.ConstructorArguments[0].Value!,
                isVariable,
                typeInfo,
                bits,
                condition));
        }

        var orderedFields = fields
            .OrderBy(static field => field.Order)
            .ToImmutableArray();

        var duplicateOrders = orderedFields
            .GroupBy(static field => field.Order)
            .Where(static group => group.Count() > 1)
            .ToArray();
        foreach (var duplicateOrder in duplicateOrders)
        {
            diagnostics.Add(CreateInvalidSpecDiagnostic(specType, $"Duplicate ConceptPacketField order '{duplicateOrder.Key}' in '{specType.Name}'."));
        }

        var fieldNames = new HashSet<string>(orderedFields.Select(static field => field.Name), StringComparer.Ordinal);
        foreach (var field in orderedFields)
        {
            if (field.Condition is not null && !fieldNames.Contains(field.Condition.FlagFieldName))
            {
                diagnostics.Add(CreateInvalidSpecDiagnostic(specType, $"Field '{field.Name}' references unknown flag field '{field.Condition.FlagFieldName}'."));
            }
        }

        return new PacketSpecModel(
            specType.Name,
            specType.ContainingNamespace.ToDisplayString(),
            codecTypeName!,
            definitionTypeName!,
            runtimePacketType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Convert.ToInt32(attribute.ConstructorArguments[0].Value),
            orderedFields,
            diagnostics.ToImmutable());
    }

    private static string GenerateSource(PacketSpecModel spec)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine($"namespace {spec.Namespace};");
        builder.AppendLine();
        builder.AppendLine($"public static class {spec.CodecTypeName}");
        builder.AppendLine("{");
        builder.AppendLine($"    public const byte MessageId = (byte){spec.MessageId};");
        builder.AppendLine($"    public const int MaxEncodedLength = {1 + spec.Fields.Sum(static field => field.Type.GetMaximumEncodedLength())};");
        builder.AppendLine();
        builder.AppendLine($"    public static int GetEncodedLength({spec.RuntimePacketTypeName} packet)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(packet);");
        builder.AppendLine("        return GetEncodedLengthCore(packet);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    public static byte[] Serialize({spec.RuntimePacketTypeName} packet)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(packet);");
        builder.AppendLine("        var bytes = new byte[GetEncodedLengthCore(packet)];");
        builder.AppendLine("        SerializeCore(packet, bytes, out _);");
        builder.AppendLine("        return bytes;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    public static void Serialize({spec.RuntimePacketTypeName} packet, global::System.Span<byte> destination, out int written)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(packet);");
        builder.AppendLine("        var requiredLength = GetEncodedLengthCore(packet);");
        builder.AppendLine("        if (destination.Length < requiredLength)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentException($\"Destination span is too small. Required={requiredLength}, Actual={destination.Length}\", nameof(destination));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        SerializeCore(packet, destination, out written);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    public static void Serialize({spec.RuntimePacketTypeName} packet, global::System.Buffers.IBufferWriter<byte> destination)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(packet);");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(destination);");
        builder.AppendLine("        SerializeCore(packet, destination.GetSpan(MaxEncodedLength), out var written);");
        builder.AppendLine("        destination.Advance(written);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    internal static int GetEncodedLengthCore({spec.RuntimePacketTypeName} packet)");
        builder.AppendLine("    {");
        AppendFlagValueLocals(builder, spec, 8);
        builder.AppendLine("        var length = 1;");
        foreach (var field in spec.Fields)
        {
            builder.Append("        ");
            if (field.Condition is not null)
            {
                builder.Append($"if ({GetConditionExpression(field.Condition, useFlagValues: true)}) ");
            }

            builder.AppendLine($"length += {field.Type.GetEncodedLengthExpression($"packet.{field.Name}")};");
        }

        builder.AppendLine("        return length;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    internal static void SerializeCore({spec.RuntimePacketTypeName} packet, global::System.Span<byte> destination, out int written)");
        builder.AppendLine("    {");
        AppendFlagValueLocals(builder, spec, 8);
        builder.AppendLine("        var offset = 0;");
        builder.AppendLine("        WriteByte(destination, ref offset, MessageId);");
        foreach (var field in spec.Fields)
        {
            if (field.Condition is not null)
            {
                builder.AppendLine($"        if ({GetConditionExpression(field.Condition, useFlagValues: true)})");
                builder.AppendLine("        {");
                builder.AppendLine($"            {field.Type.GetWriteStatement("destination", $"packet.{field.Name}", 12)}");
                builder.AppendLine("        }");
                continue;
            }

            builder.AppendLine($"        {field.Type.GetWriteStatement("destination", $"packet.{field.Name}", 8)}");
        }

        builder.AppendLine();
        builder.AppendLine("        written = offset;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    public static {spec.RuntimePacketTypeName} Deserialize(byte[] source)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(source);");
        builder.AppendLine("        return Deserialize(source.AsSpan(), out _);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    public static {spec.RuntimePacketTypeName} Deserialize(global::System.ReadOnlySpan<byte> source, out int consumed)");
        builder.AppendLine("    {");
        builder.AppendLine("        var offset = 0;");
        builder.AppendLine("        var messageId = ReadByte(source, ref offset);");
        builder.AppendLine("        if (messageId != MessageId)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.IO.InvalidDataException($\"Unexpected message id {messageId}. Expected {MessageId}.\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        var packet = new {spec.RuntimePacketTypeName}();");
        foreach (var field in spec.Fields)
        {
            if (field.Condition is not null)
            {
                builder.AppendLine($"        if ({GetConditionExpression(field.Condition)})");
                builder.AppendLine("        {");
                builder.AppendLine($"            packet.{field.Name} = {field.Type.GetReadExpression("source", 12)};");
                builder.AppendLine("        }");
                builder.AppendLine("        else");
                builder.AppendLine("        {");
                builder.AppendLine($"            packet.{field.Name} = {field.Type.GetDefaultExpression()};");
                builder.AppendLine("        }");
                continue;
            }

            builder.AppendLine($"        packet.{field.Name} = {field.Type.GetReadExpression("source", 8)};");
        }

        builder.AppendLine();
        builder.AppendLine("        if (offset != source.Length)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.IO.InvalidDataException($\"Packet was not fully consumed. Remaining={source.Length - offset}.\");");
        builder.AppendLine("        }");
        builder.AppendLine("        consumed = offset;");
        builder.AppendLine("        return packet;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void EnsureAvailable(global::System.ReadOnlySpan<byte> source, int offset, int length)");
        builder.AppendLine("    {");
        builder.AppendLine("        if ((uint)offset > (uint)source.Length || source.Length - offset < length)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.IO.InvalidDataException($\"Packet payload was truncated. Need {length} bytes at offset {offset}, but only {source.Length - offset} remain.\");");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteByte(global::System.Span<byte> destination, ref int offset, byte value)");
        builder.AppendLine("    {");
        builder.AppendLine("        destination[offset++] = value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static byte ReadByte(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnsureAvailable(source, offset, 1);");
        builder.AppendLine("        return source[offset++];");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteInt16(global::System.Span<byte> destination, ref int offset, short value)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(offset, 2), value);");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static short ReadInt16(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnsureAvailable(source, offset, 2);");
        builder.AppendLine("        var value = global::System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(source.Slice(offset, 2));");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine("        return value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteUInt16(global::System.Span<byte> destination, ref int offset, ushort value)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), value);");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static ushort ReadUInt16(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnsureAvailable(source, offset, 2);");
        builder.AppendLine("        var value = global::System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2));");
        builder.AppendLine("        offset += 2;");
        builder.AppendLine("        return value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteUInt32(global::System.Span<byte> destination, ref int offset, uint value)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(offset, 4), value);");
        builder.AppendLine("        offset += 4;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static uint ReadUInt32(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnsureAvailable(source, offset, 4);");
        builder.AppendLine("        var value = global::System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset, 4));");
        builder.AppendLine("        offset += 4;");
        builder.AppendLine("        return value;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteSingle(global::System.Span<byte> destination, ref int offset, float value)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset, 4), global::System.BitConverter.SingleToInt32Bits(value));");
        builder.AppendLine("        offset += 4;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static float ReadSingle(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnsureAvailable(source, offset, 4);");
        builder.AppendLine("        var bits = global::System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset, 4));");
        builder.AppendLine("        offset += 4;");
        builder.AppendLine("        return global::System.BitConverter.Int32BitsToSingle(bits);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteBoolean(global::System.Span<byte> destination, ref int offset, bool value)");
        builder.AppendLine("    {");
        builder.AppendLine("        destination[offset++] = value ? (byte)1 : (byte)0;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static bool ReadBoolean(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        EnsureAvailable(source, offset, 1);");
        builder.AppendLine("        return source[offset++] != 0;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void WriteVector2(global::System.Span<byte> destination, ref int offset, global::System.Numerics.Vector2 value)");
        builder.AppendLine("    {");
        builder.AppendLine("        WriteSingle(destination, ref offset, value.X);");
        builder.AppendLine("        WriteSingle(destination, ref offset, value.Y);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static global::System.Numerics.Vector2 ReadVector2(global::System.ReadOnlySpan<byte> source, ref int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        return new global::System.Numerics.Vector2(ReadSingle(source, ref offset), ReadSingle(source, ref offset));");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine($"internal static class {spec.DefinitionTypeName}");
        builder.AppendLine("{");
        builder.AppendLine("    private sealed class Codec : global::Terraria.NetWork.Core.Protocol.IPacketCustomCodec<" + spec.RuntimePacketTypeName + ">");
        builder.AppendLine("    {");
        builder.AppendLine("        public byte[] Write(global::Terraria.NetWork.Core.Protocol.PacketDefinition<" + spec.RuntimePacketTypeName + "> definition, " + spec.RuntimePacketTypeName + " packet)");
        builder.AppendLine("        {");
        builder.AppendLine($"            return {spec.CodecTypeName}.Serialize(packet);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public " + spec.RuntimePacketTypeName + " Read(global::Terraria.NetWork.Core.Protocol.PacketDefinition<" + spec.RuntimePacketTypeName + "> definition, byte[] packetBytes)");
        builder.AppendLine("        {");
        builder.AppendLine($"            return {spec.CodecTypeName}.Deserialize(packetBytes);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public void ValidatePacket(global::Terraria.NetWork.Core.Protocol.PacketDefinition<" + spec.RuntimePacketTypeName + "> definition, " + spec.RuntimePacketTypeName + " packet)");
        builder.AppendLine("        {");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private sealed class Layout");
        builder.AppendLine("    {");
        builder.AppendLine("        public Layout()");
        builder.AppendLine("        {");
        builder.AppendLine("            var builder = new global::Terraria.NetWork.Core.Protocol.PacketDefinitionBuilder<" + spec.RuntimePacketTypeName + ">();");
        foreach (var field in spec.Fields)
        {
            if (field.Condition is not null)
            {
                var conditionVariable = GetConditionVariableName(field.Condition);
                if (!spec.Fields.TakeWhile(candidate => candidate.Name != field.Name).Any(candidate => candidate.Condition == field.Condition))
                {
                    var flagHandleVariable = GetFlagHandleVariableName(field.Condition.FlagFieldName);
                    builder.AppendLine($"            var {conditionVariable} = global::Terraria.NetWork.Core.Protocol.PacketCondition.Flag({flagHandleVariable}, {field.Condition.BitIndex});");
                }
            }

            builder.AppendLine($"            {GetBuilderStatement(field)}");
        }

        builder.AppendLine($"            Definition = builder.Build({spec.CodecTypeName}.MessageId, new Codec());");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        public global::Terraria.NetWork.Core.Protocol.PacketDefinition<" + spec.RuntimePacketTypeName + "> Definition { get; }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static readonly Layout LayoutData = new();");
        builder.AppendLine();
        builder.AppendLine("    public static global::Terraria.NetWork.Core.Protocol.PacketDefinition<" + spec.RuntimePacketTypeName + "> Instance { get; } = LayoutData.Definition;");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string GetBuilderStatement(FieldModel field)
    {
        var fieldNameLiteral = $"\"{field.Name}\"";
        var conditionArgument = field.Condition is null ? string.Empty : $", {GetConditionVariableName(field.Condition)}";
        var hasValueArgument = field.Condition is not null && field.Type.IsNullable ? $", packet => packet.{field.Name}.HasValue" : string.Empty;
        var bitsArray = $"new global::Terraria.NetWork.Core.Protocol.PacketBitDefinition[] {{ {string.Join(", ", field.Bits.Select(static bit => $"new global::Terraria.NetWork.Core.Protocol.PacketBitDefinition({bit.Index}, \"{bit.Name}\")"))} }}";

        if (field.Type.IsBitsByte)
        {
            var assignment = field.Condition is not null || field.Bits.Length > 0 || field.Name.EndsWith("Flags", StringComparison.Ordinal)
                ? $"var {GetFlagHandleVariableName(field.Name)} = "
                : string.Empty;
            return assignment +
                $"builder.BitsByte({fieldNameLiteral}, {bitsArray}, packet => packet.{field.Name}, (packet, value) => packet.{field.Name} = value{conditionArgument});";
        }

        if (field.Type.IsVector2)
        {
            if (field.Type.IsNullable)
            {
                return $"builder.Vector2({fieldNameLiteral}, packet => packet.{field.Name}!.Value, (packet, value) => packet.{field.Name} = value{conditionArgument}{hasValueArgument});";
            }

            return $"builder.Vector2({fieldNameLiteral}, packet => packet.{field.Name}, (packet, value) => packet.{field.Name} = value{conditionArgument});";
        }

        if (!field.Type.IsNullable)
        {
            return field.Type.BuilderMethodName switch
            {
                "Byte" or "Int16" or "UInt16" or "Int32" or "Single" => $"builder.{field.Type.BuilderMethodName}({fieldNameLiteral}, packet => packet.{field.Name}, (packet, value) => packet.{field.Name} = value{conditionArgument});",
                _ => $"builder.Custom<{field.Type.UnderlyingTypeDisplayName}>({fieldNameLiteral}, \"{field.Type.TypeDisplayName}\", packet => packet.{field.Name}, (packet, value) => packet.{field.Name} = value, {field.Type.GetBuilderWriteLambda("value")}, {field.Type.GetBuilderReadLambda()}{conditionArgument});"
            };
        }

        return field.Type.BuilderMethodName switch
        {
            "Byte" or "Int16" or "UInt16" or "Int32" or "Single" => $"builder.{field.Type.BuilderMethodName}({fieldNameLiteral}, packet => packet.{field.Name}!.Value, (packet, value) => packet.{field.Name} = value{conditionArgument}{hasValueArgument});",
            _ => $"builder.Custom<{field.Type.UnderlyingTypeDisplayName}>({fieldNameLiteral}, \"{field.Type.UnderlyingTypeDisplayName}\", packet => packet.{field.Name}!.Value, (packet, value) => packet.{field.Name} = value, {field.Type.GetBuilderWriteLambda("value")}, {field.Type.GetBuilderReadLambda()}{conditionArgument}{hasValueArgument});"
        };
    }

    private static void AppendFlagValueLocals(StringBuilder builder, PacketSpecModel spec, int indentSize)
    {
        var indent = new string(' ', indentSize);
        foreach (var field in spec.Fields.Where(static field => field.Type.IsBitsByte))
        {
            var valueExpression = field.Condition is null
                ? $"(byte)packet.{field.Name}"
                : $"{GetConditionExpression(field.Condition, useFlagValues: true)} ? (byte)packet.{field.Name} : (byte)0";
            builder.AppendLine($"{indent}var {GetFlagValueVariableName(field.Name)} = {valueExpression};");
        }
    }

    private static string GetConditionExpression(ConditionModel condition, bool useFlagValues = false)
    {
        return useFlagValues
            ? $"({GetFlagValueVariableName(condition.FlagFieldName)} & 0x{1 << condition.BitIndex:X2}) != 0"
            : $"packet.{condition.FlagFieldName}[{condition.BitIndex}]";
    }

    private static string GetFlagValueVariableName(string fieldName) => $"{char.ToLowerInvariant(fieldName[0])}{fieldName.Substring(1)}Value";

    private static string GetConditionVariableName(ConditionModel condition)
    {
        return $"condition{condition.FlagFieldName}Bit{condition.BitIndex}";
    }

    private static string GetFlagHandleVariableName(string fieldName)
    {
        return $"{char.ToLowerInvariant(fieldName[0])}{fieldName.Substring(1)}Handle";
    }

    private static Diagnostic CreateInvalidSpecDiagnostic(ISymbol symbol, string message)
    {
        return Diagnostic.Create(InvalidSpecDescriptor, symbol.Locations.FirstOrDefault(), message);
    }

    private static Diagnostic CreateUnsupportedTypeDiagnostic(ISymbol symbol, string fieldName, string typeName)
    {
        return Diagnostic.Create(UnsupportedTypeDescriptor, symbol.Locations.FirstOrDefault(), fieldName, typeName);
    }

    private sealed record PacketSpecModel(
        string SpecTypeName,
        string Namespace,
        string CodecTypeName,
        string DefinitionTypeName,
        string RuntimePacketTypeName,
        int MessageId,
        ImmutableArray<FieldModel> Fields,
        ImmutableArray<Diagnostic> Diagnostics);

    private sealed record FieldModel(
        string Name,
        int Order,
        bool IsVariable,
        TypeModel Type,
        ImmutableArray<BitModel> Bits,
        ConditionModel? Condition);

    private sealed record BitModel(int Index, string Name);

    private sealed record ConditionModel(string FlagFieldName, int BitIndex);

    private sealed record TypeModel(
        string TypeDisplayName,
        string UnderlyingTypeDisplayName,
        string BuilderMethodName,
        bool IsNullable,
        bool IsBitsByte,
        bool IsVector2,
        bool IsSupported)
    {
        public static TypeModel Create(ITypeSymbol type)
        {
            var nullableType = type as INamedTypeSymbol;
            var isNullable = nullableType?.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
            var underlyingType = isNullable ? nullableType!.TypeArguments[0] : type;
            var underlyingName = underlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var displayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var builderMethodName = underlyingType.SpecialType switch
            {
                SpecialType.System_Byte => "Byte",
                SpecialType.System_Int16 => "Int16",
                SpecialType.System_UInt16 => "UInt16",
                SpecialType.System_Int32 => "Int32",
                SpecialType.System_Single => "Single",
                _ => string.Empty
            };

            if (underlyingType.ToDisplayString() == BitsByteMetadataName)
            {
                return new TypeModel(displayName, underlyingName, "BitsByte", isNullable, true, false, true);
            }

            if (underlyingType.ToDisplayString() == Vector2MetadataName)
            {
                return new TypeModel(displayName, underlyingName, "Vector2", isNullable, false, true, true);
            }

            var isSupported = builderMethodName.Length > 0 ||
                underlyingType.SpecialType is SpecialType.System_UInt32 or SpecialType.System_Boolean;

            return new TypeModel(displayName, underlyingName, builderMethodName, isNullable, false, false, isSupported);
        }

        public string GetEncodedLengthExpression(string valueExpression)
        {
            if (IsVector2)
            {
                return "8";
            }

            if (IsBitsByte)
            {
                return "1";
            }

            return UnderlyingTypeDisplayName switch
            {
                "byte" or "global::System.Byte" => "1",
                "bool" or "global::System.Boolean" => "1",
                "short" or "global::System.Int16" => "2",
                "ushort" or "global::System.UInt16" => "2",
                "int" or "global::System.Int32" => "4",
                "uint" or "global::System.UInt32" => "4",
                "float" or "global::System.Single" => "4",
                _ => throw new InvalidOperationException($"Unsupported encoded length type '{UnderlyingTypeDisplayName}'.")
            };
        }

        public int GetMaximumEncodedLength()
        {
            if (IsVector2)
            {
                return 8;
            }

            if (IsBitsByte)
            {
                return 1;
            }

            return UnderlyingTypeDisplayName switch
            {
                "byte" or "global::System.Byte" => 1,
                "bool" or "global::System.Boolean" => 1,
                "short" or "global::System.Int16" => 2,
                "ushort" or "global::System.UInt16" => 2,
                "int" or "global::System.Int32" => 4,
                "uint" or "global::System.UInt32" => 4,
                "float" or "global::System.Single" => 4,
                _ => throw new InvalidOperationException($"Unsupported maximum encoded length type '{UnderlyingTypeDisplayName}'.")
            };
        }

        public string GetWriteStatement(string destinationExpression, string valueExpression, int indentSize)
        {
            var indent = new string(' ', indentSize);
            var value = IsNullable ? $"{valueExpression}.GetValueOrDefault()" : valueExpression;
            return UnderlyingTypeDisplayName switch
            {
                "byte" or "global::System.Byte" => $"WriteByte({destinationExpression}, ref offset, {value});",
                "short" or "global::System.Int16" => $"WriteInt16({destinationExpression}, ref offset, {value});",
                "ushort" or "global::System.UInt16" => $"WriteUInt16({destinationExpression}, ref offset, {value});",
                "uint" or "global::System.UInt32" => $"WriteUInt32({destinationExpression}, ref offset, {value});",
                "float" or "global::System.Single" => $"WriteSingle({destinationExpression}, ref offset, {value});",
                "bool" or "global::System.Boolean" => $"WriteBoolean({destinationExpression}, ref offset, {value});",
                Vector2MetadataName or "global::System.Numerics.Vector2" => $"WriteVector2({destinationExpression}, ref offset, {value});",
                BitsByteMetadataName or "global::Terraria.NetWork.Core.Protocol.BitsByte" => $"WriteByte({destinationExpression}, ref offset, (byte){value});",
                _ => throw new InvalidOperationException($"Unsupported write type '{UnderlyingTypeDisplayName}'.")
            };
        }

        public string GetReadExpression(string sourceExpression, int indentSize)
        {
            return UnderlyingTypeDisplayName switch
            {
                "byte" or "global::System.Byte" => $"ReadByte({sourceExpression}, ref offset)",
                "short" or "global::System.Int16" => $"ReadInt16({sourceExpression}, ref offset)",
                "ushort" or "global::System.UInt16" => $"ReadUInt16({sourceExpression}, ref offset)",
                "uint" or "global::System.UInt32" => $"ReadUInt32({sourceExpression}, ref offset)",
                "float" or "global::System.Single" => $"ReadSingle({sourceExpression}, ref offset)",
                "bool" or "global::System.Boolean" => $"ReadBoolean({sourceExpression}, ref offset)",
                Vector2MetadataName or "global::System.Numerics.Vector2" => $"ReadVector2({sourceExpression}, ref offset)",
                BitsByteMetadataName or "global::Terraria.NetWork.Core.Protocol.BitsByte" => $"(global::Terraria.NetWork.Core.Protocol.BitsByte)ReadByte({sourceExpression}, ref offset)",
                _ => throw new InvalidOperationException($"Unsupported read type '{UnderlyingTypeDisplayName}'.")
            };
        }

        public string GetDefaultExpression()
        {
            return IsNullable ? "null" : "default";
        }

        public string GetBuilderWriteLambda(string parameterName)
        {
            return UnderlyingTypeDisplayName switch
            {
                "uint" or "global::System.UInt32" => $"static (writer, {parameterName}) => writer.Write({parameterName})",
                "bool" or "global::System.Boolean" => $"static (writer, {parameterName}) => writer.Write({parameterName})",
                _ => throw new InvalidOperationException($"Unsupported custom builder write type '{UnderlyingTypeDisplayName}'.")
            };
        }

        public string GetBuilderReadLambda()
        {
            return UnderlyingTypeDisplayName switch
            {
                "uint" or "global::System.UInt32" => "static reader => reader.ReadUInt32()",
                "bool" or "global::System.Boolean" => "static reader => reader.ReadBoolean()",
                _ => throw new InvalidOperationException($"Unsupported custom builder read type '{UnderlyingTypeDisplayName}'.")
            };
        }
    }
}
