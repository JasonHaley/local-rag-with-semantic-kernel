Two weeks ago I blogged [Part 1](/2024/04/11/semantic-kernel-hello-world-plugin-part1/), in which I moved the prompt to a prompt template. In this part, I implement a [native function](https://learn.microsoft.com/en-us/semantic-kernel/agents/plugins/using-the-kernelfunction-decorator?tabs=Csharp) that will take in the current date and make the call to the LLM.

I’ve put the code for this blog in the [HelloWorld.Plugin2.Console project](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/src/HelloWorld.Plugin2.Console) in the same repo as the other SK entries: [semantic-kernel-getting-started](https://github.com/JasonHaley/semantic-kernel-getting-started).

# Semantic Kernel Plugin: Native Function

There is a good Microsoft Learn module: [Give your AI agent skills](https://learn.microsoft.com/en-us/training/modules/give-your-ai-agent-skills/) that walks you through the details of what a native function is and how to implement them.

The functions in that learn module don't make calls to OpenAI - which is something I wanted to do ... so I had to do some digging on how to make this work. Turns out it isn't that hard to do - though I also discovered there is a lot of SK demo code out there that has not been updated to v1 code which changed the syntax on quite a few things.

Just a reminder of what we are looking to reproduce: a hello world example that calls OpenAI with a simple prompt with today's day and return a historical fact about the date. The output should be similar to this: 

![Bills Code](/img/2024-03-30_img1.jpg)

## The Code:

Like with the [first blog in this series](/2024/03/30/semantic-kernel-hello-world/), I started with a new console and copied the setup and configuration code I created in that original hello world console app.

The following two files are important for this blog:

* [/Plugins/DailyFactPlugin.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin2.Console/Plugins/DailyFactPlugin.cs) (a new file) - this is the native function
* [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin2.Console/Program.cs) - this has the code to utilize the new native function

### DailyFactPlugin

The native function is in the Plugins directory, just to keep it clean (I could have put it anywhere in the project).

```C#
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace HelloWorld.Plugin.Console.Plugins;

public class DailyFactPlugin
{
    private const string DESCRIPTION = "Provides interesting historic facts for the current date.";
    private const string TEMPLATE = @"Tell me an interesting fact from world 
        about an event that took place on {{$today}}.
        Be sure to mention the date in history for context.";
    internal const string GET_DAILY_FACT_FUNC = "GetDailyFactFunc";
    internal const string PLUGIN_NAME = "DailyFactPlugin";

    private readonly KernelFunction _dailyFact;
    
    public DailyFactPlugin()
    {
        PromptExecutionSettings settings = new()
        {
            ExtensionData = new Dictionary<string, object>()
            {
                { "Temperature", 0.7 },
                { "MaxTokens", 250 }
            }

        };

        _dailyFact = KernelFunctionFactory.CreateFromPrompt(TEMPLATE, 
            functionName: GET_DAILY_FACT_FUNC, 
            description: DESCRIPTION, 
            executionSettings: settings);
    }
    
    [KernelFunction]
    public async Task<string> GetDailyFact([Description("Current date")] string today, Kernel kernel)
    {
        var result = await _dailyFact.InvokeAsync(kernel, new() { ["today"] = today }).ConfigureAwait(false);

        return result.ToString();
    }
}


```

I created constants to put the description of the function, promt template text and function name - again just to make the code cleaner.

I've seen many ways to declare and setup KernelFunctions, I modeled mine after this one: [TextSummaryPlugin](https://github.com/Azure/Vector-Search-AI-Assistant/blob/cognitive-search-vector/VectorSearchAiAssistant.SemanticKernel/Plugins/Core/TextSummaryPlugin.cs). It seemed to be close to what I want to do (it takes in some text and calls OpenAI to get a summary of that text). The one difference is that TextSummaryPlugin is instatiated in the [SemanticKernalRAGService](https://github.com/Azure/Vector-Search-AI-Assistant/blob/cognitive-search-vector/VectorSearchAiAssistant.Service/Services/SemanticKernelRAGService.cs) which passes a Kernel to the constructor, which I don't need to do since a Kernel will be passed when I invoke the function call.

The member variable `_dailyFact` is initialized in the constructor. The `GetDailyFact` method is has the `[KernelFunction]` attribute to indicate it is a native function. All it does it take the current day string (and kernel) then invokes the `_dailyFact` with the passed parameters.

### Using the Native Function

In the Program.cs file, the top code hasn't really changed and it still just does the configuration:

```C#

using HelloWorld.Plugin.Console.Plugins;
using HelloWorld.Plugin2.Console.Configuration;

internal class Program
{
    static void Main(string[] args)
    {
        MainAsync(args).Wait();
    }

    static async Task MainAsync(string[] args)
    {
        var config = Configuration.ConfigureAppSettings();

        // Get Settings (all this is just so I don't have hard coded config settings here)
        var openAiSettings = new OpenAIOptions();
        config.GetSection(OpenAIOptions.OpenAI).Bind(openAiSettings);

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConfiguration(config);
            builder.AddConsole();
        });

        // Configure Semantic Kernel
        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(loggerFactory);
        builder.Services.AddChatCompletionService(openAiSettings);
        
        ....
    }
}

```
> NOTE: I did remove some additional Open Telemetry logging configuration, that isn't important at this point

In order to load the DailyFactPlugin, I add it to the Plugins on the KernelBuilder (I could have added it to the kernel instead):

```C#
        builder.Plugins.AddFromType<DailyFactPlugin>();

        Kernel kernel = builder.Build();
```

The code for getting today's date and writing it out, is the same as the last blog entry:

```C#
        // output today's date just for fun
        var today = DateTime.Now.ToString("MMMM dd");
        WriteLine($"Today is {today}");

```

To call the `GetDailyFact` function, I created a `KernelArguments` object to add the `today` variable to (but could have done it inline like [last time](/2024/04/11/semantic-kernel-hello-world-plugin-part1/) when I was using the prompt template). Then I invoke the function using the `kernel.InvokeAsync()`, passing the name of the plugin, function and args in order to make the call to OpenAI and get the response back.

```C#
        // Using a function with a parameter -----------------------------
        var funcargs = new KernelArguments { ["today"] = today };

        var funcresult = await kernel.InvokeAsync(
            DailyFactPlugin.PLUGIN_NAME,
            DailyFactPlugin.GET_DAILY_FACT, 
            funcargs
            );
        
        WriteLine($"\nRESPONSE: \n\n{funcresult}");
```

When you run the code, the output is pretty similar to the last entry:

![Output](/img/2024-04-26_img1.jpg)

## Conclusion

In this short entry, I show the implementation of a Semantic Kernel native function that calls OpenAI. In the next blog I'll cover
doing the same thing but I'll use [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling) to get the current day and call the function (plus some utility code to help view the api calls).

If you have a comment, please message me @haleyjason on twitter/X.
