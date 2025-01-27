Last week I blogged [Part 2](/2024/04/26/semantic-kernel-hello-world-plugin-part2/) showing the creation of a native function plugin, in this post I want to take that native function a step further and use the [OpenAI Function calling](https://platform.openai.com/docs/guides/function-calling). This will allow us to not provide the current date when making the call to get a historic daily fact and have OpenAI call a function to get the current date.

I've added the [HelloWorld.Plugin3.Console](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/src/HelloWorld.Plugin3.Console) project to the GitHub repo for the code in this blog entry.

# Semantic Kernel Plugin: Native Function and OpenAI Function Calling

Function calling is also called "external tools". I mentioned it in [Demo Review: Simple RAG using Blazor, SQL Server, Azure OpenAI and Function Calling](https://jasonhaley.com/2024/02/11/simple-rag-sql-openai-functioncall/) and how it changes the flow a bit. Instead of calling OpenAI and expecting an answer, we pass a question and in addition we give it information about functions they can call and what they do, then have the LLM decide what functions to call and what information to pass to them, then we return those results back to the LLM and so on until it has enough information to answer.

> NOTE: The descriptions on the function and arguments are **very important** 

Since not all OpenAI models support function calling, you will need to verify in the [docs](https://platform.openai.com/docs/guides/function-calling) that model version you are using supports it. 

Just a reminder of what we are looking to reproduce: a hello world example that calls OpenAI with a simple prompt with today's day and return a historical fact about the date - **however in this entry we are going to let OpenAI figure out what today's day is**. The output should be similar to this: 

![Bills Code](/img/2024-03-30_img1.jpg)

## The Code:

The code is in the [GitHub repo](/2024/03/30/semantic-kernel-hello-world/) and like the other blog entries, I created a console app just for the code in this one to make it easier to focus on one thing at a time.

The following two files are important for this blog:

* [/Plugins/DailyFactPlugin.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin3.Console/Plugins/DailyFactPlugin.cs) - added an additional function to retrieve the current day
* [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin3.Console/Program.cs) - this has the code to utilize the new native function
* [/Configuration/ServiceCollectionExtensions.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin3.Console/Configuration/ServiceCollectionExtensions.cs) - added new extension and utilities for logging JSON going back and forth between OpenAI or Azure OpenAI.

### DailyFactPlugin

This is mostly the same as Part 2 with some additions.

```C#
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HelloWorld.Plugin.Console.Plugins;

public class DailyFactPlugin
{
    private const string DESCRIPTION = "Provides interesting historic facts for the current date.";
    private const string TEMPLATE = @"Tell me an interesting fact from world 
        about an event that took place on {{$today}}.
        Be sure to mention the date in history for context.";
    private const string GET_DAILY_FACT_FUNC = "GetDailyFactFunc";
    internal const string PLUGIN_NAME = "DailyFactPlugin";
    internal const string GET_DAILY_FACT = "GetDailyFact";

    private readonly KernelFunction _dailyFact;
    private readonly KernelFunction _currentDay;
    
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
            executionSettings: settings);
        
        _currentDay = KernelFunctionFactory.CreateFromMethod(() => DateTime.Now.ToString("MMMM dd"), "GetCurrentDay");
    }
    
    [KernelFunction, Description(DESCRIPTION)]
    public async Task<string> GetDailyFact([Description("Current day"), Required] string today, Kernel kernel)
    {
        var result = await _dailyFact.InvokeAsync(kernel, new() { ["today"] = today }).ConfigureAwait(false);

        return result.ToString();
    }

    [KernelFunction, Description("Retrieves the current day.")]
    public async Task<string> GetCurrentDay(Kernel kernel)
    {
        var today = await _currentDay.InvokeAsync(kernel);

        return today.ToString();
    }
}

```

I added a member level variable for the new KernelFunction

```C#
private readonly KernelFunction _currentDay;
```

In the constructor, I initialized the `_currentDay` creating a `KernelFunction` from an inline method with its name and description

```C#
_currentDay = KernelFunctionFactory.CreateFromMethod(() => DateTime.Now.ToString("MMMM dd"), "GetCurrentDay", "Retrieves the current day.");
```

I also modified the `GetDailyFact` signature to indicate the `today` parameter is required with the `[Required]` attribute, which wasn't necessary before due to the code in Program.cs calling the method directly with the arguments.

```C#
public async Task<string> GetDailyFact([Description("Current day"), Required] string today, Kernel kernel)
```

The `GetCurrentDay` method is the new `KernelFunction` that will return the current day in the format we want by invoking the `_currentDay` setup in the constructor.

```C#
[KernelFunction, Description("Retrieves the current day.")]
public async Task<string> GetCurrentDay(Kernel kernel)
{
    var today = await _currentDay.InvokeAsync(kernel);

    return today.ToString();
}
```

### Using the Native Function

In the Program.cs file the top code hasn't really changed, and it still just does the configuration:

```C#

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using HelloWorld.Plugin.Console.Plugins;
using HelloWorld.Plugin2.Console.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
        builder.AddChatCompletionService(openAiSettings);
        
        ....
    }
}

```
> NOTE: the last line in the above code has changed a little (more on that later)

To load the `DailyFactPlugin`, I add it to the Plugins on the `KernelBuilder` (this is just like the last entry):

```C#
        builder.Plugins.AddFromType<DailyFactPlugin>();

        Kernel kernel = builder.Build();
```

The prompt we want to use (compared to the [first Hello World example](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/Program.cs) on the right), is where this setup is a little different. We a not passing in the date, ***we are asking the LLM to use today's date***.
![Prompt](/img/2024-04-30_img1.jpg)

Also like in the first Hello World example, we setup the `OpenAIPromptExecutionSettings` and this time need to set the ToolCallBehavior to `ToolCallBehavior.AutoInvokeKernelFunctions`

```C#
        OpenAIPromptExecutionSettings settings = new() 
        { 
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, 
            Temperature = 0.7f,
            MaxTokens = 250
        };
```

To cause the conversation to get started (so to speak), we invoke the prompt on the kernel:

```C#
        var funcresult = await kernel.InvokePromptAsync(prompt, new KernelArguments(settings));
 
        WriteLine($"\nRESPONSE: \n\n{funcresult}");        
    
```

When you run the code there are more info entries than last time, but the output is pretty similar:

![Output](/img/2024-04-30_img2.jpg)

### Lesson Learned on Logging

As I mention in [Part 1](/2024/04/11/semantic-kernel-hello-world-plugin-part1/), if you turn on Trace logging you will get more information about what is going on. Here is some of it:

![Trace Output](/img/2024-04-30_img3.jpg)

The extra tracing information is nice, but **wouldn't it be nice to see the JSON in the API calls going back and forth?** This is where that little change in the `builder.AddChatCompletionService(openAiSettings);` line earlier comes into play.

#### RequestAndResponseLoggingHttpClientHandler and RequestLoggingHttpClientHandler

In the [ServiceCollectionExtensions.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin3.Console/Configuration/ServiceCollectionExtensions.cs) file, there is some new logic to provide the ability to log out the JSON being passed in the Request and/or Response to the LLM's API.

There is an `enum` that allows you to indicate what level of logging you'd like to see - the default is None.

```C#
public enum ApiLoggingLevel
{
    None = 0,
    RequestOnly = 1,
    ResponseAndRequest = 2,
}
```

In order to use the overloads on the `IKernelBuilder` that take in an `HttpClient` I needed to use methods on it instead of the `IServiceCollection`. The new extension method is:

```C#
internal static class IKernelBuilderExtensions
{
    internal static IKernelBuilder AddChatCompletionService(this IKernelBuilder kernelBuilder, OpenAIOptions openAIOptions, ApiLoggingLevel apiLoggingLevel = ApiLoggingLevel.None)
    {
        switch (openAIOptions.Source)
        {
            case "AzureOpenAI":
                {
                    if (apiLoggingLevel == ApiLoggingLevel.None)
                    {
                        kernelBuilder = kernelBuilder.AddAzureOpenAIChatCompletion(openAIOptions.ChatDeploymentName, endpoint: openAIOptions.Endpoint,
                            apiKey: openAIOptions.ApiKey, serviceId: openAIOptions.ChatModelId);
                    }
                    else
                    {
                        var client = CreateHttpClient(apiLoggingLevel);
                        kernelBuilder.AddAzureOpenAIChatCompletion(openAIOptions.ChatDeploymentName, openAIOptions.Endpoint, openAIOptions.ApiKey, null, null, client);
                    }
                    break;
                }
            case "OpenAI":
                {
                    if (apiLoggingLevel == ApiLoggingLevel.None)
                    {
                        kernelBuilder = kernelBuilder.AddOpenAIChatCompletion(modelId: openAIOptions.ChatModelId, apiKey: openAIOptions.ApiKey);
                        break;
                    }
                    else
                    {
                        var client = CreateHttpClient(apiLoggingLevel);
                        kernelBuilder.AddOpenAIChatCompletion(openAIOptions.ChatModelId, openAIOptions.ApiKey, null, null, client);
                    }
                    break;
                }
            default:
                throw new ArgumentException($"Invalid source: {openAIOptions.Source}");
        }

        return kernelBuilder;
    }

    private static HttpClient CreateHttpClient(ApiLoggingLevel apiLoggingLevel)
    {
        HttpClientHandler httpClientHandler;
        if (apiLoggingLevel == ApiLoggingLevel.RequestOnly)
        {
            httpClientHandler = new RequestLoggingHttpClientHandler();
        }
        else
        {
            httpClientHandler = new RequestAndResponseLoggingHttpClientHandler();
        }
        var client = new HttpClient(httpClientHandler);
        return client;
    }
}

```

As you can see, there are two HttpHandlers that will either log the Request or the Request and Response. The code is inspired by this GitHub issue [.Net: Document OpenAI function calling JSON Schema support](https://github.com/microsoft/semantic-kernel/issues/5107) by [Frederik Rosenberg](https://github.com/frederikrosenberg) that I found really handy.

The code for the two handlers:
```C#
public class RequestAndResponseLoggingHttpClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            var json = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonDocument>(content),
                new JsonSerializerOptions { WriteIndented = true });
            System.Console.WriteLine("***********************************************");
            System.Console.WriteLine("Request:");
            System.Console.WriteLine(json);
        }

        var result = await base.SendAsync(request, cancellationToken);

        if (result.Content is not null)
        {
            var content = await result.Content.ReadAsStringAsync(cancellationToken);
            var json = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonDocument>(content),
                new JsonSerializerOptions { WriteIndented = true });
            System.Console.WriteLine("***********************************************");
            System.Console.WriteLine("Response:");
            System.Console.WriteLine(json);
        }

        return result;
    }
}
public class RequestLoggingHttpClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            var json = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonDocument>(content),
                new JsonSerializerOptions { WriteIndented = true });
            System.Console.WriteLine("***********************************************");
            System.Console.WriteLine("Request:");
            System.Console.WriteLine(json);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

> NOTE: These are handy for debugging purposes, if you wanted to do something like this in a production application you'd want to modify them a bit.

In order to change the logging, you need to modify the line 38 in [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin3.Console/Program.cs) from:

```C#
        builder.AddChatCompletionService(openAiSettings);
````
 
 to 
 
```C#
        builder.AddChatCompletionService(openAiSettings, ApiLoggingLevel.ResponseAndRequest);
````

Now when you run it again, you get to see the full JSON going back and forth - which also gives you a good idea of what is really going on under the hood.

The first request to the API shows the user message with the `[KernelFunction]` schema descriptions:
```JSON
Request:
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world about an event that took place on today\u0027s date. Be sure to mention the date in history for context.",
      "role": "user"
    }
  ],
  "max_tokens": 250,
  "temperature": 0.7,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt-3.5-turbo-1106",
  "tools": [
    {
      "function": {
        "name": "DailyFactPlugin-GetDailyFact",
        "description": "Provides interesting historic facts for the current date.",
        "parameters": {
          "type": "object",
          "required": [
            "today"
          ],
          "properties": {
            "today": {
              "type": "string",
              "description": "Current day"
            }
          }
        }
      },
      "type": "function"
    },
    {
      "function": {
        "name": "DailyFactPlugin-GetCurrentDay",
        "description": "Retrieves the current day.",
        "parameters": {
          "type": "object",
          "required": [],
          "properties": {}
        }
      },
      "type": "function"
    }
  ],
  "tool_choice": "auto"
}
```
The response from the API shows the choices array having a **tool_calls** array for the `GetCurrentDay` function on the `DailyFactPlugin` and a **finish_reason** = "tool_calls":

```JSON
Response:
{
  "id": "chatcmpl-9K4mdUZPbp1PJvpULSIBGXCYkZyi4",
  "object": "chat.completion",
  "created": 1714571975,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": null,
        "tool_calls": [
          {
            "id": "call_C5rmd3huqt3gWsTV3BHi6tK0",
            "type": "function",
            "function": {
              "name": "DailyFactPlugin-GetCurrentDay",
              "arguments": "{}"
            }
          }
        ]
      },
      "logprobs": null,
      "finish_reason": "tool_calls"
    }
  ],
  "usage": {
    "prompt_tokens": 104,
    "completion_tokens": 15,
    "total_tokens": 119
  },
  "system_fingerprint": "fp_b953e4de39"
}
```

If you have the `Trace` logging on, you'll see Semantic Kernel takes care of handling the response and calling the `GetCurrentDay` function, then taking the result to again call the API:

![Handle First Response](/img/2024-04-30_img5.jpg)

The second request to the API has a new message in the `message` array with the `role`="tool" and the content of today's date "May 01", which was the result of the function.

```JSON
Request:
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world about an event that took place on today\u0027s date. Be sure to mention the date in history for context.",
      "role": "user"
    },
    {
      "content": null,
      "tool_calls": [
        {
          "function": {
            "name": "DailyFactPlugin-GetCurrentDay",
            "arguments": "{}"
          },
          "type": "function",
          "id": "call_rOuuLodebcK1rrtvHf1z3DKq"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "May 01",
      "tool_call_id": "call_rOuuLodebcK1rrtvHf1z3DKq",
      "role": "tool"
    }
  ],
  "max_tokens": 250,
  "temperature": 0.7,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt-3.5-turbo-1106",
  "tools": [
    {
      "function": {
        "name": "DailyFactPlugin-GetDailyFact",
        "description": "Provides interesting historic facts for the current date.",
        "parameters": {
          "type": "object",
          "required": [
            "today"
          ],
          "properties": {
            "today": {
              "type": "string",
              "description": "Current day"
            }
          }
        }
      },
      "type": "function"
    },
    {
      "function": {
        "name": "DailyFactPlugin-GetCurrentDay",
        "description": "Retrieves the current day.",
        "parameters": {
          "type": "object",
          "required": [],
          "properties": {}
        }
      },
      "type": "function"
    }
  ],
  "tool_choice": "auto"
}
```

The second response from the API, is another `tool_calls` array and  `finish_reason` = "tool_calls". This time it is to call the `GetDailyFact` on the `DailyFactPlugin` with the encoded arguments of `{ "today": "May 01" }`

```JSON
Response:
{
  "id": "chatcmpl-9K4qAUQx4ekxxKhKyAb8Hljahseex",
  "object": "chat.completion",
  "created": 1714572194,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": null,
        "tool_calls": [
          {
            "id": "call_vpbIr0MWEtZMgQT0VuYOYARx",
            "type": "function",
            "function": {
              "name": "DailyFactPlugin-GetDailyFact",
              "arguments": "{\u0022today\u0022:\u0022May 01\u0022}"
            }
          }
        ]
      },
      "logprobs": null,
      "finish_reason": "tool_calls"
    }
  ],
  "usage": {
    "prompt_tokens": 134,
    "completion_tokens": 22,
    "total_tokens": 156
  },
  "system_fingerprint": "fp_482d920018"
}
```

In the second response, SK invokes the `GetDailyFact` function passing the date, which then causes another request to the API:

![Handle Second Response](/img/2024-04-30_img4.jpg)

The third request to the API is a pretty simple prompt being sent to get a daily fact, very similar to the [first Hello World example](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Console/Program.cs). 

```JSON
Request:
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world \r\n        about an event that took place on May 01.\r\n        Be sure to mention the date in history for context.",
      "role": "user"
    }
  ],
  "temperature": 0.7,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt-3.5-turbo-1106"
}
```
> NOTE: This call is just the `GetDailyFact` plugin method executing, it does not have all the conversation of the original request

The response from the `GetDailyFact` is what we would expect for the prompt being passed:

```JSON
Response:
{
  "id": "chatcmpl-9K4qBEjOAUBTOkAxKQiOVhPEBZr2h",
  "object": "chat.completion",
  "created": 1714572195,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "On May 1, 1931, the Empire State Building in New York City was officially opened. At 102 stories, it was the tallest building in the world at the time and held that title for nearly 40 years. The building\u0027s construction was completed in just 410 days, a remarkable feat considering the technology available at the time. Today, the Empire State Building remains an iconic symbol of New York City and a popular tourist attraction."
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 39,
    "completion_tokens": 90,
    "total_tokens": 129
  },
  "system_fingerprint": "fp_482d920018"
}
```

Now that the `GetDailyFact` plugin method returned, SK will take the result and continue with the original call.

![Handle Third Response](/img/2024-04-30_img6.jpg)

The fourth (and last) request to the API now includes the tool message with the response from the `GetDailyFact` plugin method - along with all the other history of this call:

```JSON
Request:
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world about an event that took place on today\u0027s date. Be sure to mention the date in history for context.",
      "role": "user"
    },
    {
      "content": null,
      "tool_calls": [
        {
          "function": {
            "name": "DailyFactPlugin-GetCurrentDay",
            "arguments": "{}"
          },
          "type": "function",
          "id": "call_rOuuLodebcK1rrtvHf1z3DKq"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "May 01",
      "tool_call_id": "call_rOuuLodebcK1rrtvHf1z3DKq",
      "role": "tool"
    },
    {
      "content": null,
      "tool_calls": [
        {
          "function": {
            "name": "DailyFactPlugin-GetDailyFact",
            "arguments": "{\u0022today\u0022:\u0022May 01\u0022}"
          },
          "type": "function",
          "id": "call_vpbIr0MWEtZMgQT0VuYOYARx"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "On May 1, 1931, the Empire State Building in New York City was officially opened. At 102 stories, it was the tallest building in the world at the time and held that title for nearly 40 years. The building\u0027s construction was completed in just 410 days, a remarkable feat considering the technology available at the time. Today, the Empire State Building remains an iconic symbol of New York City and a popular tourist attraction.",
      "tool_call_id": "call_vpbIr0MWEtZMgQT0VuYOYARx",
      "role": "tool"
    }
  ],
  "max_tokens": 250,
  "temperature": 0.7,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt-3.5-turbo-1106",
  "tools": [
    {
      "function": {
        "name": "DailyFactPlugin-GetDailyFact",
        "description": "Provides interesting historic facts for the current date.",
        "parameters": {
          "type": "object",
          "required": [
            "today"
          ],
          "properties": {
            "today": {
              "type": "string",
              "description": "Current day"
            }
          }
        }
      },
      "type": "function"
    },
    {
      "function": {
        "name": "DailyFactPlugin-GetCurrentDay",
        "description": "Retrieves the current day.",
        "parameters": {
          "type": "object",
          "required": [],
          "properties": {}
        }
      },
      "type": "function"
    }
  ],
  "tool_choice": "auto"
}
```

The final response there is just the assistant message with the content we would expect and a **finish_reason** = "stop"

```JSON
Response:
{
  "id": "chatcmpl-9K4qEm0zdHPZbULEh1ztSu5rJsLhj",
  "object": "chat.completion",
  "created": 1714572198,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "On May 1st, 1931, the Empire State Building in New York City was officially opened. At 102 stories, it was the tallest building in the world at the time and held that title for nearly 40 years. The building\u0027s construction was completed in just 410 days, a remarkable feat considering the technology available at the time. Today, the Empire State Building remains an iconic symbol of New York City and a popular tourist attraction."
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 259,
    "completion_tokens": 92,
    "total_tokens": 351
  },
  "system_fingerprint": "fp_482d920018"
}
```

## Conclusion

In this entry, I showed how to use [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling) with our simple Hello World example as well as the detail of what is going on behind the scene with the API calls and Semantic Kernel.

If you have a comment, please message me @haleyjason on twitter/X.
