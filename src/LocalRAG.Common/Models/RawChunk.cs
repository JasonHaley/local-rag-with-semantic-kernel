
using Microsoft.Extensions.VectorData;

namespace LocalRAG.Common.Models;

public class RawChunk
{
    public RawChunk()
    { }

    public RawChunk(string text, int index, string fileName, string blogDate, string blogSlug)
    {
        Text = text;
        Index = index;

        FileName = fileName;
        BlogDate = blogDate;
        BlogSlug = blogSlug;
    }

    [VectorStoreRecordKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [VectorStoreRecordData]
    public string BlogDate { get; set; } = "";
    [VectorStoreRecordData]
    public string BlogSlug { get; set; } = "";
    [VectorStoreRecordData]
    public string FileName { get; set; } = "";
    [VectorStoreRecordData]
    public int Index { get; set; }
    [VectorStoreRecordData]
    public string Text { get; set; } = "";
    
    [VectorStoreRecordVector(Dimensions: 768)]
    public ReadOnlyMemory<float>? TextEmbedding { get; set; }
}
