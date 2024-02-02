using System.Text.Json;
using System.Text.Json.Serialization;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Converters;

public class DotnetVersionJsonConverter : JsonConverter<DotnetVersion>
{
    public override DotnetVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return (value is not null) 
            ? DotnetVersion.Parse(value)
            : default;
    }

    public override void Write(Utf8JsonWriter writer, DotnetVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
