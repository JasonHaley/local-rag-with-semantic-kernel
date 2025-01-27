
//using Microsoft.Extensions.VectorData;

//namespace LocalRAG.Common.Models;

//public class Chunk
//{
//    public Chunk()
//    {}

//    public Chunk(MarkdownSection sectionBase, string text, int index, string fileName, string blogDate, string blogSlug)
//    {
//        Header = sectionBase.Header;
//        SectionIndex = sectionBase.Index;
//        Text = text;
//        Index = index;

//        FileName = fileName;
//        BlogDate = blogDate;
//        BlogSlug = blogSlug;
//    }

//    [VectorStoreRecordData]
//    public string BlogDate { get; set; } = "";
//    [VectorStoreRecordData]
//    public string BlogSlug { get; set; } = "";
//    [VectorStoreRecordData]
//    public string FileName { get; set; } = "";

//    [VectorStoreRecordKey]
//    public Guid Id { get; set; } = Guid.NewGuid();
//    [VectorStoreRecordData]
//    public int SectionIndex { get; set; }
//    [VectorStoreRecordData]
//    public int Index { get; set; }
//    [VectorStoreRecordData]
//    public string Text { get; set; } = "";
//    [VectorStoreRecordData]
//    public string Header { get; set; } = "";


//    [VectorStoreRecordVector(Dimensions: 768)]
//    public ReadOnlyMemory<float>? TextEmbedding { get; set; }
//}
