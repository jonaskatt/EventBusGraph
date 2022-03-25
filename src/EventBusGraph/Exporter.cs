using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventBusGraph;

public static class Exporter
{
    public static void ExportToFile(object item, string path)
    {
        File.WriteAllBytes(path, JsonSerializer.SerializeToUtf8Bytes(item, SerializerOptions));
    }

    private static JsonSerializerOptions SerializerOptions => new()
    {
        Converters = {new JsonStringEnumConverter()}
    };
}