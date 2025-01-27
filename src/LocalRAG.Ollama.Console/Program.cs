using Filters;
using LocalRAG.Common.Configuration;
using LocalRAG.Common.Loaders;
using LocalRAG.Common.Models;
using LocalRAG.Ollama.Console;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using OpenAI.Chat;
using Qdrant.Client;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args).AddAppSettings();

//var ollamaClient = new OllamaApiClient("http://localhost:11434", "phi3");
//var ollamaClient = new OllamaApiClient("http://localhost:11434", "llama3.1");
//var ollamaClient = new OllamaApiClient("http://localhost:11434", "phi3.5");
//var chatService = ollamaClient.AsChatCompletionService();


var kernelBuilder = Kernel.CreateBuilder()
    .AddQdrantVectorStore("localhost")
    .AddOllamaChatCompletion("phi3.5", new Uri("http://localhost:11434"))
    .AddOllamaTextEmbeddingGeneration("nomic-embed-text", new Uri("http://localhost:11434"));

kernelBuilder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
kernelBuilder.Services.AddSingleton<IAutoFunctionInvocationFilter, AutoFunctionInvocationLoggingFilter>();

var kernel = kernelBuilder.Build();

kernel.ImportPluginFromPromptDirectory("Prompts");

var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
var chatService = kernel.GetRequiredService<IChatCompletionService>();

var vectorStore = kernel.GetRequiredService<IVectorStore>();
var vectorStoreCollection = vectorStore.GetCollection<Guid, RawChunk>("blog-entries");

await vectorStoreCollection.CreateCollectionIfNotExistsAsync();

var loader = new MarkdownLoader(embeddingService, vectorStoreCollection);
await loader.LoadMarkdownAsync(@"D:\__ai\GitHubJH\local-rag-with-semantic-kernel\src\LocalRAG.Ollama.Console\sample-data");

//var rag = kernel.Plugins["Prompts"];

//PromptExecutionSettings promptExecutionSettings = new()
//{
//    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
//};

var responseTokens = new StringBuilder();
var chatHistory = new ChatHistory("You are a helpful assistant that knows blog entries Jason Haley has made.");
while (true)
{
    Console.Write("\nUser: ");

    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question))
    {
        break;
    }

    var promptEmbedding = await embeddingService.GenerateEmbeddingAsync(question);

    var results = new StringBuilder();


    var searchResult = await vectorStoreCollection.VectorizedSearchAsync(promptEmbedding, new() { Top = 5 });

    Console.WriteLine("============== SERACH RESULTS ==============");  
    await foreach (var result in searchResult.Results)
    {
        results.AppendLine(result.Record.Text);
        Console.WriteLine($"{result.Score}");
        Console.WriteLine($"{result.Record.Text}");

        Console.WriteLine("============================================");
    }


    //var prompt = @$"Your are an intelligent, cheerful assistant who prioritizes answers to user questions using the data in this conversation. 
    //            If you do not know the answer, say 'I don't know.'. 
    //            Answer the following question: 

    //            [Question]
    //            {question}

    //            Prioritize the following data to answer the question:
    //            [Data]
    //            {results}
    //";

    var prompt = @$"Your are an intelligent, cheerful assistant who prioritizes answers to user questions using the data in this conversation. 
                You will be give relevant parts of blog posts written by Jason Haley for data. Use the content give to answer the question the best you can.
                For reference, today's date is: {DateTime.Now.ToShortDateString()}.
                If you do not know the answer, say 'I don't know.'. 
                Answer the following question: 

                [Question]
                {question}

                Prioritize the following data to answer the question:
                [Data]
                {results}
    ";

    chatHistory.AddUserMessage(prompt);
    responseTokens.Clear();

    Console.WriteLine("Calling Ollama API...");

    await foreach (var token in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
    {
        Console.Write(token);
        responseTokens.Append(token);
    }
    chatHistory.AddAssistantMessage(responseTokens.ToString());

    Console.WriteLine();
}