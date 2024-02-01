using System.Text.Json;
using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Enums;

namespace Dotnet.Installer.Core;

public class StringToArchitectureJsonConverter : JsonConverter<Architecture>
{
    public override Architecture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        
        return Enum.TryParse<Architecture>(value, ignoreCase: true, out var result)
            ? result
            : Architecture.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, Architecture value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
