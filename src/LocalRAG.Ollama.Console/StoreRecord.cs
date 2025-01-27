using Microsoft.Extensions.VectorData;
using System.Text.Json.Serialization;

namespace LocalRAG.Ollama.Console;

public sealed class StoreRecord
{
    [VectorStoreRecordKey]
    public ulong Key { get; set; }

    [VectorStoreRecordData]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [VectorStoreRecordData]
    [JsonPropertyName("descrption")]
    public string Description { get; set; }

    [VectorStoreRecordVector(384)]
    [JsonPropertyName("embedding")]
    public ReadOnlyMemory<float> Embedding { get; set; }

}
