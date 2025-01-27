This past Thursday night after the Virtual Boston Azure meetup, Bill Wilder ([@codingoutloud](https://twitter.com/codingoutloud)) created an
[AI mini-workshop (hands on)](https://www.meetup.com/bostonazure/events/299729681/) for the attendees that were interested in getting hands on with code using the Azure OpenAI API.

This post is me using the same idea but with Semantic Kernel.

# OpenAI Chat Hello World C#

Bill provided the following code for us to get a simple OpenAI chat working:
```C#
using Azure;
using Azure.AI.OpenAI;


string? key = "...";
string? endpoint = "...";
string? deployment = "...";

// output today's date just for fun
Console.WriteLine($"\n----------------- DEBUG INFO -----------------");
var today = DateTime.Now.ToString("MMMM dd");
Console.WriteLine($"Today is {today}");
Console.WriteLine("----------------------------------------------");


var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

// TODO: CHALLENGE 1: does the AI respond accurately to this prompt? How to fix?
var prompt = $"Tell me an interesting fact from world about an event " +
            $"that took place on {today}. " +
            $"Be sure to mention the date in history for context.";

CompletionsOptions completionsOptions = new()
{
    Temperature = 0.7f,
    DeploymentName = deployment,
    Prompts = { prompt },
    MaxTokens = 250,  // PLEASE DON'T MAKE LARGER THAN 250 (but see what happens at 25)
};

Response<Completions> completionsResponse = client.GetCompletions(completionsOptions);

Console.WriteLine($"\nPROMPT: \n\n{prompt}");

int i = 0;
foreach (var choice in completionsResponse.Value.Choices)
{    
    Console.WriteLine($"\nRESPONSE {++i}/{completionsResponse.Value.Choices.Count}:" +
        $"{choice.Text}");
}

Console.WriteLine($"\n----------------- DEBUG INFO -----------------");
Console.WriteLine($"Tokens used: {completionsResponse.Value.Usage.CompletionTokens}/{completionsOptions.MaxTokens}");
Console.WriteLine("----------------------------------------------");
```

When you run this code (you'll of course need to add in you own values for the `key`, `endpoint` and `deployment`), you will get a response like this:

![Bills Code](/img/2024-03-30_img1.jpg)

If you want to watch the 30min or so portion of the recording when we were hacking around with the above code in both C# and python, you can access it with this [link](https://www.youtube.com/watch?v=3Zh9MEuyTQo&t=1h34m32s).

Basically, we played around with changing the prompt and parameters, to see how the response was effected.

# OpenAI Chat Hello World C# using Semantic Kernel

So yesterday, I finally started playing around with [Semantic Kernel](https://github.com/microsoft/semantic-kernel) (SK) in a brand new codebase and thought it would be a good exercise to take the code Bill created and rewrite it using SK. 

I've put the following code out on github for those of you who want the running solution: [semantic-kernel-getting-started](https://github.com/JasonHaley/semantic-kernel-getting-started) and I plan to add to it as I dig into SK and learn how to use it better this next month.

## The Code:

I started with a brand new .NET 8 console app and added references to several Nuget packages as I worked through the implementation.

In order to build for future projects, the first thing I did was to move all the settings (api key, deployment name, etc.) to the [appsetting.json](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/appsettings.json) file and add the [options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) to use the settings in my code.

That code is the following set of files:
* [appsetting.json](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/appsettings.json) - this has the logging configuration and the OpenAIOptions values to be overridden in the next file.
* appsetting.user_username.json - this file is not included in GitHub and is meant for me to store my credentials safely for the demo.
* [OpenAIOptions.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/OpenAIOptions.cs) - this is the C# class that will get populated from the appsettings files.
* [Configuration.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/Configuration.cs) - this is where the json files are added to the configuration.
* [ServiceCollectionExtensions.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/ServiceCollectionExtensions.cs) - has an extension method that abstracts the usage of AzureOpenAI or OpenAI and calls the appropriate extension method in Semantic Kernel to add the appropriate services.
* [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/Program.cs) - this is where the majority of the code is that implements the same call as the code Bill created above.

The configuration/settings values are wired up with this code in the top of `MainAsync`.

```C#
var config = Configuration.ConfigureAppSettings();

// Get Settings (all this is just so I don't have hard coded config settings here)
var openAiSettings = new OpenAIOptions();
config.GetSection(OpenAIOptions.OpenAI).Bind(openAiSettings);

```

Next is the logging which I'll come back to later. After the logging is the creation of the `kernel`:

```C#

 // Configure Semantic Kernel
 var builder = Kernel.CreateBuilder();
         
 builder.Services.AddSingleton(loggerFactory);
 builder.Services.AddChatCompletionService(openAiSettings);

 Kernel kernel = builder.Build();

```

All the above is just setup code/overhead of using SK for this simple example (I know it probably seems like a lot).

Next comes the code that attempts to create the same functionality as the code we played with on Thursday night:

```C#
// output today's date just for fun
WriteLine($"\n----------------- DEBUG INFO -----------------");
var today = DateTime.Now.ToString("MMMM dd");
WriteLine($"Today is {today}");
WriteLine("----------------------------------------------");

IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// TODO: CHALLENGE 1: does the AI respond accurately to this prompt? How to fix?
var prompt = $"Tell me an interesting fact from world about an event " +
            $"that took place on {today}. " +
            $"Be sure to mention the date in history for context.";
                        
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    Temperature = 0.7f,
    MaxTokens = 250
};

var result = await chatCompletionService.GetChatMessageContentsAsync(prompt, openAIPromptExecutionSettings, kernel);

WriteLine($"\nPROMPT: \n\n{prompt}");

// Write out the result
foreach (var content in result)
{
    WriteLine($"\nRESPONSE:\n{content}");
}
```

The code uses the following utility method to set the color on the `Console.WriteLine` calls:
```C#
static void WriteLine(string message)
{
    var currentColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Green;
    
    Console.WriteLine(message);
    
    Console.ForegroundColor = currentColor;
}
```

If you run the code, you will get similar output as the code we played with Thursday night:

![SK Code](/img/2024-03-30_img2.jpg)

You can see the line that follows the date information is a logging statement (and not in my code specifically). In Bill's code, we had a statement at the end of the output that provided information on the tokens. However, using the result from the `GetChatMessageContentsAsync()` call does not return the result sent from OpenAI like Bill's code - so the information about tokens is not available. This is why the `Microsoft.SemanticKernel` logging level is set in the appsettings file.

![SK Logging](/img/2024-03-30_img3.jpg)

### Logging Code

If you go to the GitHub and download the code and run it, you get the output shown above by running this code:

```C#
 using var loggerFactory = LoggerFactory.Create(builder =>
 {
     builder.SetMinimumLevel(LogLevel.Information);

     builder.AddConfiguration(config);
     builder.AddConsole();
 });

```

As you can guess, the code above configures the logging factory, sets the log level based on the appsettings configuration files (in this case the `Microsoft.SemanticKernel` information level) - that is what is responsible for writing out the token information.

However, Semantic Kernel is setup to use [Open Telemetry](https://opentelemetry.io/) - so we can also get that information using a Console Exporter with Open Telemetry. I've commented out the code that does it, but you can clearly see the regions in the code yourself. When you uncomment those code blocks, the logging configuration looks like this:

```C#

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Microsoft.SemanticKernel*")
    .AddConsoleExporter()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);

    builder.AddOpenTelemetry(options =>
    {
        options.AddConsoleExporter();
        options.IncludeFormattedMessage = true;
    });

    builder.AddConfiguration(config);
    builder.AddConsole();
});

```

Now if you run it, you will get more information logged to the console:

![SK Open Telemetry](/img/2024-03-30_img4.jpg)

Open Telemetry is still on my list of technologies to learn more about, so all I can tell is there are LogRecords ouput as well as Metrics. Now that I know SK uses Open Telemetry I have a reason to spend some time digging into it as well. Stay tuned for more details on that in April.

## Conclusion

Implementing the same simple hello world example we did on Thursday night using Semantic Kernel really feels like keeping a sports car under the speed limit. It just feels like SK can do so much more ... and that is what I want to start digging into next.


If you have a comment, please message me @haleyjason on twitter/X.
