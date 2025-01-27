A couple of weeks ago I thought I'd written my last of these blogs, mainly due to me getting more in depth with Semantic Kernel. However, after I watched Will Velida's video [Using Bing Search API in the Semantic Kernel SDK](https://www.youtube.com/watch?v=eOpDSz1ZB0M) ... I couldn't help but wonder what the API calls were behind the scenes. Will does a great job at explaining how to use the plugin and the Bing resource needed to make calls to the search API, so I won't get into that part of it - I want to focus on the usefulness and API calls made by the plugin.

The code for this entry is the [HelloWorld.WebSearchEnginePlugin.Console](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/src/HelloWorld.WebSearchEnginePlugin.Console) project in my GitHub repo. As usual, you'll need to add your own Azure OpenAI credentials and for this one you'll need a Bing api key as well (watch Will Velida's video if you need more information).

# Usefulness of the WebSearchEnginePlugin

Unlike my other Semantic Kernel Hello World blog posts, I'm skipping the setup since Will covers it so well in his video ([Using Bing Search API in the Semantic Kernel SDK](https://www.youtube.com/watch?v=eOpDSz1ZB0M)) and just focusing on the usefulness of the plugin and some code to see the API calls going on behind the scenes.

## Ever wish the LLM would just do a Bing search if it doesn't know something?

We all know LLMs will attempt to answer your question - even if it has to make something up that sounds good. Don't get me wrong, things **are** getting better. For example: if I login to my OpenAI account, use the ChatGPT 4o model and ask **"Who are the organizers for the Boston Azure meetup?"** it does a good job (and searches 3 sites):

![ChatGPT 4o](/img/2024-06-10_img1.jpg)

However, if I use the ChatGPT 4o model in code, it does not search any additional sites and does not know the answer ... though it makes something up that sounds good:

> NOTE: each time I run this code, it will give me a different list of organizers, this is just one of them.

![ChatGPT 4o with Code](/img/2024-06-10_img2.jpg)

If you live in the Boston area and attend local tech events, you probably know of Bob Familiar and Dan Stolts. They are involved in the local tech community, but are not organizers of Boston Azure.

## Adding the WebSearchEnginePlugin makes the search more accurate

If I add the WebSearchEnginePlugin into the code, and ask the same question **"Who are the organizers for the Boston Azure meetup?"**, this time it gets it right:

![Using WebSearchEnginePlugin](/img/2024-06-10_img3.jpg)

> NOTE: one thing I should mention is I am not using any system message or fancy prompt - I am just asking the LLM a simple question.

So you may now be wondering the same thing I was after I watched Will's video ... ***What API calls is it making? What do the request and response calls look like?***

## The Code:

Like the previous Semantic Kernel Hello World entries, I've started with the same console app as before. The following files are important for this entry:

* [PluginConfig.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.WebSearchEnginePlugin.Console/Configuration/PluginConfig.cs) - this is just an options class for getting the BingApiKey from the `appsettings.json` file
* [ServiceCollectionExtensions.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.WebSearchEnginePlugin.Console/Configuration/ServiceCollectionExtensions.cs) - I added an extension method for adding the [BingConnector](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Web/Bing/BingConnector.cs) along with the option of turning on the request and response logging that was added in [Semantic Kernel Hello World Plugins Part 3](https://jasonhaley.com/2024/04/30/semantic-kernel-hello-world-plugin-part3/#requestandresponselogginghttpclienthandler-and-requestlogginghttpclienthandler). I also modified the HttpClientHandlers to log the Request URL if there is not a response body.
* [Program.cs](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.WebSearchEnginePlugin.Console/Program.cs) - this has the code that adds the WebSearchEnginePlugin plugin

### Using the WebSearchEnginePlugin

In the Program.cs file, the top code is basically the same as the other Semantic Kernel Hello World blog entries:

```C#
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using HelloWorld.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;

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

        var pluginSettings = new PluginOptions();
        config.GetSection(PluginOptions.PluginConfig).Bind(pluginSettings);

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);

            builder.AddConfiguration(config);
            builder.AddConsole();
        });

        // Configure Semantic Kernel
        var builder = Kernel.CreateBuilder();

        builder.Services.AddSingleton(loggerFactory);
        builder.AddChatCompletionService(openAiSettings);
        //builder.AddChatCompletionService(openAiSettings, ApiLoggingLevel.ResponseAndRequest); // use this line to see the JSON between SK and OpenAI

        
```

To use the WebSearchEnginePlugin, you need to add the [Microsoft.SemanticKernel.Plugins.Web](https://www.nuget.org/packages/Microsoft.SemanticKernel.Plugins.Web) nuget package. I also added the `<NoWarn>SKEXP0050</NoWarn>` to my .csproj file to avoid the build warnings of the prerelease functionality.

In order to add the plugin, it needs to use either Bing or Google (I'm only covering Bing, to configure Google would be an exercise for the reader to take on if they choose to do so). The adding of the BingConnector is done with an extension code (link is above if you want to look at the code), and also need to add the `WebSearchEnginePlugin`.

```C#
        builder.AddBingConnector(pluginSettings);
        //builder.AddBingConnector(pluginSettings, ApiLoggingLevel.ResponseAndRequest); // use this line to see the JSON between SK and OpenAI

        builder.Plugins.AddFromType<WebSearchEnginePlugin>();
```

The rest of the relevant code is the mostly the same as adding of the plugin in [Semantic Kernel Hello World Plugins Part 3](https://jasonhaley.com/2024/04/30/semantic-kernel-hello-world-plugin-part3/) - which uses OpenAI function calling. The only difference is the prompt.

```C#
        Kernel kernel = builder.Build();

        var prompt = "Who are the organizers for the Boston Azure meetup?";

        WriteLine($"\nQUESTION: \n\n{prompt}");

        OpenAIPromptExecutionSettings settings = new() 
        { 
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, 
            Temperature = 0.7f,
            MaxTokens = 250
        };
        
        var funcresult = await kernel.InvokePromptAsync(prompt, new KernelArguments(settings));

        WriteLine($"\nANSWER: \n\n{funcresult}");
    }

```

When you run the code, you can see where the Bing Search is done and that it took 670 tokens:

![Code Run with WebSearchEnginePlugin](/img/2024-06-10_img4.jpg)

Now let's turn on the logging to see the JSON request and response.

### A look at the API calls

As you could see above there are only two calls to the LLM - but keep in mind the WebSearchEnginePlugin makes an API call to Bing which is not captured in the output above.

The first call goes to the LLM with the plugin function information and it responded with a request to run the function `WebSearchEnginePlugin-Search` using the arguments **"Boston Azure meetup organizers"**:

> Notice there are two functions: WebSearchEnginePlugin-Search and WebSearchEnginePlugin-GetSearchResults

#### First request:
```JSON
{
  "messages": [
    {
      "content": "Who are the organizers for the Boston Azure meetup?",
      "role": "user"
    }
  ],
  "max_tokens": 250,
  "temperature": 0.7,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt4o",
  "tools": [
    {
      "function": {
        "name": "WebSearchEnginePlugin-Search",
        "description": "Perform a web search.",
        "parameters": {
          "type": "object",
          "required": [
            "query"
          ],
          "properties": {
            "query": {
              "type": "string",
              "description": "Search query"
            },
            "count": {
              "type": "integer",
              "description": "Number of results (default value: 10)"
            },
            "offset": {
              "type": "integer",
              "description": "Number of results to skip (default value: 0)"
            }
          }
        }
      },
      "type": "function"
    },
    {
      "function": {
        "name": "WebSearchEnginePlugin-GetSearchResults",
        "description": "Perform a web search and return complete results.",
        "parameters": {
          "type": "object",
          "required": [
            "query"
          ],
          "properties": {
            "query": {
              "type": "string",
              "description": "Text to search for"
            },
            "count": {
              "type": "integer",
              "description": "Number of results (default value: 1)"
            },
            "offset": {
              "type": "integer",
              "description": "Number of results to skip (default value: 0)"
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
#### First response from LLM
```JSON
{
  "choices": [
    {
      "content_filter_results": {},
      "finish_reason": "tool_calls",
      "index": 0,
      "logprobs": null,
      "message": {
        "content": null,
        "role": "assistant",
        "tool_calls": [
          {
            "function": {
              "arguments": "{\u0022query\u0022:\u0022Boston Azure meetup organizers\u0022}",
              "name": "WebSearchEnginePlugin-Search"
            },
            "id": "call_cYLNxjWAfFDrwmeAHdwl5Snr",
            "type": "function"
          }
        ]
      }
    }
  ],
  "created": 1718025706,
  "id": "chatcmpl-9YZFyyy6HB7Dh9hAPGIBeFc7hPWE0",
  "model": "gpt-4o-2024-05-13",
  "object": "chat.completion",
  "prompt_filter_results": [
    {
      "prompt_index": 0,
      "content_filter_results": {
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        }
      }
    }
  ],
  "system_fingerprint": "fp_5f4bad809a",
  "usage": {
    "completion_tokens": 21,
    "prompt_tokens": 155,
    "total_tokens": 176
  }
}
```
The LLM decided the `WebSearchEnginePlugin-Search` function call would be the right thing to do, instead of making something up.

### The HTTP request to Bing

```
GET https://api.bing.microsoft.com/v7.0/search?q=Boston Azure meetup organizers&count=10&offset=0
```
The request is asking for the first 10 results of the search for "Boston Azure meetup organizers"

### The HTTP response from Bing

> NOTE: I've truncated the results a bit for readability.

```JSON
{
  "_type": "SearchResponse",
  "queryContext": {
    "originalQuery": "Boston Azure meetup organizers"
  },
  "webPages": {
    "webSearchUrl": "https://www.bing.com/search?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers",
    "totalEstimatedMatches": 58600,
    "value": [
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.0",
        "name": "Boston Azure | Meetup",
        "url": "https://www.meetup.com/bostonazure/",
        "isFamilyFriendly": true,
        "displayUrl": "https://www.meetup.com/bostonazure",
        "snippet": "Boston Azure is a community-run group with the goal of learning about cloud computing with the Microsoft Azure cloud computing platform. We have been meeting since October 2009, making us the oldest such Azure-focused community group in the world. The group was founded by Bill Wilder ( @codingoutloud ). If you have any questions or feedback you ...",
        "deepLinks": [
          {
            ... not relevant here
          },
        ],
        "dateLastCrawled": "2024-06-06T05:36:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4676021507668935\u0026mkt=en-US\u0026setlang=en-US\u0026w=gN8QouIfMB5OaJxrivvPbSQMJGw7H7Ok",
        "language": "en",
        "isNavigational": true,
        "noCache": false
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.1",
        "name": "Boston Azure",
        "url": "https://bostonazure.org/",
        "isFamilyFriendly": true,
        "displayUrl": "https://bostonazure.org",
        "snippet": "We are planning to resume in-person events when possible. Boston Azure is a community-run group with the goal of learning about cloud computing with the Microsoft Azure cloud computing platform. We have been meeting since October 2009, making us the oldest such Azure-focused community group in the world. The group was founded by Bill Wilder.",
        "dateLastCrawled": "2024-06-02T03:34:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4948653136549211\u0026mkt=en-US\u0026setlang=en-US\u0026w=xpPlc7rPcRLKY1uY5FMqqMpJ2yHN-377",
        "language": "en",
        "isNavigational": false,
        "noCache": false
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.2",
        "name": "Boston Azure - Global Azure 2024",
        "url": "https://globalazure.net/communities/2024/BostonAzure",
        "isFamilyFriendly": true,
        "displayUrl": "https://globalazure.net/communities/2024/BostonAzure",
        "snippet": "Global Azure Bootcamp 2024 - Boston Azure Edition. Join us for a full day of hands-on Azure AI learning. Details for the event will be maintained on the Boston Azure Meetup page. You can find the organizers on Twitter: Veronika Kolesnikova @veronika_dev1; Jason Haley @haleyjason; Bill Wilder @codingoutloud",
        "dateLastCrawled": "2024-04-22T15:28:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4573878590709323\u0026mkt=en-US\u0026setlang=en-US\u0026w=8y4q6uBlR0gf9Zbi81XqNAe2tVuLONFR",
        "language": "en",
        "isNavigational": false,
        "noCache": false
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.3",
        "name": "Communities - Global Azure 2024",
        "url": "https://globalazure.net/communities/2024",
        "isFamilyFriendly": true,
        "displayUrl": "https://globalazure.net/communities",
        "snippet": "Global Azure wouldn\u0027t be global without the communities around the world! Every community involved in the Global Azure event can be found on this page. In case your community is missing, we invite you over to the communities GitHub repository to create a pull request and add your community to the website. A total of 102 communities and 106 ...",
        "dateLastCrawled": "2024-06-04T17:51:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4557965734211063\u0026mkt=en-US\u0026setlang=en-US\u0026w=Kjjm_CdeTdUYuUnmC4YgJVsHayIZxlgw",
        "language": "en",
        "isNavigational": false,
        "noCache": false
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.4",
        "name": "Boston Azure on Twitter: \u0022Our March #VirtualBostonAzure meetup will be ...",
        "url": "https://twitter.com/bostonazure/status/1638927782116720641",
        "datePublished": "2023-03-23T00:00:00.0000000",
        "datePublishedDisplayText": "Mar 23, 2023",
        "isFamilyFriendly": true,
        "displayUrl": "https://twitter.com/bostonazure/status/1638927782116720641",
        "snippet": "Boston Azure. @bostonazure. Our March #VirtualBostonAzure meetup will be on Wednesday, March 29 at 6pm EDT. @arindam0310018. ... Not a Meetup member yet? Log in and find groups that host online or in person events and meet people in your local community who share your interests.",
        "dateLastCrawled": "2023-04-21T04:22:00.0000000Z",
        "language": "en",
        "isNavigational": false,
        "noCache": true
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.5",
        "name": "Home | AZURE AND FRIENDS",
        "url": "https://www.azureandfriends.com/",
        "isFamilyFriendly": true,
        "displayUrl": "https://www.azureandfriends.com",
        "snippet": "Azure and Friends is an open community of people who share a passion for Microsoft Azure and cloud services in general. We work to promote and share knowledge around Azure over content like meetups, webinars and such. ... If you would you be interested on this please reach organizers in Meetup or Slack. Speakers. Azure \u0026 Friends is always open ...",
        "dateLastCrawled": "2024-06-08T12:14:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4932697340968969\u0026mkt=en-US\u0026setlang=en-US\u0026w=UrJBgTi9BCrDIYAusuvaW522c31uJ_dc",
        "language": "en",
        "isNavigational": false,
        "noCache": false
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.6",
        "name": "Boston Azure on Twitter",
        "url": "https://twitter.com/bostonazure/status/1529494954875183104",
        "datePublished": "2022-05-25T00:00:00.0000000",
        "datePublishedDisplayText": "May 25, 2022",
        "isFamilyFriendly": true,
        "displayUrl": "https://twitter.com/bostonazure/status/1529494954875183104",
        "snippet": "\u201CTomorrow @NorthBTownAzure will have the first in-person meetup in two years! Hope you can come to Burlington to hear @nhcloud talk! #Azure #PrivateLink #BostonAzure\u201D",
        "dateLastCrawled": "2022-05-28T00:48:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4606219691844532\u0026mkt=en-US\u0026setlang=en-US\u0026w=lXuxbUYTotMuqtqyIeTvmY5Mpl8fjaBI",
        "language": "en",
        "isNavigational": false,
        "noCache": false
      },
      {
        "id": "https://api.bing.microsoft.com/api/v7/#WebPages.7",
        "name": "Boston Azure - YouTube",
        "url": "https://www.youtube.com/channel/UCsH4BBOjC84hx9P1wfHcdsA",
        "isFamilyFriendly": true,
        "displayUrl": "https://www.youtube.com/channel/UCsH4BBOjC84hx9P1wfHcdsA",
        "snippet": "Boston Azure User Groups is a combination of Boston Azure and North Boston Azure user groups. Normally each group tries to meet once a month, however currently due to the COVID-19 crisis we are ...",
        "dateLastCrawled": "2024-02-28T20:55:00.0000000Z",
        "cachedPageUrl": "http://cc.bingj.com/cache.aspx?q=Boston\u002BAzure\u002Bmeetup\u002Borganizers\u0026d=4538625495213496\u0026mkt=en-US\u0026setlang=en-US\u0026w=7P78zVg19iAP7ZFWPzaYtYn28U25LBmh",
        "language": "en",
        "isNavigational": false,
        "noCache": false
      }
    ],
    "someResultsRemoved": true
  },
  "videos": {
    ... not relevant here
  },
  "rankingResponse": {
  ... not relevant here
  }
}

```
The results get serialized into a [WebPage](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Web/WebPage.cs) object - which means only the Name, Url and Snippet properties are going to be looked at. If you look at the [BingConnector's](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Plugins/Plugins.Web/Bing/BingConnector.cs) `SearchAsync()` you'll see it is only going to send back the snippet list to the LLM:

```C#
                returnValues = results?.Select(x => x.Snippet).ToList() as List<T>;
```

### Second request shows the search results going to the LLM

The request's tool message's content has the list of snippets from the Bing search result.

```JSON
{
  "messages": [
    {
      "content": "Who are the organizers for the Boston Azure meetup?",
      "role": "user"
    },
    {
      "content": null,
      "tool_calls": [
        {
          "function": {
            "name": "WebSearchEnginePlugin-Search",
            "arguments": "{\u0022query\u0022:\u0022Boston Azure meetup organizers\u0022}"
          },
          "type": "function",
          "id": "call_cYLNxjWAfFDrwmeAHdwl5Snr"
        }
      ],
      "role": "assistant"
    },
    {
      "content": "[\u0022Boston Azure is a community-run group with the goal of learning about cloud computing with the Microsoft Azure cloud computing platform. We have been meeting since October 2009, making us the oldest such Azure-focused community group in the world. The group was founded by Bill Wilder ( @codingoutloud ). If you have any questions or feedback you ...\u0022,\u0022We are planning to resume in-person events when possible. Boston Azure is a community-run group with the goal of learning about cloud computing with the Microsoft Azure cloud computing platform. We have been meeting since October 2009, making us the oldest such Azure-focused community group in the world. The group was founded by Bill Wilder.\u0022,\u0022Global Azure Bootcamp 2024 - Boston Azure Edition. Join us for a full day of hands-on Azure AI learning. Details for the event will be maintained on the Boston Azure Meetup page. You can find the organizers on Twitter: Veronika Kolesnikova @veronika_dev1; Jason Haley @haleyjason; Bill Wilder @codingoutloud\u0022,\u0022Global Azure wouldn\u0027t be global without the communities around the world! Every community involved in the Global Azure event can be found on this page. In case your community is missing, we invite you over to the communities GitHub repository to create a pull request and add your community to the website. A total of 102 communities and 106 ...\u0022,\u0022Boston Azure. @bostonazure. Our March #VirtualBostonAzure meetup will be on Wednesday, March 29 at 6pm EDT. @arindam0310018. ... Not a Meetup member yet? Log in and find groups that host online or in person events and meet people in your local community who share your interests.\u0022,\u0022Azure and Friends is an open community of people who share a passion for Microsoft Azure and cloud services in general. We work to promote and share knowledge around Azure over content like meetups, webinars and such. ... If you would you be interested on this please reach organizers in Meetup or Slack. Speakers. Azure \u0026 Friends is always open ...\u0022,\u0022\u201CTomorrow @NorthBTownAzure will have the first in-person meetup in two years! Hope you can come to Burlington to hear @nhcloud talk! #Azure #PrivateLink #BostonAzure\u201D\u0022,\u0022Boston Azure User Groups is a combination of Boston Azure and North Boston Azure user groups. Normally each group tries to meet once a month, however currently due to the COVID-19 crisis we are ...\u0022]",
      "tool_call_id": "call_cYLNxjWAfFDrwmeAHdwl5Snr",
      "role": "tool"
    }
  ],
  "max_tokens": 250,
  "temperature": 0.7,
  "top_p": 1,
  "n": 1,
  "presence_penalty": 0,
  "frequency_penalty": 0,
  "model": "gpt4o",
  "tools": [
    {
      "function": {
        "name": "WebSearchEnginePlugin-Search",
        "description": "Perform a web search.",
        "parameters": {
          "type": "object",
          "required": [
            "query"
          ],
          "properties": {
            "query": {
              "type": "string",
              "description": "Search query"
            },
            "count": {
              "type": "integer",
              "description": "Number of results (default value: 10)"
            },
            "offset": {
              "type": "integer",
              "description": "Number of results to skip (default value: 0)"
            }
          }
        }
      },
      "type": "function"
    },
    {
      "function": {
        "name": "WebSearchEnginePlugin-GetSearchResults",
        "description": "Perform a web search and return complete results.",
        "parameters": {
          "type": "object",
          "required": [
            "query"
          ],
          "properties": {
            "query": {
              "type": "string",
              "description": "Text to search for"
            },
            "count": {
              "type": "integer",
              "description": "Number of results (default value: 1)"
            },
            "offset": {
              "type": "integer",
              "description": "Number of results to skip (default value: 0)"
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

#### Second response from the LLM

The response is the result of our original question but taking into account the Bing search

```JSON
{
  "choices": [
    {
      "content_filter_results": {
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        }
      },
      "finish_reason": "stop",
      "index": 0,
      "logprobs": null,
      "message": {
        "content": "The Boston Azure meetup is organized by:\n\n- Bill Wilder (Twitter: [@codingoutloud](https://twitter.com/codingoutloud))\n- Veronika Kolesnikova (Twitter: [@veronika_dev1](https://twitter.com/veronika_dev1))\n- Jason Haley (Twitter: [@haleyjason](https://twitter.com/haleyjason))",
        "role": "assistant"
      }
    }
  ],
  "created": 1718025708,
  "id": "chatcmpl-9YZG0hssz4QrbWdHRjnnloablQP91",
  "model": "gpt-4o-2024-05-13",
  "object": "chat.completion",
  "prompt_filter_results": [
    {
      "prompt_index": 0,
      "content_filter_results": {
        "hate": {
          "filtered": false,
          "severity": "safe"
        },
        "self_harm": {
          "filtered": false,
          "severity": "safe"
        },
        "sexual": {
          "filtered": false,
          "severity": "safe"
        },
        "violence": {
          "filtered": false,
          "severity": "safe"
        }
      }
    }
  ],
  "system_fingerprint": "fp_5f4bad809a",
  "usage": {
    "completion_tokens": 84,
    "prompt_tokens": 686,
    "total_tokens": 770
  }
}
```

# Conclusion

In this entry, I showed the usefulness of the `WebSearchEnginePlugin` and the API conversations between the LLM and also Bing that all come together to provide an accurate answer to my simple question (with no complicated system prompt).


If you have a comment, please message me @haleyjason on twitter/X.
