using LocalRAG.Common.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;

namespace LocalRAG.Common.Loaders;

public class TextFileLoader(ITextEmbeddingGenerationService embeddingService, IVectorStoreRecordCollection<Guid, RawChunk> collection)
{
    public async Task LoadTextFileAsync(string path, string extension = "txt")
    {
        if (File.Exists(path))
        {
            await LoadTextAsync(path);
        }
        else if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, $"*.{extension}");
            foreach (var file in files)
            {
                await LoadTextAsync(file);
            }
        }
    }

    public async Task LoadTextAsync(string path)
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
}
