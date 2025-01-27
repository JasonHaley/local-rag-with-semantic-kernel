# Demo Review: Simple RAG using Blazor, SQL Server, Azure OpenAI and Function Calling

If you are like me, a full stack C# developer who is attempting to get up to speed on how GenAI technologies are going to show up in our business applictions - then after you get the [first demo](/2024/02/07/simple-rag-sql-openai/) up and running, this demo (also by Michael Washington) is a great next step.

RAG (Retrieval Augmented Generation) applications typically have the following steps:

1. Gather input from the user
2. **(Retrieval)** Use the user's input to do a query over data or documents (ofen using vector embeddings) and gather N number of the best results from that search <u>then pass</u> to an LLM as context to use in crafting a response for the user
3. **(Generation)** Return the natual language result from the LLM describing its search result

However, there is another option that changes the flow slightly: [function calling](https://platform.openai.com/docs/guides/function-calling). This alternative looks like this:

1. Gather input from the user
2. <u>Pass the input</u> to the LLM along with instructions on functions to call if needed
3. LLM takes the input, function calls and instructions, then calls back to the originating system with parameters it has picked out for the functions to call.
4. **(Retrieval)** The originating system, then runs the logic for the functions with the provided arguments and returns to the LLM.
5. **(Generation)** The LLM then returns the natural language result describing its search results.

Michael has another blog post specifically about this flow: [Recursive Azure OpenAI Function Calling](https://blazorhelpwebsite.com/ViewBlogPost/10068), though the demo below is described in his blog: [Azure OpenAI RAG Pattern using Functions and a SQL Vector Database](https://blazorhelpwebsite.com/ViewBlogPost/11068). 

## Demo Details

| Item of Interest | As Of 2/11/2024 |
|----------|----------|
| Author: | Michael Washington (@ADefWebserver) |
| Date created: | 8/11/2023 |
| Updated within last month: | No |
| Link to source code: | https://blazorhelpwebsite.com/filedownloads  <br/> **NOTE:**  you have to login to download code|
| Link to documentation: | https://blazorhelpwebsite.com/ViewBlogPost/10068 |
| Github project link: | Not on GitHub |
| Knowledge needed to use: | C# <br/> SQL Server |
| Prerequisites to get demo working: | Azure Open AI (described in his article) <br/> **NOTE:** Can use OpenAI's API with a few small modifications |
| Knowledge would be helpful but not required: | Blazor <br/> Radzen.Blazor <br/> EF Core <br/> JSON <br/> Azure <br/> Open AI APIs <br/> Vector Embeddings |
| Technologies Used: | ASPNET 7 <br/> C# <br/> Blazor <br/> SQL Server <br/> SQL Function <br/> ColumnStore index<br/> Azure Open AI API <br/> Simple chunking technique <br/> Open AI function calling (now called tools)

## Demo Description

Before you go through this demo, you really should read the [first demo](/2024/02/07/simple-rag-sql-openai/) because the codebases are very similiar. The end functionality/experience for the user is the same, the differences are in the backend.

This demo is the first implementation of the functions calling that I found. Also keep in mind the OpenAI API/SDK syntax has changed a bit since Michael published this demo. If you try and implement the same functionality with the latest API/SDK, expect to have quite a few syntax changes to make.

The key points in this demo are declaring the function definition and handling the function call in the flow of the chat result from OpenAI.

### Some Screenshots:

The definition of the function and metadata about it are set on the chat completion request:

![Function Declaration](/img/2024-02-11_img1.jpg)

Once the user submits a message like "When is Cocodona250?", OpenAI responds with the information to call the function defined above. Notice the **prompt** and value are sent in a JSON string:

![OpenAI Response](/img/2024-02-11_img2.jpg)

In this case the LLM "cleaned up" the user input and has determined the proper prompt argument value is "Cocodona250 date" (more on this below).

The demo code goes through the same search and returns the same values it did in the [first demo](/2024/02/07/simple-rag-sql-openai/), so nothing really new there.

## How to get it up and running

1. The steps are the same as the [first demo](/2024/02/07/simple-rag-sql-openai/) in order to get everything up and working.
2. Once you have that demo up and running, login to Michael's site and download the code for this demo.

![Function Call](/img/2024-02-11_img5.jpg)

3. Use the same appsetting.json you ended the first demo with.

I would recommend setting some breakpoints on the following lines:

**Index.razor line 322** to see the what is being passed to OpenAI and what is being returned.

![OpenAI Call](/img/2024-02-11_img3.jpg)

**Index.razor line 464** to see the arguments coming into the Function call.

![Function Call](/img/2024-02-11_img4.jpg)


## Points of Interest

These are some items in the demo that are RAG feature related and you may revisit later when you start creating your own RAG application, the first two items are here for completeness but where originally introduced in the first demo.

### Storing Vectors in SQL Server

This demo stores the vector embedding array returned from OpenAI into the ArticleVectorData table. The **text-embedding-ada-002** embedding model returns an array of 1,536 floating point numbers that need to be stored for each chunk of text sent. In order to efficiently work with queryinng that table, he adds a [Columnstore index](https://learn.microsoft.com/en-us/sql/relational-databases/indexes/columnstore-indexes-overview?view=sql-server-ver16) to it.

Also he uses a SQL Function to encapsulate the calculation of the cosine similarity from [Azure-Samples/zaure-sql-db-openai](https://github.com/Azure-Samples/azure-sql-db-openai/blob/main/vector-embeddings/04-sample-function.sql). This is the functionaliy that provides a sort of semantic/similiarity matching ability to the text that you type in the chat and the content stored in the database.

### Simple chunking technique

The RagChat.razor component has the logic that splits and normalizes the text into smaller chunks to send off to OpenAI. The more you work with RAG applications, the more you will realize the chunking strategy is important. This demo uses a pretty simple, but effective for demo purposes, chunking method:

```    
    // The method that splits the text into chunks of a given size
    public static List<string> SplitTextIntoChunks(string text, int chunkSize)
    {
        // Initialize an empty list to store the chunks
        List<string> chunks = new List<string>();

        // Fix up text
        var NormalizedText = NormalizeText(text);

        // Split the text by whitespace characters
        string[] words = NormalizedText.Split();

        // Loop through the words and add them to the current chunk
        string currentChunk = "";
        foreach (string word in words)
        {
            // If adding the word would exceed the chunk size,
            // add the current chunk to the list and start a new one
            if (currentChunk.Length + word.Length + 1 > chunkSize)
            {
                chunks.Add(currentChunk);
                currentChunk = "";
            }

            // Add the word to the current chunk with a space
            currentChunk += word + " ";
        }

        // Add the last chunk to the list if it is not empty
        if (currentChunk != "")
        {
            chunks.Add(currentChunk);
        }

        // Return the list of chunks
        return chunks;
    }
```

### Function Calling

As I mentioned earlier, the syntax for the latest API/SDK has changed a little, but the declaration of the fuction, description and parameters are similiar. More detail on the new format can be found here: [How to use function calling with Azure OpenAI Service (Preview)](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling?tabs=python)

One thing to keep in mind is the descriptions you include with the function declaration **are important and compare to system messages**. For example: 

Providing the user input of: *"Will there be restrooms?"*

When I originally ran the demo with the description **"Provides content for any question asked by the user"**, OpenAI returned a prompt of "restrooms"

![Function Call](/img/2024-02-11_img7.jpg)

![Function Call](/img/2024-02-11_img6.jpg)

However, when I changed the description to **"Provides the exact prompt the user entered. DO NOT SHORTEN."**, OpenAI returned a prompt containing the exact phrase I entered for input.

![Function Call](/img/2024-02-11_img8.jpg)

![Function Call](/img/2024-02-11_img9.jpg)

As always, you'll need to play around with the descriptions that provide the best results for you.

## Other resources

* OpenAI documentation on [Function Calling](https://platform.openai.com/docs/guides/function-calling)
* [How to call functions with chat models](https://cookbook.openai.com/examples/how_to_call_functions_with_chat_models)
* Microsoft documentation: [How to use function calling with Azure OpenAI Service (Preview)](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling?tabs=python)
* Michael has some resources on the bottom of his blogs as well: [Recursive Azure OpenAI Function Calling](https://blazorhelpwebsite.com/ViewBlogPost/10068) and [Azure OpenAI RAG Pattern using a SQL Vector Database](https://blazorhelpwebsite.com/ViewBlogPost/11068) 
* Video: [Chat Completion API Tools & Functions in RAG Chat Apps](https://www.youtube.com/watch?v=ccyF5nMiA68)

## Other related demos

* [Demo Review: Simple RAG using Blazor, SQL Server and Azure OpenAI](/2024/02/07/simple-rag-sql-openai/)

If you have a comment please message me @haleyjason on twitter/X.