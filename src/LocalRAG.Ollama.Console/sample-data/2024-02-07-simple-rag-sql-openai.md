# Demo Review: Simple RAG using Blazor, SQL Server and Azure OpenAI

Are you a full stack C# developer attempting to get up to speed on all this GenAI stuff? Are you typically a relational database developer (ie. SQL Server) wondering what all the hoopla is around vector databases and more importantly how all this stuff relates to some type of functionaly that you have a chance at really using in your day-to-day work? Oh and you probably have not had time to dig into this pattern named Retrieval Augmented Generation (RAG) due to being too busy implementing web application or service code. 

If so, then fear not! This demo is a fantastic place for you to start. It uses your skillset to get a RAG application up and running without introducing too many things outside of what you already know ... and once it is up and running you will see the power of the RAG pattern. At that point you can start your list of where you want to go next and what else you'll need to learn to get there.

## Demo Details

| Item of Interest | As Of 2/7/2024 |
|----------|----------|
| Author: | Michael Washington (@ADefWebserver) |
| Date created: | 7/15/2023 |
| Updated within last month: | No |
| Link to source code: | https://blazorhelpwebsite.com/filedownloads  <br/> **NOTE:**  you have to login to download code|
| Link to documentation: | https://blazorhelpwebsite.com/ViewBlogPost/10067 |
| Github project link: | Not on GitHub |
| Knowledge needed to use: | C# <br/> SQL Server |
| Prerequisites to get demo working: | Azure Open AI (described in his article) <br/> **NOTE:** Can use OpenAI's API with a few small modifications |
| Knowledge would be helpful but not required: | Blazor <br/> Radzen.Blazor <br/> EF Core <br/> Azure <br/> Open AI APIs <br/> Vector Embeddings |
| Technologies Used: | ASPNET 7 <br/> C# <br/> Blazor <br/> SQL Server <br/> SQL Function <br/> ColumnStore index<br/> Azure Open AI API <br/> Simple chunking technique |

## Demo Description

This demo is the first demo that helped me get my head around how useful and powerful the RAG pattern applications can be. Michael Washington provides the walkthrough of how to build the application on his site: [Azure OpenAI RAG Pattern using a SQL Vector Database](https://blazorhelpwebsite.com/ViewBlogPost/10067). If you [register](https://blazorhelpwebsite.com/Identity/Account/Register) on his site, you can get the full code zip from his [downloads](https://blazorhelpwebsite.com/filedownloads) page.

When I tested the demo code, I added the text from 32 pages of a 34 page PDF file copying and pasting the text from a single page at a time into his Load Data dialog. The file I used was the [2024 Runner Guide](http://cocodona.com/wp-content/uploads/2023/12/Runner-Guide-Cocodona-2024-231203.pdf) from [https://cocodona.com/race-info/](https://cocodona.com/race-info/). The document is about Cocodona250, an ultra marathon race held in Arizona (knowing this should help the screenshots make more sense).

### Some Screenshots:

Loading the data
![Loading the data](/img/2024-02-07_img1.jpg)

Asking questions about the race(s)
![Question 1](/img/2024-02-07_img2.jpg)

![Question 2](/img/2024-02-07_img3.jpg)

![Question 3](/img/2024-02-07_img4.jpg)

Since 2024 is not the first Cocodona race, if you were to ask those same questions on OpenAI's ChatGPT you would get more general answers and no specific dates. However using the RAG pattern I can easily chat with the pdf contents I uploaded to find out the most recent information - this highlights how useful this can be.

## How to get it up and running

In order to get this demo up and running:
1. [Register](https://blazorhelpwebsite.com/Identity/Account/Register) on Michael's site
2. Go to his [Downloads Page](https://blazorhelpwebsite.com/filedownloads) and find **"Azure OpenAI RAG Patterna using a SQL Vector Database"** and download the zip.

![Downloads](/img/2024-02-07_img5.jpg)

3. Extract the files somewhere on your machine and open the **AzureOpenAIChat.sln** in Visual Studio 2022
4. I found it quicker to create a new SQL Database locally and then run the /!SQL/01.00.00.sql file than the steps he details in his blog.

![SQL file](/img/2024-02-07_img6.jpg)

5. Open the appsettings.json file and change the connection string for the database you just created.

![Connection String](/img/2024-02-07_img7.jpg)

5. Read through his walkthrough on his blog to get everything else: [Azure OpenAI RAG Pattern using a SQL Vector Database](https://blazorhelpwebsite.com/ViewBlogPost/10067)

### Alternative: Use OpenAI API
This is completely optional, but if you don't have access to Azure OpenAI (yet), you can use the OpenAI's API by making these changes:

1. Go to [https://platform.openai.com/api-keys](https://platform.openai.com/api-keys) login and create an new API key:

![OpenAI key](/img/2024-02-07_img12.jpg)

2. In the appsettings.json, change the Endpoint and the Key to use the OpenAI api url and the secret key you just created:

![OpenAI settings](/img/2024-02-07_img8.jpg)

3. In the Index.razor file, change the OpenAIClient constructor call to only take the Key (lines 248 - 250) also lines 360 - 362. And change line 390 to be the "gpt-3.5-turbo"

![OpenAI settings](/img/2024-02-07_img9.jpg)
![OpenAI settings](/img/2024-02-07_img10.jpg)

4. In the RagChat.razor file, make the same change on lines 116 - 118:

![OpenAI settings](/img/2024-02-07_img11.jpg)

Now you should be up and running. Load some data into the system and try it out.

## Points of Interest

These are some items in the demo that are RAG feature related and you may revisit later when you start creating your own RAG application. 

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

## Other resources

* [Vector Similarity Search with Azure SQL database and OpenAI](https://devblogs.microsoft.com/azure-sql/vector-similarity-search-with-azure-sql-database-and-openai/) and related repo: [azure-sql-db-openai](https://github.com/Azure-Samples/azure-sql-db-openai/tree/main)
* Michael has some resources on the bottom of his blog as well: [Azure OpenAI RAG Pattern using a SQL Vector Database](https://blazorhelpwebsite.com/ViewBlogPost/10067)

## Other related demos

* [Demo Review: Simple RAG using SQL Server, OpenAI and Function Calling](/2024/02/11/simple-rag-sql-openai-functioncall/)

If you have a comment please message me @haleyjason on twitter/X.