This is the seventh part of the [eShopSupport Series](/2024/08/23/introducing-eshopsupport-series/) which covers the details of the [eShopSupport](https://github.com/dotnet/eshopsupport) GitHub repository.   
  
> NOTE: This entry will make the most sense to you if you've first read [eShopSupport: Evaluator Project](https://jasonhaley.com/2024/09/20/eshopsupport-evaluator/)

# EvaluationTests Project
The [EvaluationTests](https://github.com/dotnet/eShopSupport/tree/main/test/EvaluationTests) project is a recently added xUnit Test Project that uses the new `Microsoft.Extensions.AI.Evaluation` packages highlighted in this blog: [Evaluate the quality of your AI applications with ease](https://devblogs.microsoft.com/dotnet/evaluate-the-quality-of-your-ai-applications-with-ease/). I think the idea is to replace the [Evaluator](https://github.com/dotnet/eShopSupport/tree/main/src/Evaluator) project but, as is, it doesn't run all 500 evaluation questions in the dev folder (which is good since it would not be cheap to do unless you are running locally).

**The good news is**: the EvaluationTests project is a good example of how to use the new [Microsoft.Extensions.AI.Evaluation](https://www.nuget.org/packages/Microsoft.Extensions.AI.Evaluation/) package - which currently has very few blog posts or videos on yet. I've put the resources I've found so far in the [other resources](#other-resources) section below.

The EvaluationTests project is located in in the test folder:

![Files](/img/2024-12-11_img1.jpg)

In this entry I'll cover the details of how the tests in the EvaluationTests work, a few things I found interesting and some thoughts on improvements.

> NOTE: the Microsoft.Extensions.AI.Evaluation packages are still in preview. Once it is released I'll update this entry with the final usage - I'm assuming it is going to change a little.

## What does it do?

Luis Quintanilla gives a sneak-peak of Microsoft.Extensions.AI.Evaluation in the eShopSupport solution in the .NET Conf session: [Building AI Applications from Scratch: A Hands-On Guide for .NET Developers](https://www.youtube.com/watch?v=7Rw_ciSh2Wk&t=1236s).

![Video Screenshot](/img/2024-12-11_img2.jpg)

He discusses the Evaluation framework and its purpose more than focusing on the implementation in the eShopSupport project.

### The AnswerScoringEvaluator.cs file

Since this file uses the namespace `Microsoft.Extensions.AI.Evaluation.Quality`, I'm assuming this will end up inside the Microsoft.Extensions.AI.Evaluation.Quality package and not be needed in the project. However, it is currently there and gives us and idea of how the evaluators are built.

First, it inherits from the `ChatConversationEvaluator` class which provides some base functionality for calling the LLM in the `EvaluateAsync()` and returning the metrics. Before calling the LLM, the base class calls the over ridden methods like `RenderEvaluationPromptAsync` to get the prompt to use in the chat call.  The `ParseEvaluationResponseAsync` is where the specific metrics are parsed and populated.

### The EvalQuestion.cs file

This is just a DTO class for materializing the evalquestions.json file:

```C#
public class EvalQuestion
{
    public int QuestionId { get; set; }

    public int? ProductId { get; set; }

    public required string Question { get; set; }

    public required string Answer { get; set; }
}
```

### Settings.cs file

This file is used to parse settings from the appsettings.json and seems a little out of place compared to the other projects in the solution. However, it shows you another way to get the settings out of an appsettings.json file to be used in the application code.

```C#
public class Settings
{

    public readonly string DeploymentName;
    public readonly string ModelName;
    public readonly string Endpoint;
    public readonly string StorageRootPath;

    public Settings(IConfiguration config)
    {
        DeploymentName = config.GetValue<string>("DeploymentName") ?? throw new ArgumentNullException(nameof(DeploymentName));
        ModelName = config.GetValue<string>("ModelName") ?? throw new ArgumentNullException(nameof(ModelName));
        Endpoint = config.GetValue<string>("Endpoint") ?? throw new ArgumentNullException(nameof(Endpoint));
        StorageRootPath = config.GetValue<string>("StorageRootPath") ?? throw new ArgumentNullException(nameof(StorageRootPath));
    }

    private static Settings? currentSettings = null;

    public static Settings Current
    {
        get {
            currentSettings ??= GetCurrentSettings();
            return currentSettings;
        }
    }

    private static Settings GetCurrentSettings()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        IConfigurationRoot config = builder.Build();

        return new Settings(config);
    }
}
```

You may notice there are no keys in this settings file. That is one big difference from the  appsettings.Local.json used in the other projects. However this also assumes you have everything configured to use a managed identity, which is different than the other projects.

### EvaluationTests.cs file

This is ***the file*** that has the bulk of the logic you'll want to learn.

The top of the class has the `Questions`, which uses a utility method to load the evalquestions.json file into memory.

There are a few variables for holding references to the `backend`, `chatCompletion` (which does not seem to be used currently), `ExecutionName` for a unique name for the execution results.

The `InitializeAsync()` method sets up the `backend` to the same pre-authenticated client the [Evaluator uses](https://jasonhaley.com/2024/09/20/eshopsupport-evaluator/#configure-the-system-under-test).

```C#
    public class EvaluationTests : IAsyncLifetime
    {
        public static IEnumerable<EvalQuestion> Questions => LoadEvaluationQuestions()
            .OrderBy(a => a.QuestionId);

        public static StaffBackendClient? backend = null;
        public static IChatClient? chatCompletion = null;
        private static readonly string ExecutionName = $"{DateTime.UtcNow:yyyyMMddTHHmmss}";

        public async Task InitializeAsync()
        {
            backend = await DevToolBackendClient.GetDevToolStaffBackendClientAsync(
                identityServerHttpClient: new HttpClient { BaseAddress = new Uri("https://localhost:7275/") },
                backendHttpClient: new HttpClient { BaseAddress = new Uri("https://localhost:7223/") });
            chatCompletion = new AzureOpenAIClient(new Uri(Settings.Current.Endpoint), new DefaultAzureCredential())
                .AsChatClient(Settings.Current.DeploymentName);
        }
        
        ...    
        
    }
```

The `LoadEvaluationQuestions()` is exactly the same as the Evaluator project.

```C#
        static EvalQuestion[] LoadEvaluationQuestions()
        {
            var questionDataPath = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "EvalQuestionsJsonPath").Value!;
            if (!File.Exists(questionDataPath))
            {
                throw new FileNotFoundException("Questions not found. Ensure the data ingestor has run.", questionDataPath);
            }
            var questionsJson = File.ReadAllText(questionDataPath);
            return JsonSerializer.Deserialize<EvalQuestion[]>(questionsJson)!;
        }
```

`GetReportingConfiguration()` is where the `ReportingConfiguration` is created. This is where the different evaluators, chat client and config are used to initialize the configuration a set of scenario runs will use and the storage location the results will be saved.

```C#
static ReportingConfiguration GetReportingConfiguration()
        {
            // Setup and configure the evaluators you would like to utilize for each AI chat
            IEvaluator rtcEvaluator =
                new RelevanceTruthAndCompletenessEvaluator(
                    new RelevanceTruthAndCompletenessEvaluator.Options(includeReasoning: true));
            IEvaluator coherenceEvaluator = new CoherenceEvaluator();
            IEvaluator fluencyEvaluator = new FluencyEvaluator();
            IEvaluator groundednessEvaluator = new GroundednessEvaluator();
            IEvaluator answerScoringEvaluator = new AnswerScoringEvaluator();

            var endpoint = new Uri(Settings.Current.Endpoint);
            var azureClient = new AzureOpenAIClient(endpoint, new DefaultAzureCredential());

            IChatClient chatClient = azureClient.AsChatClient(Settings.Current.DeploymentName);
            Tokenizer tokenizer = TiktokenTokenizer.CreateForModel(Settings.Current.ModelName);

            var chatConfig = new ChatConfiguration(chatClient, tokenizer.ToTokenCounter(6000));

            return DiskBasedReportingConfiguration.Create(
                    storageRootPath: Settings.Current.StorageRootPath,
                    chatConfiguration: chatConfig,
                    evaluators: [
                        rtcEvaluator,
                        coherenceEvaluator,
                        fluencyEvaluator,
                        groundednessEvaluator,
                        answerScoringEvaluator],
                    executionName: ExecutionName);
        }
```

As you can see there are several evaluators configured:

* **RelevanceTruthAndCompletenessEvaluator** - an evaluator that evaluates the relevance, truth and completeness.
* **CoherenceEvaluator** - evaluates the coherence.
* **FluencyEvaluator** - evaluates the fluency.
* **GroundednessEvaluator** - evaluates the groundedness
* **AnswerScoringEvaluator** - this is the file mentioned above, evaluates a response and gives it a single score.

`EvaluateQuestionsInALoop()` shows how to iterate through the `Questions` member level list (mentioned above) and run evaluations in parallel.

```C#
[Fact]
public async Task EvaluateQuestionsInALoop()
{

    const int scoringParallelism = 4;
    var reportingConfiguration = GetReportingConfiguration();

    await Parallel.ForEachAsync(Questions.Take(5), new ParallelOptions { MaxDegreeOfParallelism = scoringParallelism }, (Func<EvalQuestion, CancellationToken, ValueTask>)(async (question, cancellationToken) =>
    {

        for (int i = 0; i < 3; i++)
        {
            await EvaluateQuestion(question, reportingConfiguration, i, cancellationToken);
        }
    }));
}
```

Next the `EvaluateQuestionsWithMemberData()` method, gives us a good example of how to utilize the `EvalQuestions` member level list of questions as data for the `Theory`, meaning it will run this method with the contents of that list.

```C#
        public static TheoryData<EvalQuestion> EvalQuestions => [.. LoadEvaluationQuestions().OrderBy(a => a.QuestionId).Take(5)];
        
        [Theory]
        [MemberData(nameof(EvalQuestions))]
        public async Task EvaluateQuestionsWithMemberData(EvalQuestion question)
        {
            var reportingConfiguration = GetReportingConfiguration();
            for (int i = 0; i < 3; i++)
            {
                await EvaluateQuestion(question, reportingConfiguration, i, CancellationToken.None);
            }
        }
```

> NOTE: there is a different syntax used between the Questions and the EvalQuestions lists


The `EvaluateQuestion_HowToAccessEssentials()`, `EvaluateQuestion_WhatAreTheOverheatingPrecautions` and `EvaluateQuestion_Summit3000TrekkingBackpackStrapAdjustment` methods show how to set up a `Fact` to run single evaluations: 
```C#
[Fact]
public async Task EvaluateQuestion_HowToAccessEssentials()
{
    var reportingConfiguration = GetReportingConfiguration();
    var question = new EvalQuestion
    {
        QuestionId = 1,
        ProductId = 158,
        Question = "How to access essentials?",
        Answer = "Unzip the main compartment"
    };
    await EvaluateQuestion(question, reportingConfiguration, 0, CancellationToken.None);
}
[Fact]
public async Task EvaluateQuestion_WhatAreTheOverheatingPrecautions()
{
    var reportingConfiguration = GetReportingConfiguration();
    var question = new EvalQuestion
    {
        QuestionId = 2,
        ProductId = 199,
        Question = "What are the overheating precautions?",
        Answer = "Do not leave in direct sunlight for extended periods."
    };

    await EvaluateQuestion(question, reportingConfiguration, 0, CancellationToken.None);
}
[Fact]
public async Task EvaluateQuestion_Summit3000TrekkingBackpackStrapAdjustment()
{
    var reportingConfiguration = GetReportingConfiguration();
    var question = new EvalQuestion
    {
        QuestionId = 3,
        ProductId = 99,
        Question = "Hi there, I recently purchased the Summit 3000 Trekking Backpack and I\u0027m having issues with the strap adjustment. Can you provide me with the specified torque value for the strap adjustment bolts?",
        Answer = "15-20 Nm"
    };
    await EvaluateQuestion(question, reportingConfiguration, 0, CancellationToken.None);
}
```

As you have probably noticed, each of the test above call the `EvaluateQuestion()`. This is the method that interacts with the evaluation package. In this case it calls `reportingConfiguration.CreateScenarioRunAsync()` to setup the run, then performs the same testing `backend` call the Evaluator does passing the information from the evalquestions.json file. Once the response is captured, the `scenario.EvaluateAsync()` is called passing the question, response from the backend and the answer from the json file. Each of the evaluators setup in the reporting configuration will be making calls to the LLM to evaluate the question, response and answer to capture the metrics. In the process there are json files generated in the cache storage path with the call results.

```C#
private static async Task EvaluateQuestion(EvalQuestion question, ReportingConfiguration reportingConfiguration, int i, CancellationToken cancellationToken)
{
    string uniqueId = $"{Guid.NewGuid().ToString("N")}";
    await using ScenarioRun scenario = await reportingConfiguration.CreateScenarioRunAsync($"Question_{question.QuestionId}-{uniqueId}", $"Iteration {i + 1}", cancellationToken: cancellationToken);

    var responseItems = backend!.AssistantChatAsync(new AssistantChatRequest(
        question.ProductId,
        null,
        null,
        null,
        [new() { IsAssistant = true, Text = question.Question }]),
        cancellationToken);

    var answerBuilder = new StringBuilder();
    await foreach (var item in responseItems)
    {
        if (item.Type == AssistantChatReplyItemType.AnswerChunk)
        {
            answerBuilder.Append(item.Text);
        }
    }

    var finalAnswer = answerBuilder.ToString();

    EvaluationResult evalResult = await scenario.EvaluateAsync(
        [new ChatMessage(ChatRole.User, question.Question)],
        new ChatMessage(ChatRole.Assistant, finalAnswer),
        additionalContext: [new AnswerScoringEvaluator.Context(question.Answer)],
        cancellationToken);


    Assert.False(evalResult.Metrics.Values.Any(m => m.Interpretation?.Rating == EvaluationRating.Inconclusive), "Model response was inconclusive");
}
```

> NOTE: I've added the line: `string uniqueId = $"{Guid.NewGuid().ToString("N")}";` and used it in the scenario name `$"Question_{question.QuestionId}-{uniqueId}"` in order to keep from stomping the same file when the same question is used in multiple evaluations.

## Dependencies

The EvaluationTests project needs the [Aspire AspHost](http://localhost:1313/2024/10/04/eshopsupport-aspire-projects/) running but not with the debugger connected to it (more on this in the next session). This means you'll need docker running and at least Identity Server and the Backend project. Like with the [Evaluator Project](http://localhost:1313/2024/09/20/eshopsupport-evaluator/), The EvaluationTests are evaluating the `backend.AssistantChatAsync()` which is the RAG portion of the application.

## How to set it up

Like the Evaluator project, by default the EvaluationTests looks for the evalquestions.json file at the location set in the [EvaluationTests.csproj](https://github.com/dotnet/eShopSupport/blob/main/test/EvaluationTests/EvaluationTests.csproj) file:

```xml
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
      <_Parameter1>EvalQuestionsJsonPath</_Parameter1>
      <_Parameter2>$(SolutionDir)seeddata\test\evalquestions.json</_Parameter2>
    </AssemblyAttribute>
```

> NOTE: I recommend changing the file path to the dev folder like the Evaluator uses:

```xml
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
      <_Parameter1>EvalQuestionsJsonPath</_Parameter1>
      <_Parameter2>$(SolutionDir)seeddata\dev\evalquestions.json</_Parameter2>
    </AssemblyAttribute>
```

There are also a few other changes I recommend making (***Assuming you are using Azure OpenAI***)

1. Copy the appsettings.Local.json you have working in the AppHost project into the EvaluationTests project at the root level.

![Copy appsettings.Local.json](/img/2024-12-11_img3.jpg)

2. Verify the properties are set to have it copied over to output directory

![Properties](/img/2024-12-11_img4.jpg)

3. In the Evaluator project, copy the [GetChatCompletionService() starting line 186](https://github.com/dotnet/eShopSupport/blob/main/src/Evaluator/Program.cs#L186) in the Program.cs file:

```C#
static IChatClient GetChatCompletionService(string connectionStringName)
{
    var config = new ConfigurationManager();
    config.AddJsonFile("appsettings.json");
    config.AddJsonFile("appsettings.Local.json", optional: true);

    var connectionStringBuilder = new DbConnectionStringBuilder();
    var connectionString = config.GetConnectionString(connectionStringName);
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"Missing connection string {connectionStringName}");
    }

    connectionStringBuilder.ConnectionString = connectionString;

    var deployment = connectionStringBuilder.TryGetValue("Deployment", out var deploymentValue) ? (string)deploymentValue : throw new InvalidOperationException($"Connection string {connectionStringName} is missing 'Deployment'");
    var endpoint = connectionStringBuilder.TryGetValue("Endpoint", out var endpointValue) ? (string)endpointValue : throw new InvalidOperationException($"Connection string {connectionStringName} is missing 'Endpoint'");
    var key = connectionStringBuilder.TryGetValue("Key", out var keyValue) ? (string)keyValue : throw new InvalidOperationException($"Connection string {connectionStringName} is missing 'Key'");

    return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key)).AsChatClient(deployment);
}
```

4. Paste that method into the [EvaluationTest.cs class](https://github.com/dotnet/eShopSupport/blob/main/test/EvaluationTests/EvaluationTests.cs)

5. Comment out line 34 and 35, since they don't seem to be used

```C#
            // chatCompletion = new AzureOpenAIClient(new Uri(Settings.Current.Endpoint), new  DefaultAzureCredential())
            //    .AsChatClient(Settings.Current.DeploymentName);
```

6. Comment out line 65 and 67

```C#
            var azureClient = new AzureOpenAIClient(endpoint, new DefaultAzureCredential());

            IChatClient chatClient = azureClient.AsChatClient(Settings.Current.DeploymentName);
```
7. Add this line before line 68 to use the settings in the `appsettings.Local.json` file:

```C#
            var chatClient = GetChatCompletionService("chatcompletion");
```

8. You'll need to add a full path for the output files to be cached in the appsettings.json file:

```json
  "StorageRootPath": "D:\\GitHub\\eShopSupport\\test\\cache"
```

Now, you need to start the AppHost without debugging. Debug -> Start Without Debugging

![Start Without Debugging](/img/2024-12-11_img5.jpg)

Once the AppHost has the projects started, you can now go to the Test menu -> Test Explorer

![Test Explorer](/img/2024-12-11_img6.jpg)

Select the EvaluationTests (5) 

![Evaluation Tests](/img/2024-12-11_img7.jpg)

Click run to run all the tests

![Run all](/img/2024-12-11_img8.jpg)

All tests should pass.

### View Results

I'm sure the tooling will improve for this, but for now you need to manually generate an html page to view the results.

1. Open a command window in the location you put for `StorageRootPath` in the appsettings.json.

2. Next you'll need to install the `Microsoft.Extensions.AI.Evaluation.Console` dotnet plugin if you don't already have it installed by running the following:

```powershell
dotnet tool install Microsoft.Extensions.AI.Evaluation.Console --version 0.9.37-preview --create-manifest-if-needed
```

If your run `dotnet tool list` you should see it:

![dotnet tool list](/img/2024-12-11_img9.jpg)

3. Now you should be able to run the command to generate a report.html file showing the evaluation results:

```powershell
dotnet aieval report --path D:\__ai\GitHub\eShopSupport\test\cache --output report.html
```

The output will look something like this:

```powershell
info: Microsoft.Extensions.AI.Evaluation.Console[0]
      Microsoft.Extensions.AI.Evaluation.Console [0.9.6-preview+b6677b77bc.RR]
info: Microsoft.Extensions.AI.Evaluation.Console[0]
      Storage root path: 0
info: Microsoft.Extensions.AI.Evaluation.Console[0]
      Report: D:\__ai\GitHub\eShopSupport\test\cache\report.html [html]
```

If you look in the directory, you should see the report.html file. Open it and you'll see a nice visual display of the results.

![report.html](/img/2024-12-11_img10.jpg)

## Points of Interest

These are some points in the code base that I found interesting and will be revisiting when writing my own code.

### Different ways to pass data to tests

I like that they have examples of passing lists and running singular evaluations. This will be a great resource to point at when the evaluations package is closer to shipping.

## Thoughts on Improvements

This project shows the promise of the new evaluations package, though I have a feeling things are going to change - so I don't want to pick on any of the evaluations logic here.

### Settings consistency

I like the idea of using either a managed identity with Azure OpenAI or just using a local LLM for running the evaluations - however it feels like those settings should be consistent between the projects.

Once the evaluations packages get updated I'll see if I can contribute some setup instructions to the project to help others get up and running faster.

## Other Resources

* [eShopSupport Github](https://github.com/dotnet/eShopSupport)
* [Evaluate the quality of your AI applications with ease](https://devblogs.microsoft.com/dotnet/evaluate-the-quality-of-your-ai-applications-with-ease/)
* [Building AI Applications from Scratch: A Hands-On Guide for .NET Developers](https://www.youtube.com/watch?v=7Rw_ciSh2Wk)

If you have a comment, please message me @haleyjason on twitter/X.