using System.Text;
using System.Text.Json;
using Dotnet.Installer.Core.Converters;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Tests.Converters;

public class DotnetVersionJsonConverterTests
{
    [Theory]
    [InlineData("8.0.100-preview.6", 8, 0, 100, true, false, 6)]
    [InlineData("8.0.100-rc.1", 8, 0, 100, false, true, 1)]
    [InlineData("8.0.2", 8, 0, 2, false, false, null)]
    public void Read_WithValidInput_ShouldDeserializeDotnetVersionCorrectly(string version,
        int major, int minor, int patch, bool isPreview, bool isRc, int? previewIdentifier)
    {
        // Arrange
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes($"\"{version}\""));
        reader.Read(); // Advance to the first token
        var converter = new DotnetVersionJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(DotnetVersion), new JsonSerializerOptions());

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier), result);
    }

    [Theory]
    [InlineData("8.0.aaa-preview.6")]
    [InlineData("8.0.aaa-rc.1")]
    [InlineData("8.0.a")]
    public void Read_WithInvalidInput_ShouldDeserializeDotnetVersionCorrectly(string version)
    {
        // Arrange
        var converter = new DotnetVersionJsonConverter();

        // Assert
        Assert.Throws<FormatException>(() => 
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes($"\"{version}\""));
            reader.Read(); // Advance to the first token
            converter.Read(ref reader, typeof(DotnetVersion), new JsonSerializerOptions());
        });
    }

    [Fact]
    public void Read_WithNullInput_ShouldReturnNull()
    {
        // Arrange
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes("null"));
        reader.Read(); // Advance to the first token
        var converter = new DotnetVersionJsonConverter();

        // Act
        var result = converter.Read(ref reader, typeof(DotnetVersion), JsonSerializerOptions.Default);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("\"8.0.100-preview.6\"", 8, 0, 100, true, false, 6)]
    [InlineData("\"8.0.100-rc.1\"", 8, 0, 100, false, true, 1)]
    [InlineData("\"8.0.2\"", 8, 0, 2, false, false, null)]
    public void Write_WithValidDotnetVersion_ShouldOutputCorrectJson(string expectedOutput,
        int major, int minor, int patch, bool isPreview, bool isRc, int? previewIdentifier)
    {
        // Arrange
        var version = new DotnetVersion(major, minor, patch, isPreview, isRc, previewIdentifier);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var converter = new DotnetVersionJsonConverter();

        // Act
        converter.Write(writer, version, JsonSerializerOptions.Default);
        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(stream);
        var json = sr.ReadToEnd();

        // Assert
        Assert.Equal(expectedOutput, json);
    }
}
