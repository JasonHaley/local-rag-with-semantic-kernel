Last night was the [Season of AI](https://www.meetup.com/bostonazure/events/301201162/) presentation. We started with Bill Wilder presenting the fundamentals of Generative AI and quick introduction to Azure AI Studio, then I finished up with a .NET code walkthrough implement Retrieval Augmented Generation (RAG) using Semantic Kernel.

It was nice to see a lot of regular faces and meet several new people.

# Demo Code

The demo code is on my GitHub repo [BostonAzure-June2024](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/samples/demos/BostonAzure-June2024) under a subdirectory.

![Github](/img/2024-06-25_img1.jpg)

The code is setup as the beginning of the demo (ie. simple echo client/api implementation), you'll find the steps I used to progressively create the demo in the [demo-script.md file](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/samples/demos/BostonAzure-June2024/demo-script.md).

Since I ran of time to do the last "bonus step", you'll find it at the end of that script along with the full content of the final code (shown below):

```csharp
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using System.Collections.Frozen;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddKernel()
//	.AddOpenAIChatCompletion("gpt-4o", builder.Configuration["AI:OpenAI:ApiKey"])
//	.AddOpenAITextEmbeddingGeneration("text-embedding-ada-002", builder.Configuration["AI:OpenAI:ApiKey"]);

builder.Services.AddKernel()
	.AddOpenAIChatCompletion("gpt-4o", builder.Configuration["AI:OpenAI:ApiKey"], null, null, new HttpClient(new RequestAndResponseLoggingHttpClientHandler()))
	.AddOpenAITextEmbeddingGeneration("text-embedding-ada-002", builder.Configuration["AI:OpenAI:ApiKey"], null, null, new HttpClient(new RequestLoggingHttpClientHandler()));

var app = builder.Build();

// Step 2: Text Chunking
var code = File.ReadAllLines(@"transcript.txt");
var tokenizer = Tiktoken.CreateTiktokenForModel("gpt-4o");
var chunks = TextChunker.SplitPlainTextParagraphs(code, 500, 100, null, text => tokenizer.CountTokens(text));

// Step 3: Vector Store
var embeddingService = app.Services.GetRequiredService<ITextEmbeddingGenerationService>();

var memoryBuilder = new MemoryBuilder();
memoryBuilder.WithTextEmbeddingGeneration(embeddingService);
memoryBuilder.WithMemoryStore(new VolatileMemoryStore());

var memory = memoryBuilder.Build();

for (int i = 0; i < 10; i++)
{
	await memory.SaveInformationAsync("chunks", id: i.ToString(), text: chunks[i]);
}

app.MapGet("/copilot", async (string question, Kernel kernel) =>
{
	// Step 4:  Search the Vector Store
	var results = await memory.SearchAsync("chunks", question, 10, 0.6).ToListAsync();

	var context = new StringBuilder();

	int tokensRemaining = 2000;
	foreach (var result in results)
	{
		//-----------------------------------------------------------------------------------------------------------------------------
		// Keep Prompt under specific size
		if ((tokensRemaining -= tokenizer.CountTokens(result.Metadata.Text)) < 0)
			break;
		//-----------------------------------------------------------------------------------------------------------------------------

		System.Console.WriteLine($"Search Result: {result.Relevance.ToString("P")}");
		System.Console.WriteLine(result.Metadata.Text);
		System.Console.WriteLine("");

		//	prompt.AppendLine(result.Metadata.Text);
		context.AppendLine(result.Metadata.Text);
	}

	
	var prompts = kernel.CreatePluginFromPromptDirectory("Prompts");
	return prompts["RAG"].InvokeStreamingAsync<string>(kernel, new KernelArguments()
		{
			{ "question", question },
			{ "context", context.ToString() }
		});

});

app.Run();
```

# References and other learning materials

Here are some Semantic Kernel references that you may find useful:

* [My Semantic Kernel Hello World blogs](https://jasonhaley.com/tags/semantic-kernel/)
* [Semantic Kernel Discord](https://discord.com/invite/semantic-kernel-1063152441819942922)
* [Semantic Kernel Office Hours Videos](https://www.youtube.com/playlist?list=PLmsFUfdnGr3xAtM3ofjpEkIWnuSA2d-i-)
* [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
* [Semantic Kernel Github](https://github.com/microsoft/semantic-kernel)

If you have a comment, please message me @haleyjason on twitter/X.
