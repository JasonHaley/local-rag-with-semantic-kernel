Last week in the [Semantic Kernel Hello World Planners Part 1](/2024/05/19/semantic-kernel-hello-world-planners-part1/) entry, I used the [Handlebars planner](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Planners/Planners.Handlebars) to implement the sample Hello World functionality and then looked at the token difference between using a saved plan vs. generating a plan. In this entry I use the [Function Calling Stepwise Planner](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Planners/Planners.OpenAI) to create the sample Hello World functionality and compare it to the implementation in the [Semantic Kernel Hello World Plugins Part 3](/2024/04/30/semantic-kernel-hello-world-plugin-part3/) entry.

The code for this entry is the [HelloWorld.Planner2.Console](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/src/HelloWorld.Planner2.Console) project in my GitHub repo.

# Function Calling Stepwise Planner

As mentioned last week, the [Function Calling Stepwise Planner](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Planners/Planners.OpenAI) is in the [Microsoft.SemanticKernel.Planners.OpenAI](https://www.nuget.org/packages/Microsoft.SemanticKernel.Planners.OpenAI) nuget package. When we used functions a few weeks ago, we used the `AutoInvokeKernelFunctions` setting - which allowed the LLM to more or less act as a planner. There is also another "manual" mode using `EnableKernelFunctions` that requires you to handle the tool (ie. function calling) requests and responses manually in your code. A good example of how to do that can be found in this blog: [Manual function calling with Semantic Kernel and OpenAI](https://systenics.ai/blog/2024-03-27-manual-function-calling-with-semantic-kernel/). As I'm sure you can guess, the code to handle the manual scenario is more involved. Simplifying that scenario is what this planner does.

## What does this planner do?

This planner takes the user request (typically called a "goal") and all the metadata of the registered functions and calls OpenAI to get a plan for accomplishing the user's goal. For our Hello World example, the plan looks like this:

1. Retrieve the current day using the function DailyFactPlugin-GetCurrentDay.
2. Use the current day obtained in Step 1 as input to the function DailyFactPlugin-GetDailyFact to get an interesting historic fact for the current date.
3. Send the fact obtained in Step 2 as the final answer using the function UserInteraction-SendFinalAnswer.

We'll take a look at this in more detail soon. First, let's look at the code.

## The Code:

Like the previous Semantic Kernel Hello World entries, I've started with the same console app as before. The following file is important for this blog:

* [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Planner2.Console/Program.cs) - this has the code to use the planner

### Using the Function Calling Stepwise Planner

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
using Microsoft.SemanticKernel.Planning;
using System.Text.Json;

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
> NOTE: This code uses the same `DailyFactPlugin` and the same prompt we created in a previous blog entry

To use the FunctionCallingStepwisePlanner, we have to instantiate it and set the options, then call the `ExecuteAsyc()` method passing the prompt.

```C#

        var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions
        {
            MaxIterations = 15,
            MaxTokens = 2500,
        });

        var result = await planner.ExecuteAsync(kernel, prompt);

        WriteLine($"\nRESPONSE: \n\n{result}");

```

> NOTE: I had to increase the MaxTokens for this to work. You will get an exception due to the ChatHistory getting too big if you leave it at 250

When you run the code, the output is close to what the last entry's output:

![Information Log](/img/2024-05-27_img1.jpg)

Using this planner for the Hello World sample, it makes five calls to the LLM - this is closer to the API interaction we saw with the [function calling implementation](/2024/04/30/semantic-kernel-hello-world-plugin-part3/). All those calls add up to a total of 1,869 tokens (vs. 1,660 with the Handlebars planner or 755 for the OpenAI function calling).

Unlike the Handlebars planner, there isn't a simple way to save the plan for reloading later. There also isn't an easy way to see the plan before it gets executed.

Now let's turn on the logging to see the JSON request and response.

### A look at the API calls

As seen above, there are five calls - let's take a look at the JSON going back and forth.

The first call is interesting because it shows the prompt used to have the LLM create the plan:

#### First request:
```JSON
{
  "messages": [
    {
      "content": "You are an expert at generating plans from a given GOAL. Think step by step and determine a plan to satisfy the specified GOAL using only the FUNCTIONS provided to you. You can also make use of your own knowledge while forming an answer but you must not use functions that are not provided. Once you have come to a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.\n\n[FUNCTIONS]\n\n[{\u0022name\u0022:\u0022DailyFactPlugin-GetCurrentDay\u0022,\u0022description\u0022:\u0022Retrieves the current day.\u0022,\u0022parameters\u0022:{\u0022type\u0022:\u0022object\u0022,\u0022required\u0022:[],\u0022properties\u0022:{}},\u0022responses\u0022:{}},{\u0022name\u0022:\u0022DailyFactPlugin-GetDailyFact\u0022,\u0022description\u0022:\u0022Provides interesting historic facts for the current date.\u0022,\u0022parameters\u0022:{\u0022type\u0022:\u0022object\u0022,\u0022required\u0022:[\u0022today\u0022],\u0022properties\u0022:{\u0022today\u0022:{\u0022type\u0022:\u0022string\u0022,\u0022description\u0022:\u0022Current day\u0022}}},\u0022responses\u0022:{}},{\u0022name\u0022:\u0022UserInteraction-SendFinalAnswer\u0022,\u0022description\u0022:\u0022This function is used to send the final answer of a plan to the user.\u0022,\u0022parameters\u0022:{\u0022type\u0022:\u0022object\u0022,\u0022required\u0022:[\u0022answer\u0022],\u0022properties\u0022:{\u0022answer\u0022:{\u0022type\u0022:\u0022string\u0022,\u0022description\u0022:\u0022The final answer\u0022}}},\u0022responses\u0022:{}}]\n\n[END FUNCTIONS]\n\nTo create the plan, follow these steps:\n0. Each step should be something that is capable of being done by the list of available functions.\n1. Steps can use output from one or more previous steps as input, if appropriate.\n2. The plan should be as short as possible.",
      "role": "system"
    },
    {
      "content": "Tell me an interesting fact from world about an event that took place on today\u0027s date. Be sure to mention the date in history for context.",
      "role": "user"
    }
  ],
  "max_tokens": 256,
  "temperature": 0,
  "top_p": 0,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt-3.5-turbo-1106"
}

```
#### First response from LLM with the plan
```JSON
{
  "id": "chatcmpl-9TaATcwg4eANSN58r9XVuNTwZywa3",
  "object": "chat.completion",
  "created": 1716837569,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Step 1: Retrieve the current day using the DailyFactPlugin-GetCurrentDay function.\nStep 2: Use the retrieved current day as input to the DailyFactPlugin-GetDailyFact function to get an interesting historic fact for the current date.\nStep 3: Send the retrieved fact as the final answer using the UserInteraction-SendFinalAnswer function."
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 335,
    "completion_tokens": 72,
    "total_tokens": 407
  },
  "system_fingerprint": null
}
```
This first request/response was all about having the LLM create a plan for the planner to execute. In this case it is now going to start a flow that is very similar to the calls in the [OpenAI function calling](/2024/04/30/semantic-kernel-hello-world-plugin-part3/) implementation. The next four request/response pairs are almost the same as the function calling requests/responses.

### Second request is the beginning of the plan execution

The response has a system message which contains the plan for the LLM to follow along with a user message that you'll see after each step informing the LLM to continue on with the next step or to call the `UserInteraction-SendFinalAnswer` function if it is complete. 

```JSON
{
  "messages": [
    {
      "content": "Original request: Tell me an interesting fact from world about an event that took place on today\u0026#39;s date. Be sure to mention the date in history for context.\n\nYou are in the process of helping the user fulfill this request using the following plan:\nStep 1: Retrieve the current day using the DailyFactPlugin-GetCurrentDay function.\nStep 2: Use the retrieved current day as input to the DailyFactPlugin-GetDailyFact function to get an interesting historic fact for the current date.\nStep 3: Send the retrieved fact as the final answer using the UserInteraction-SendFinalAnswer function.\n\nThe user will ask you for help with each step.",
      "role": "system"
    },
    {
      "content": "Perform the next step of the plan if there is more work to do. When you have reached a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.",
      "role": "user"
    }
  ],
  "temperature": 1,
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
    },
    {
      "function": {
        "name": "UserInteraction-SendFinalAnswer",
        "description": "This function is used to send the final answer of a plan to the user.",
        "parameters": {
          "type": "object",
          "required": [
            "answer"
          ],
          "properties": {
            "answer": {
              "type": "string",
              "description": "The final answer"
            }
          }
        }
      },
      "type": "function"
    }
  ],
  "tool_choice": "auto"
}
```

> NOTE: the `UserInteraction-SendFinalAnswer` function is added by the `FunctionCallingStepwisePlanner`.

#### Second response

The response is a request to call the `GetCurrentDay` function on the `DailyFactPlugin`:

```JSON
{
  "id": "chatcmpl-9TaAV1jgyXbi6MLc4xGHuGeYyxTX2",
  "object": "chat.completion",
  "created": 1716837571,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": null,
        "tool_calls": [
          {
            "id": "call_WZ5ljfhIJ7rAJbUyBUvnOnV0",
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
    "prompt_tokens": 290,
    "completion_tokens": 15,
    "total_tokens": 305
  },
  "system_fingerprint": null
}
```

#### Third Request

This request has added the response from the `GetCurrentDay` as a `tool` message. Another instance of that user message indicating the LLM should move on to the next step or call the `SendFinalAnswer` function.

```JSON
{
  "messages": [
    {
      "content": "Original request: Tell me an interesting fact from world about an event that took place on today\u0026#39;s date. Be sure to mention the date in history for context.\n\nYou are in the process of helping the user fulfill this request using the following plan:\nStep 1: Retrieve the current day using the DailyFactPlugin-GetCurrentDay function.\nStep 2: Use the retrieved current day as input to the DailyFactPlugin-GetDailyFact function to get an interesting historic fact for the current date.\nStep 3: Send the retrieved fact as the final answer using the UserInteraction-SendFinalAnswer function.\n\nThe user will ask you for help with each step.",
      "role": "system"
    },
    {
      "content": "Perform the next step of the plan if there is more work to do. When you have reached a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.",
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
          "id": "call_WZ5ljfhIJ7rAJbUyBUvnOnV0"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "May 27",
      "tool_call_id": "call_WZ5ljfhIJ7rAJbUyBUvnOnV0",
      "role": "tool"
    },
    {
      "content": "Perform the next step of the plan if there is more work to do. When you have reached a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.",
      "role": "user"
    }
  ],
  "temperature": 1,
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
    },
    {
      "function": {
        "name": "UserInteraction-SendFinalAnswer",
        "description": "This function is used to send the final answer of a plan to the user.",
        "parameters": {
          "type": "object",
          "required": [
            "answer"
          ],
          "properties": {
            "answer": {
              "type": "string",
              "description": "The final answer"
            }
          }
        }
      },
      "type": "function"
    }
  ],
  "tool_choice": "auto"
}
```

#### Third Response

The API response is a request for SK to call the `GetDailyFact` function with the arguments of "May 27".

```JSON
{
  "id": "chatcmpl-9TaAWiQUgk9dwsfdWoF4OUyqavIjx",
  "object": "chat.completion",
  "created": 1716837572,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": null,
        "tool_calls": [
          {
            "id": "call_ljL6D7BeP6GiQesb0eMhkl9z",
            "type": "function",
            "function": {
              "name": "DailyFactPlugin-GetDailyFact",
              "arguments": "{\u0022today\u0022:\u0022May 27\u0022}"
            }
          }
        ]
      },
      "logprobs": null,
      "finish_reason": "tool_calls"
    }
  ],
  "usage": {
    "prompt_tokens": 364,
    "completion_tokens": 22,
    "total_tokens": 386
  },
  "system_fingerprint": null
}
```

#### Fourth Request

The request is the function which makes a call to the LLM to get a daily fact.

```JSON
{
  "messages": [
    {
      "content": "Tell me an interesting fact from world \r\n        about an event that took place on May 27.\r\n        Be sure to mention the date in history for context.",
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

#### Fourth Response

The response is the LLM providing a fact about the day.

```JSON
{
  "id": "chatcmpl-9TaAWauiHVbbHPiRvdqEYhRi9YNV8",
  "object": "chat.completion",
  "created": 1716837572,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "On May 27, 1937, the Golden Gate Bridge in San Francisco, California, was opened to pedestrian traffic. It was the longest suspension bridge in the world at the time, with a main span of 4,200 feet. The opening ceremony was attended by approximately 200,000 people, who were able to walk across the bridge before it was opened to vehicular traffic the next day."
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 39,
    "completion_tokens": 82,
    "total_tokens": 121
  },
  "system_fingerprint": null
}
```
#### Fifth Request

The fifth (last request) adds the `tool` message with the historic daily fact to use in a response to the user **and** again that message telling the LLM to continue to the next step or call the `SendFinalAnswer` function.

```JSON
{
  "messages": [
    {
      "content": "Original request: Tell me an interesting fact from world about an event that took place on today\u0026#39;s date. Be sure to mention the date in history for context.\n\nYou are in the process of helping the user fulfill this request using the following plan:\nStep 1: Retrieve the current day using the DailyFactPlugin-GetCurrentDay function.\nStep 2: Use the retrieved current day as input to the DailyFactPlugin-GetDailyFact function to get an interesting historic fact for the current date.\nStep 3: Send the retrieved fact as the final answer using the UserInteraction-SendFinalAnswer function.\n\nThe user will ask you for help with each step.",
      "role": "system"
    },
    {
      "content": "Perform the next step of the plan if there is more work to do. When you have reached a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.",
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
          "id": "call_WZ5ljfhIJ7rAJbUyBUvnOnV0"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "May 27",
      "tool_call_id": "call_WZ5ljfhIJ7rAJbUyBUvnOnV0",
      "role": "tool"
    },
    {
      "content": "Perform the next step of the plan if there is more work to do. When you have reached a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.",
      "role": "user"
    },
    {
      "content": null,
      "tool_calls": [
        {
          "function": {
            "name": "DailyFactPlugin-GetDailyFact",
            "arguments": "{\u0022today\u0022:\u0022May 27\u0022}"
          },
          "type": "function",
          "id": "call_ljL6D7BeP6GiQesb0eMhkl9z"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "On May 27, 1937, the Golden Gate Bridge in San Francisco, California, was opened to pedestrian traffic. It was the longest suspension bridge in the world at the time, with a main span of 4,200 feet. The opening ceremony was attended by approximately 200,000 people, who were able to walk across the bridge before it was opened to vehicular traffic the next day.",
      "tool_call_id": "call_ljL6D7BeP6GiQesb0eMhkl9z",
      "role": "tool"
    },
    {
      "content": "Perform the next step of the plan if there is more work to do. When you have reached a final answer, use the UserInteraction-SendFinalAnswer function to communicate this back to the user.",
      "role": "user"
    }
  ],
  "temperature": 1,
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
    },
    {
      "function": {
        "name": "UserInteraction-SendFinalAnswer",
        "description": "This function is used to send the final answer of a plan to the user.",
        "parameters": {
          "type": "object",
          "required": [
            "answer"
          ],
          "properties": {
            "answer": {
              "type": "string",
              "description": "The final answer"
            }
          }
        }
      },
      "type": "function"
    }
  ],
  "tool_choice": "auto"
}
```
#### Fifth Response

The fifth response is the call to the `SendFinalAnswer` indicating to SK the plan is complete.

```JSON
{
  "id": "chatcmpl-9TaAYkzBVbh7gI6arvfQVvOKL8Ld5",
  "object": "chat.completion",
  "created": 1716837574,
  "model": "gpt-3.5-turbo-1106",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": null,
        "tool_calls": [
          {
            "id": "call_EaZB8rfmFThmPDmFtUVjkxRb",
            "type": "function",
            "function": {
              "name": "UserInteraction-SendFinalAnswer",
              "arguments": "{\u0022answer\u0022:\u0022On May 27, 1937, the Golden Gate Bridge in San Francisco, California, was opened to pedestrian traffic. It was the longest suspension bridge in the world at the time, with a main span of 4,200 feet. The opening ceremony was attended by approximately 200,000 people, who were able to walk across the bridge before it was opened to vehicular traffic the next day.\u0022}"
            }
          }
        ]
      },
      "logprobs": null,
      "finish_reason": "tool_calls"
    }
  ],
  "usage": {
    "prompt_tokens": 525,
    "completion_tokens": 99,
    "total_tokens": 624
  },
  "system_fingerprint": null
}
```

# Conclusion

In this entry, I used the `FunctionCallingStepwisePlanner` to provide the same functionality as the previous. In looking at the detailed API calls, we can see this planner has more in common with the OpenAI function calling than the `HandlebarsPlanner` we saw last week.

## Token Usage Comparison

If we compare the tokens used in the last two entries, which used OpenAI function calling, Handlebars planner with and without a saved plan and this entry the numbers look like this:


| Scenario                | Approximate Total Tokens Used |
|-------------------------|-------------------------------|
| OpenAI Function Calling |   755                         |
| No Saved Handlebars Plan| 1,660                         |
| Saved Handlebars Plan   |   152                         |
| Function Calling Stepwise Planner| 1,869              |




If you have a comment, please message me @haleyjason on twitter/X.
