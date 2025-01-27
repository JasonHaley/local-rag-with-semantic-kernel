A couple of weeks ago, in my [last entry](/2024/03/30/semantic-kernel-hello-world/) I created a simple Hello World application with Semantic Kernel. Since then, I've worked my way through the MS Learning path: [APL-2005 Develop AI agents using Azure OpenAI and the Semantic Kernel SDK](https://learn.microsoft.com/en-us/training/paths/develop-ai-agents-azure-open-ai-semantic-kernel-sdk/) - which I highly recommend if you are also learning SK.

In this entry I'm going to start with the code from the last entry and extract the prompt to a plugin. I've put the code for this blog in the same repo as the last entry: [semantic-kernel-getting-started](https://github.com/JasonHaley/semantic-kernel-getting-started) 

# Semantic Kernel Plugins

There are two types of plugin functionality in the [sample code](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/src/HelloWorld.Plugin.Console): [prompts](https://learn.microsoft.com/en-us/semantic-kernel/prompts/) and [native functions](https://learn.microsoft.com/en-us/semantic-kernel/agents/plugins/using-the-kernelfunction-decorator?tabs=Csharp). Just a quick review, the hello world example is to call OpenAI with a simple prompt with today's day and return a historical fact about the date. The output should be similiar to this: 

![Bills Code](/img/2024-03-30_img1.jpg)

## Prompts

Semantic Kernel makes it easy to load a saved prompt and settings for things like the temperature, max tokens, etc. The implementation uses the directory structure Prompts\DailyFact and two files: [`config.json`](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin.Console/Prompts/DailyFact/config.json) and [`skprompt.txt`](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/src/HelloWorld.Plugin.Console/Prompts/DailyFact/skprompt.txt). More information on how to do this can be found in the documentation: [Saving and sharing prompts](https://learn.microsoft.com/en-us/semantic-kernel/prompts/saving-prompts-as-files?tabs=Csharp).

![Prompts](/img/2024-04-11_img2.jpg)

The config.json file has the configuration values like the description of the prompt, execution settings to use with the OpenAI API call and information about any input variables:
```json
{
  "schema": 1,
  "description": "Provides interesting historic facts for the current date.",
  "execution_settings": {
    "default": {
      "max_tokens": 250,
      "temperature": 0.7
    }
  },
  "input_variables": [
    {
      "name": "today",
      "description": "Current date",
      "required": true
    }
  ]
}
```

The skprompt.txt file has xml elements in it for any user, system or assistant messages you want to be created in the prompt to OpenAI:
```xml
<message role="user">Tell me an interesting fact from world 
about an event that took place on {{$today}}.
Be sure to mention the date in history for context.</message>
```
The format I used for the skprompt.txt file was from the MS Learn on Semantic Kernel: [Exercise - Use personas in prompts](https://learn.microsoft.com/en-us/training/modules/create-plugins-semantic-kernel/6-exercise-use-personas-prompts) - the contents could have been the prompt.

> NOTE: If you are using Visual Studio, you will need to set the properties on these two files so they are copied and treated as content at build time
> ![File Properties](/img/2024-04-11_img1.jpg)

For comparison, in the last blog entry I used this code to pass the prompt to OpenAI:
```C#
var prompt = $"Tell me an interesting fact from world about an event " +
            $"that took place on {today}. " +
            $"Be sure to mention the date in history for context.";
                        
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    Temperature = 0.7f,
    MaxTokens = 250
};

var result = await chatCompletionService.GetChatMessageContentsAsync(prompt, openAIPromptExecutionSettings, kernel);
```

Now with the prompt files, I use this code to accomplish the same thing:
```C#
var prompts = kernel.CreatePluginFromPromptDirectory("Prompts");

var result = await kernel.InvokeAsync(
    prompts["DailyFact"],
    new() {
        { "today", today },
    }
);
```
The output is still mostly same as last time (I added a dashed line instead of writing green text) and I'm also not logging the prompt.

![Prompt Output](/img/2024-04-11_img3.jpg)

### Lesson Learned on Logging

Last week, I learned with Semantic Kernel if you set the logging level to Trace you will get a lot more useful information (at least useful for learning this stuff). The same code from above provides this when I turn on the Trace logging:

![Prompt Output](/img/2024-04-11_img4.jpg)

As you can see, the loading of the prompt configuration and template are all captured in the trace logging. 

## Conclusion

In this short entry, I covered using the config.json and skprompt.txt to load saved prompts and then call OpenAI. In the next blog I'll cover
doing the same thing with a native function.

If you have a comment, please message me @haleyjason on twitter/X.
