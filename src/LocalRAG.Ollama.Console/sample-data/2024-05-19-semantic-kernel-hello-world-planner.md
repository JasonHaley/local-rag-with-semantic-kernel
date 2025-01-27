A few weeks ago in the [Semantic Kernel Hello World Plugins Part 3](/2024/04/30/semantic-kernel-hello-world-plugin-part3/) blog entry, I showed how to use [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling). The last half of that entry was all about [how to view the response and request JSON](/2024/04/30/semantic-kernel-hello-world-plugin-part3/#requestandresponselogginghttpclienthandler-and-requestlogginghttpclienthandler) going back and forth to OpenAI, which detailed four API calls. In this entry I look at using the [Handlebars Planner](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Planners/Planners.Handlebars) to accomplish the same functionality. Then I'll show the request and response JSON for both using a saved plan as well as having the LLM create a plan and end with a token usage comparison.

The code for this entry is the [HelloWorld.Planner1.Console](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/src/HelloWorld.Planner1.Console) project in my GitHub repo.

# Planners

Semantic Kernel 1.0 has two planners:
* [Handlebars Planner](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Planners/Planners.Handlebars) - nuget package: [Microsoft.SemanticKernel.Planners.Handlebars](https://www.nuget.org/packages/Microsoft.SemanticKernel.Planners.Handlebars)
* [Function Calling Stepwise Planner](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Planners/Planners.OpenAI) - nuget package: [Microsoft.SemanticKernel.Planners.OpenAI](https://www.nuget.org/packages/Microsoft.SemanticKernel.Planners.OpenAI)

> NOTE: I'll be covering the Function Calling Stepwise planner in the next entry.

There is Microsoft Learn module that I recommend you check out: [Use intelligent planners](https://learn.microsoft.com/en-us/training/modules/use-intelligent-planners/), it walks through more detail than I cover in this entry.

I'd also recommend taking a look at the [Semantic Kernel documentation](https://learn.microsoft.com/en-us/semantic-kernel/agents/planners/?tabs=Csharp).

## So what does a planner do?

A planner takes a user request and creates or executes a plan. A plan is a set of steps SK can use to achieve what the user is asking for, given the current registered plugins. Plans can be created by the LLM or loaded from a text file. For our simple Hello World example, a plan may consist of these steps:
1. Retrieve the current day using the function DailyFactPlugin-GetCurrentDay.
2. Use the current day obtained in step 1 as input to the function DailyFactPlugin-GetDailyFact to get an interesting historic fact for the current date.
3. Return the fact obtained in step 2 as the final answer

Remember in [my last entry](/2024/04/30/semantic-kernel-hello-world-plugin-part3/) we used OpenAI's function calling to do those same steps, **however it was a black box - we did not know what steps it was going to take in the beginning**. With a planner, we can specify the steps or have the LLM create the plan first - giving us visibility to what steps need to be taken. That also means if the step can be done locally (for example a plugin function call in #1 above "Retrieve the current day using the function DailyFactPlugin-GetCurrentDay"), then unlike OpenAI function calling - the LLM doesn't need to be involved in a local function call.

## Handlebars Planner

With this planner, the plan uses the [Handlebars syntax](https://handlebarsjs.com/guide/) - which provides the ability to leverage the conditional logic and loops in the plan.

The plan for our Hello World sample looks like this:
```
{{!-- Step 0: Extract key values --}}
{{set "today" (DailyFactPlugin-GetCurrentDay)}}

{{!-- Step 1: Get today's daily fact --}}
{{set "dailyFact" (DailyFactPlugin-GetDailyFact today=today)}}

{{!-- Step 2: Print the daily fact with the date --}}
{{json (concat "On " today ", an interesting event took place: " dailyFact)}}
```

Now that you have an idea of what a planner is and what a Handlebars plan looks like, let's look at the code.

## The Code:

Like the previous Semantic Kernel Hello World entries, I've started with the same console app as before. The following two files are important for this blog:

* [/Extensions/HandlebarsPlannerExtensions.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Planner1.Console/Extensions/HandlebarsPlannerExtensions.cs) (a new file) - this has the extension methods that allows my code to be cleaner
* [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Planner1.Console/Program.cs) - this has the code to use the planner

### HandlebarsPlannerExtensions.cs

Since I wanted to create some code that I can reuse later, I refactored the logic to extension methods:

```C#
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace HelloWorld.Planner1.Console;

public static class HandlebarsPlannerExtensions
{
    // Load from an existing file or create a new plan and save it
    public async static Task<HandlebarsPlan> GetOrCreatePlanAsync(this HandlebarsPlanner planner, string filename, 
        Kernel kernel, string goal, KernelArguments? arguments = null)
    {
        if (Exists(filename))
        {
            return planner.Load(filename);
        }
        else
        {
            return await planner.CreateAndSavePlanAsync(filename, kernel, goal, arguments);
        }
    }
    // Create a new plan then save it
    public async static Task<HandlebarsPlan> CreateAndSavePlanAsync(this HandlebarsPlanner planner, string filename, 
        Kernel kernel, string goal, KernelArguments? arguments = null)
    {
        var plan = await planner.CreatePlanAsync(kernel, goal, arguments);

        plan.Save(filename);

        return plan;
    }

    public static bool Exists(string filename)
    {
        return File.Exists(filename);
    }

    public static HandlebarsPlan Load(this HandlebarsPlanner planner, string filename)
    {
        // Load the saved plan
        var savedPlan = File.ReadAllText(filename);

        // Populate intance
        return new HandlebarsPlan(savedPlan);
    }

    public static void Save(this HandlebarsPlan plan, string filename)
    {
        File.WriteAllText(filename, plan.ToString());
    }
}

```

The `GetOrCreatePlanAsync()` method is the extension method used in the Program.cs file. It abstracts the file checking, saving, and loading into a single method. If the plan file already exists, use it. Otherwise have the LLM create a plan, save it and return that new plan.

### Using the Handlebars Planner

In the Program.cs file, the top code is basically the same as the last entry:

```C#
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using HelloWorld.Plugin.Console.Plugins;
using HelloWorld.Plugin2.Console.Configuration;
using Microsoft.SemanticKernel.Planning.Handlebars;
using HelloWorld.Planner1.Console;

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
            builder.SetMinimumLevel(LogLevel.Trace);

            builder.AddConfiguration(config);
            builder.AddConsole();
        });

        // Configure Semantic Kernel
        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(loggerFactory);
        builder.AddChatCompletionService(openAiSettings);
        //builder.AddChatCompletionService(openAiSettings, ApiLoggingLevel.ResponseAndRequest); // use this line to see the JSON between SK and OpenAI
                
        // --------------------------------------------------------------------------------------
        // Exercise from Virtual Boston Azure for creating a prompt
        // --------------------------------------------------------------------------------------

        builder.Plugins.AddFromType<DailyFactPlugin>();
               
        Kernel kernel = builder.Build();
                
        // TODO: CHALLENGE 1: does the AI respond accurately to this prompt? How to fix?
        var prompt = $"Tell me an interesting fact from world about an event " +
            $"that took place on today's date. " +
            $"Be sure to mention the date in history for context.";
        
        .... 
    }
```
> NOTE: This code uses the same `DailyFactPlugin` and the same prompt we created in the last entry

To use the HandlebarsPlanner, we have to instantiate it and set the options, then call the extension method mentioned earlier to either load or create a new plan and invoke it to execute the plan.

```C#

        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });

        var plan = await planner.GetOrCreatePlanAsync("SavedPlan.hbs", kernel, prompt);
                
        WriteLine($"\nPLAN: \n\n{plan}");

        var result = await plan.InvokeAsync(kernel);

        WriteLine($"\nRESPONSE: \n\n{result}");

```

When you run the code, the output is pretty close to what the last entry's output was, except the handlebars plan in the output:

![Information Log](/img/2024-05-19_img1.jpg)

However, there were two calls to the LLM - the first used 1,500 tokens and the second used 157. If you run it again, it will use the saved plan and only have that second LLM call of ~150 tokens:

![Saved Plan Information Log](/img/2024-05-19_img2.jpg)

Now let's turn on the logging to see the JSON request and response.

### A look at the API calls

As seen above, there are two calls when there isn't a saved plan and only one call with the plan exists already.

#### No Saved Plan Requests/Responses

The first call is interesting because it shows the prompt used to have the LLM create the plan:

First request:
```JSON
{
  "messages": [
    {
      "content": "## Instructions\nExplain how to achieve the user\u0027s goal using the available helpers with a Handlebars .Net template.\n\n## Example\nIf the user posed the goal below, you could answer with the following template.",
      "role": "system"
    },
    {
      "content": "## Goal\nI want you to generate 10 random numbers and send them to another helper.",
      "role": "user"
    },
    {
      "content": "Here\u0027s a Handlebars template that achieves the goal:\n\u0060\u0060\u0060handlebars\n{{!-- Step 0: Extract key values --}}\n{{set\n  \u0022count\u0022\n  10\n}}\n{{!-- Step 1: Loop using the count --}}\n{{#each\n  (range\n    1\n    count\n  )\n}}\n  {{!-- Step 2: Create random number --}}\n  {{set\n    \u0022randomNumber\u0022\n    (Example-Random\n      seed=this\n    )\n  }}\n  {{!-- Step 3: Call example helper with random number and print the result to the screen --}}\n  {{set\n    \u0022result\u0022\n    (Example-Helper\n      input=randomNumber\n    )\n  }}\n  {{json (concat \u0022The result\u0022 \u0022 \u0022 \u0022is:\u0022 \u0022 \u0022 result)}}\n{{/each}}\n\u0060\u0060\u0060",
      "role": "assistant"
    },
    {
      "content": "Now let\u0027s try the real thing.",
      "role": "system"
    },
    {
      "content": "The following helpers are available to you:\n\n## Built-in block helpers\n- \u0060{{#if}}{{/if}}\u0060\n- \u0060{{#unless}}{{/unless}}\u0060\n- \u0060{{#each}}{{/each}}\u0060 - inside this block, you can use:\n    - \u0060this\u0060 to reference the element being iterated over\n    - \u0060@index\u0060 to reference the current index\n    - \u0060@key\u0060 to reference the current key (for object iteration)\n- \u0060{{#with}}{{/with}}\u0060\n\n## Loop helpers\nIf you need to loop through a list of values with \u0060{{#each}}\u0060, you can use the following helpers:\n- \u0060{{range}}\u0060 \u2013 Generates a list of integral numbers within a specified range, inclusive of the first and last value.\n- \u0060{{array}}\u0060 \u2013 Generates an array of values from the given values (zero-indexed).\n\nIMPORTANT: \u0060range\u0060 and \u0060array\u0060 are the only supported data structures. Others like \u0060hash\u0060 are not supported. Also, you cannot use any methods or properties on the built-in data structures.\n\n## Math helpers\nIf you need to do basic operations, you can use these two helpers with numerical values:\n- \u0060{{add}}\u0060 \u2013 Adds two values together.\n- \u0060{{subtract}}\u0060 \u2013 Subtracts the second value from the first.\n\n## Comparison helpers\nIf you need to compare two values, you can use the \u0060{{equals}}\u0060 helper.\nTo use the math and comparison helpers, you must pass in two positional values. For example, to check if the variable \u0060var\u0060 is equal to number \u00601\u0060, you would use the following helper like so: \u0060{{#if (equals var 1)}}{{/if}}\u0060.\n\n## Variable helpers\nIf you need to create or retrieve a variable, you can use the following helpers:\n- \u0060{{set}}\u0060 \u2013 Creates a variable with the given name and value. It does not print anything to the template, so you must use \u0060{{json}}\u0060 to print the value.\n- \u0060{{json}}\u0060 \u2013 Serializes the given value and prints result as JSON string.\n- \u0060{{concat}}\u0060 \u2013 Concatenates the given values into one string.\n\n## Custom helpers\nLastly, you have the following custom helpers to use.\n\n### \u0060DailyFactPlugin-GetCurrentDay\u0060\nDescription: Retrieves the current day.\nInputs:\nOutput: String\n\n### \u0060DailyFactPlugin-GetDailyFact\u0060\nDescription: Provides interesting historic facts for the current date.\nInputs:\n    - today: today-string - Current day (required)\nOutput: String\n\nIMPORTANT: You can only use the helpers that are listed above. Do not use any other helpers that are not explicitly listed here. For example, do not use \u0060{{log}}\u0060 or any \u0060{{Example}}\u0060 helpers, as they are not supported.\n",
      "role": "user"
    },
    {
      "content": "## Goal\nTell me an interesting fact from world about an event that took place on today\u0026#39;s date. Be sure to mention the date in history for context.",
      "role": "user"
    },
    {
      "content": "## Tips and reminders\n- Add a comment above each step to describe what the step does.\n- Each variable should have a well-defined name.\n- Be extremely careful about types. For example, if you pass an array to a helper that expects a number, the template will error out.\n- Each step should contain only one helper call.\n\n## Start\nFollow these steps to create one Handlebars template to achieve the goal:\n0. Extract Key Values:\n  - Read the goal and any user-provided content carefully and identify any relevant strings, numbers, or conditions that you\u0027ll need. Do not modify any data.\n  - When generating variables or helper inputs, only use content that the user has explicitly provided or confirmed. If the user did not explicitly provide specific information, you should not invent or assume this information.\n  - Use the \u0060{{set}}\u0060 helper to create a variable for each key value.\n  - Omit this step if no values are needed from the initial context.\n1. Choose the Right Helpers:\n  - Use the provided helpers to manipulate the variables you\u0027ve created. Start with the basic helpers and only use custom helpers if necessary to accomplish the goal.\n  - Be careful with syntax, i.e., Always reference a custom helper by its full name and remember to use a \u0060#\u0060 for all block helpers.\n2. Don\u0027t Create or Assume Unlisted Helpers:\n  - Only use the helpers provided. Any helper not listed is considered hallucinated and must not be used.\n  - Do not invent or assume the existence of any functions not explicitly defined above.\n3. What if I Need More Helpers?\n  - Stop here if the goal cannot be fully achieved with the provided helpers or you need a helper not defined, and just return a string with an appropriate error message.\n4. Keep It Simple:\n  - Avoid using loops or block expressions. They are allowed but not always necessary, so try to find a solution that does not use them.\n  - Your template should be intelligent and efficient, avoiding unnecessary complexity or redundant steps.\n5. No Nested Helpers:\n  - Do not nest helpers or conditionals inside other helpers. This can cause errors in the template.\n6. Output the Result:\n  - Once you have completed the necessary steps to reach the goal, use the \u0060{{json}}\u0060 helper and print only your final template.\n  - Ensure your template and all steps are enclosed in a \u0060\u0060\u0060 handlebars block.\n\nRemember, the objective is not to use all the helpers available, but to use the correct ones to achieve the desired outcome with a clear and concise template.\n",
      "role": "system"
    }
  ],
  "temperature": 1,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt-3.5-turbo-1106"
}
```
First response from LLM with the plan
```JSON
{
  "id": "chatcmpl-9QhiNtPUUsFGBQjjeaea4B13ZaK46",
  "object": "chat.completion",
  "created": 1716151595,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "\u0060\u0060\u0060handlebars\n{{! Step 0: Extract Key Values }}\n{{set \u0022today\u0022 (DailyFactPlugin-GetCurrentDay)}}\n\n{{! Step 1: Get the daily fact for today\u0027s date }}\n{{set \u0022fact\u0022 (DailyFactPlugin-GetDailyFact today=today)}}\n\n{{! Step 2: Output the result as JSON}}\n{{json fact}}\n\u0060\u0060\u0060"
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 1404,
    "completion_tokens": 78,
    "total_tokens": 1482
  },
  "system_fingerprint": null
}
```
Second request is the actual LLM call for the daily fact with the day already plugged in the prompt:
```JSON
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world \r\n        about an event that took place on May 19.\r\n        Be sure to mention the date in history for context.",
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
Second response is the final answer with the daily fact:
```JSON
{
  "id": "chatcmpl-9QhiO2wkbHhhpSH1e53srRb3OuHIj",
  "object": "chat.completion",
  "created": 1716151596,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "On May 19, 1780, a mysterious event known as the \u0022Dark Day\u0022 occurred in New England and parts of Canada. The day began as bright and sunny, but by mid-morning, the sky became clouded over and darkness descended upon the region. It was reported that candles were needed to see indoors, and the darkness lasted well into the night. The cause of the event is still not fully understood, but it is believed to have been caused by a combination of fog, smoke, and ash from forest fires, as well as possibly a thick cloud cover. The \u0022Dark Day\u0022 was a significant and eerie event that left many people puzzled and frightened at the time."
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 39,
    "completion_tokens": 139,
    "total_tokens": 178
  },
  "system_fingerprint": null
}
```

#### Saved Plan Request/Response

As you would expect, when there is already a saved plan - the single request and response are just like the second call above. That saves the large initial request to create the plan.

Request
```JSON
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world \r\n        about an event that took place on May 19.\r\n        Be sure to mention the date in history for context.",
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

Response
```JSON
{
  "id": "chatcmpl-9QiDPllAFJ9Fe15bNPa8UixQ0fgsZ",
  "object": "chat.completion",
  "created": 1716153519,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "On May 19, 1780, a mysterious event known as the \u0022Dark Day\u0022 occurred in New England and parts of eastern Canada. The sky became incredibly dark during the day, so much so that candles were needed to see indoors. Many people believed it to be an act of God or a sign of the impending apocalypse. It was later determined that the darkness was caused by a combination of fog, smoke, and ash from forest fires that were burning in the region at the time. The \u0022Dark Day\u0022 remains a unique and fascinating event in history."
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 39,
    "completion_tokens": 113,
    "total_tokens": 152
  },
  "system_fingerprint": null
}
```

# Conclusion

In this entry, I used the `HandlebarsPlanner` to provide the same functionality as the previous entries and looked at the differences between using a saved plan vs. creating a plan. It is that if you can use a saved `HandlebarsPlan` you will save time and tokens.

## Token Usage Comparison

If we compare the tokens used in the last entry, which used OpenAI function calling and the two scenarios in this entry (no saved plan and a saved plan) the numbers look like this:


| Scenario                | Approximate Total Tokens Used |
|-------------------------|-------------------------------|
| OpenAI Function Calling |   755                         |
| No Saved Handlebars Plan| 1,660                         |
| Saved Handlebars Plan   |   152                         |




If you have a comment, please message me @haleyjason on twitter/X.
