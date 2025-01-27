using LocalRAG.Common.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;

namespace LocalRAG.Common.Loaders;
public class MarkdownLoader(ITextEmbeddingGenerationService embeddingService, IVectorStoreRecordCollection<Guid, RawChunk> collection)
{

    public async Task LoadMarkdownAsync(string path)
    {
        if (File.Exists(path))
        {
            await LoadMarkdownFileAsync(path);
        }
        else if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.md");
            foreach (var file in files)
            {
                await LoadMarkdownFileAsync(file);
            }
        }
    }
    public async Task LoadMarkdownFileAsync(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var blogDate = fileName.Substring(0, 9);
        var blogSlug = fileName.Substring(11);

        var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4"); // Use Cl100kBaseEncoding

        var markdown = await File.ReadAllTextAsync(path);
        var index = 0;
        
        // Chunk the text into smaller pieces
        var chunks = TextChunker.SplitPlainTextParagraphs([markdown], 512, chunkHeader: fileName, tokenCounter: text => tokenizer.CountTokens(text));
        foreach (var chunkText in chunks)
        {
            Console.WriteLine("Getting embeddings...");

            var embedding = await embeddingService.GenerateEmbeddingAsync(chunkText);

            Console.WriteLine("Saving to store...");

            var chunk = new RawChunk(chunkText, index: index, fileName, blogDate, blogSlug);
            chunk.TextEmbedding = embedding;

            await collection.UpsertAsync(chunk);

            Console.WriteLine("Saved...");

            index++;
        }
        

        Console.WriteLine($"File {fileName}.md complete.");
    }

    //public async Task LoadMarkdownFileBySectionsAsync(string path)
    //{
    //    var fileName = Path.GetFileNameWithoutExtension(path);
    //    var blogDate = fileName.Substring(0, 9);
    //    var blogSlug = fileName.Substring(11);

    //    var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4"); // Use Cl100kBaseEncoding

    //    var markdown = await File.ReadAllTextAsync(path);
    //    var sections = new MarkdownParser().GetSections(markdown);
    //    var index = 0;
    //    foreach (var section in sections)
    //    {
    //        // Chunk the text into smaller pieces
    //        var chunks = TextChunker.SplitPlainTextParagraphs([section.Text], 512, chunkHeader: section.Header, tokenCounter: text => tokenizer.CountTokens(text));
    //        foreach (var chunkText in chunks)
    //        {
    //            Console.WriteLine("Getting embeddings...");

    //            var embedding = await embeddingService.GenerateEmbeddingAsync(chunkText);

    //            Console.WriteLine("Saving to store...");

    //            var chunk = new Chunk(section, chunkText, index: index, fileName, blogDate, blogSlug);
    //            chunk.TextEmbedding = embedding;

    //            await collection.UpsertAsync(chunk);

    //            Console.WriteLine("Saved...");

    //            index++;
    //        }
    //    }

    //    Console.WriteLine($"File {fileName}.md complete.");
    //}
}
