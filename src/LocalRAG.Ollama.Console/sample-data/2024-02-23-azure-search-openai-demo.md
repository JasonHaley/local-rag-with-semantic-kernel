# Demo Review: Azure Search OpenAI Demo (Python)

This is the last in the [family of Azure Search OpenAI](https://github.com/Azure-Samples/azure-search-openai-demo?tab=readme-ov-file#announcing-javascript-net-and-java-samples-based-on-this-one-in-python-learn-more-at--httpsakamsazai) demos that I'm covering (I'm not looking at the Java version). I reviewed the [C# version](/2024/02/14/azure-search-openai-demo-csharp/) and the [Javascript/Typescript version](/2024/02/19/azure-search-openai-javascript/) earlier this month. Of the three I'm covering, this one seems to be the most active, popular and have the most documentation.

At the beginning of this month, the [Hack Together: The AI Chat App Hack](https://github.com/microsoft/AI-Chat-App-Hack) used this demo at the sample repository, marking it as a solid reference implementation for RAG.

> NOTE: That event included several RAG themed sessions that were recorded and are available on a [youtube playlist](https://www.youtube.com/playlist?list=PL5lwDBUC0ag6_dGZst5m3G72ewfwXLcXV).

In case you are wondering what the differences are between the demos (besides the language they are written in), here is a feature comparison of the three azure search openai demos as of 2/23/2024:
| Feature | C# version | Javascript version | Python version |
|---------|------------|--------------------|----------------|
| <u>Frontend</u> | Blazor <br /> ASP.NET | Web Components <br/> React <br/> Node | React <br/> Python |
| <u>Backend</u> | Minimal API <br/> ASP.NET | Fastify <br/> Node | Quart <br/> Python |
| <u>RAG approaches</u> | 1 | 2 by default (3 total) | 2 by default (4 total) |
| <u>Framework</u> | Semantic Kernel | LangChain | None (uses OpenAI tools calling) |
| <u>Document Loaders</u> | UI <br/> Function watching storage container <br/> PostProvision hook | PostProvision hook | PostProvision hook |
| <u>File types</u> | .pdf* | .pdf, .txt, .md | .pdf, .json, .docx, <br/> .pptx, .xlsx, .png, <br/>.jpg, .jpeg, .tiff, <br/>.bmp, .heic|
| <u>Citations</u> | Yes | Yes | Yes |
| <u>Follow-up Questions</u> | Yes | Yes | Yes |
| <u>UI Layer Host</u> | Azure Container App | Azure Static Web App | Azure Web App |
| <u>Service Layer Host</u> | Azure Function for document loading | Azure Container Apps <br/> search API <br/> indexer API | None |
| <u>Number of Azure resources</u> | 15 | 10 | 11 |

<i>*work is in progress for adding images</i>

## Demo Details

|Item of Interest|As of 2/23/2024|
|----------|----------|
|Author:|64 contributors|
|Date created:| 2/5/2023 |
|Update within last month:|Yes|
|Link to documentation:|[Project's Readme](https://github.com/Azure-Samples/azure-search-openai-demo)|
|Github project link:|[https://github.com/Azure-Samples/azure-search-openai-demo](https://github.com/Azure-Samples/azure-search-openai-demo)|
|Watching:|216|
|Forks:|2.9k|
|Stars:|4.8k|
|Knowledge needed to use:| Python 3.9, 3.10 or 3.11 <br/> Azure services|
|Prerequisites to get demo working:|Azure account <br/> Access to Azure OpenAI ([apply here](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUNTZBNzRKNlVQSFhZMU9aV09EVzYxWFdORCQlQCN0PWcu))<br/> <u>If you follow my instructions below you'll need:</u> <br/> Git  <br/> Azure Developer CLI  <br /> Python 3.9, 3.10 or 3.11 <br/> Node 14+ | 
|Knowledge would be helpful but not required:| Python <br/>	Node <br/> React <br/> Azure services below <br/> Open AI APIs|
|Technologies Used:| Python <br/> Node <br/> React <br/> Azure Web Apps <br/> Azure OpenAI <br/> Azure Storage <br/> Azure AI Search (aka Azure Search and Azure Cognitive Search) <br/> Azure AI Document Intelligence (aka Form Recognizer)|

## Demo Description

I did a file comparison of this demo with the [Javascript/Typescript version](/2024/02/19/azure-search-openai-javascript/) and it looks like the React application is related (has blocks of code that are the same). However, I'm not sure if this repo is the source or some other Microsoft chat demo (there are several that have the same look) ... just an interesting piece of trivia is all.

In this demo there are just two layers - the React application and the python application. Below is a screenshot of the landing chat page:

![Chat page](/img/2024-02-23_img2.jpg)

### A look at the User Interface (UI)

In the application code, the React application (what I'm referring to as the UI) is in the `/app/frontend` directory.

![Frontend](/img/2024-02-23_img8.jpg)

Like other chat applications, there are a few sample question buttons for you to try out the application. When I click on the far left sample question button, with the "*What is included in my Northwind Health Plus plan that is not in standard?*" - It get a nice answer back with a citation. When I click on the lightbulb button to show the **Thought Process**, it shows a very nicely styled panel with information about the flow of the application.

![Chat page with Analysis Panel Open](/img/2024-02-23_img3.jpg)

The **Supporting Content** tab, shows a nicely formatted list of the text chunks found in the retrieval step:

![Supporting Content](/img/2024-02-23_img4.jpg)

If you click on the citation link in the answer, the full pdf is opened in an IFRAME and navigated to the page mentioned in the citation - that is pretty nice.

![Citation preview](/img/2024-02-23_img5.jpg)

> NOTE: as of 2/23/2024 there seems to be a bug where the citation document doesn't load if the Analysis Panel is already visible - so you may need to click on the Thought Process tab link two times to hide the panel (then click on the citation link in the answer again) before getting the citation document to show.

There is a Developer Settings button in the upper right corner that toggles a settings panel that will allow you to change the following:

<u>For calling OpenAI</u>
* Prompt Template (this is used in the final call to OpenAI)
* Temperature (also used in the final call to OpenAI)
* Suggest follow-up questions
* Stream chat completion responses

<u>For retrieving from Search</u>
* Exclude category (as of 2/23/2024 the default prepdocs.ps1 call to the loading of files does not set a category in the index)
* Number of Search results to Use
* Use semantic ranker
* Use query-contextual summaries instead of whole documents
* Retrieval mode (Text, Vectors, Hybrid)

![Document Settings](/img/2024-02-23_img6.jpg)

Like the javascript demo version, there is another RAG approach available in the UI at the top on the **Chat** or **Ask a question** tab (notice there are two links):

![Chat or Ask a question](/img/2024-02-23_img7.jpg)

The ask page is almost the same as the chat, though the layout is a little more compact. The question is at the top, while the answers are at the bottom and the Developer Settings doesn't allow changing the streaming completions or suggesting follow-up questions. Other than those few things the UI looks about the same. The backend has different RAG approaches between the Chat side and the Ask side - so it is interesting to ask questions and view the **Thought Process** for each side to see the different results.

### A look at the Backend

The backend API is a python application in the `/app/backend` directory.

![Backend](/img/2024-02-23_img9.jpg)

> NOTE: Taking into account this round of Demo Reviews is only covering the default scenario - I won't be covering the optional configurations here such as authentication/authorization and vision/images. In future parts I'd like to cover the optional configuration and the deployment architecture in Azure.

The most interesting logic in the backend is the API and the code that deals with the RAG approaches:
* [app.py](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/app/backend/app.py)
* [approach.py](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/app/backend/approaches/approach.py)
* [chatapproach.py](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/app/backend/approaches/chatapproach.py)
* [chatreadretrieveread.py](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/app/backend/approaches/chatreadretrieveread.py)
* [retrievethenread.py](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/app/backend/approaches/retrievethenread.py)

#### app.py ####

This is the where the website and API endpoints are served. Below is a map of the endpoints in the `app.py` file:

| URL | Method | Description |
|-----|--------|-------------|
| / | GET | Returns the index.html file (with the React Application) |
| /redirect | GET | Part of the login/authentication logic |
| /favicon.ico | GET | Returns the static file for the favicon.ico |
| /assets/:path | GET | Returns static file given a path
| /content/:path | GET | Used to return files from blob storage |
| /ask | POST | API endpoint for chat functionality on the **Ask a Question** page of the application |
| /chat | POST | API endpoint for chat functionality on the **Chat** page of the application |
| /auth_setup | GET | Used with authentication logic |
| /config | GET | Returns json showing the currently configured values for a few config settings |

The bulk of the logic for the chat functionality is factored nicely in the approach files, so the majority of the `/ask` and `/chat` logic is gathering configuration information, taking in the request, passing off to the proper approach logic then handling the response to the client app.

Other than the endpoints, the `app.py` file is web application startup and configuration code. The API clients used in the application (such as clients to interact with Azure Search, Azure Blob Storage, Azure OpenAI, etc.) are part of the initialization done in this file. If you are having problems with one of the clients not working correctly - you may want to take a look at the `app.py` file. It looks like all the environment variables used in the application are all retrieved in this file too - that could be something you need when troubleshooting (like either set a breakpoint or add some logging).

#### approach.py

The `approach.py` declares some data classes using in the searching as well as a base class for the approach logic.

#### chatapproach.py

`chatapproach.py` provides common base functionality needed with the different approach classes, such as prompt templates and other common logic used by the chat approaches (but not the ask approaches).

#### chatreadretrieveread.py 

This chat approach uses multiple calls to OpenAI. First OpenAI is called to get the best search query, then uses that to query the search index. With the results from the Azure Search contents, the original user question and other prompt templates - OpenAI is then asked to provide the answer for the user. Call to the search is done using the OpenAI tools (aka function calling) that we first saw in [Demo #2](/2024/02/11/simple-rag-sql-openai-functioncall/)

![ChatReadRetrieveRead](/img/2024-02-23_img10.jpg)

#### retrievethenread.py 

The `retrievethenread.py` is a simple approach. Depending on the settings, it will get the embeddings for the user question, call search to get the top number of matches and then build a prompt to call OpenAI to provide an answer to the user. Only a single call to OpenAI.

## How I got it up and running

The repository is designed to be built and deployed to Azure using the **Azure Developer CLI (AZD)**, if you don't have it installed you will need to [install it](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-windows). ([AZD documentation home](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/reference))

You will also need to be approved for the Azure OpenAI services on your Azure subscription before getting it up and running. [Apply Here](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUNTZBNzRKNlVQSFhZMU9aV09EVzYxWFdORCQlQCN0PWcu) - it usually takes around 24 hours to get approved.

You'll need one of the versions of [Python installed 3.9, 3.10 or 3.11](https://www.python.org/downloads/) - I have 3.11 installed.

I also use [NVM](https://github.com/coreybutler/nvm-windows) to have multiple versions of Node and installed the latest 20.11.0 version before starting.

1. Clone the [repo](https://github.com/Azure-Samples/azure-search-openai-demo) to your local drive
2. Open VS Code and a PowerShell terminal (or open command line to demo's root directory to run AZD)
5. Run `azd auth login` and login to your Azure subscription that is approved for Azure OpenAI.
6. Run `azd up`

You will need to choose the subscription and location (eastus has been good for me).

The whole process has been taking around 10 - 20 minutes to deploy, *though the document loading can take longer*.

> NOTE: if you run the deployment multiple times, the document loading step uses .md5 files in the /data directory to see if a file has changed and will only upload the new or changed files.


Once the deployment is complete you should see a web app url close to the bottom of the output in the terminal

![Web app link](/img/2024-02-23_img1.jpg)

Navigate to that site to verify your demo deployed and is working.

**NOTE: Do not forget these resources will be costing money**

To remove everything, the best way is to run `azd down --purge` it will remove all resources and ask you to verify final deletion. You can delete the resource group **but Azure OpenAI and Azure AI Document Intelligence do a soft delete** when you do that, so if you want to purge those resources you will need to manually do it - this is why `azd down --purge` is better and does take some time.

## Points of Interest

These are some items in the demo that are RAG feature related and you may revisit later when you start creating your own RAG applications.

### Storing vector embeddings in Azure AI Search

This demo uses [Azure AI Search](https://learn.microsoft.com/en-us/azure/search/search-what-is-azure-search) for storing the vector embeddings, content to be indexed, metadata about that content, perform a similarity search on the index, optionally perform a semantic search, etc. The `approach.py`  file uses the Azure `SearchClient` library to perform searches.

The Document loader - [prepdocslib](#document-loader) item below has more information on interacting with the Azure Search index.

### Use of Azure AI Document Intelligence to parse PDF files

[Azure AI Document Intelligence](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/overview?view=doc-intel-4.0.0) is optionally used for parsing pdf files - this is an important feature in order to get the chunk size of your documents down to have more relevant vector embeddings. This demo uses it to parse the .pdf, .json, .docx, .pptx, .xlsx, .png, .jpg, .jpeg, .tiff, .bmp, .heic files. The latest version's [Layout model](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/concept-layout?view=doc-intel-4.0.0) added support for office and html files. 

There is also the option to use a local pdf parser if you add the `--localpdfparser True` to the `prepdocs.py` in `prepdocs.ps1` instead of using the Azure Document Intelligence library for parsing.

### Streaming chat completions

One common user experience feature this demo has, is the option to stream the results as they come in from the server - instead of waiting until the full text is ready to show. This is can be toggled on and off in the Developer Settings.

OpenAI has more information on [how to stream completions](https://cookbook.openai.com/examples/how_to_stream_completions)

### Chunking technique

The chunking technique used on the text is in the [`/scripts/prepdocslib/textsplitter.py`](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/scripts/prepdocslib/textsplitter.py) file. The default settings (shown below) look to be the same as the [C# demo](/2024/02/14/azure-search-openai-demo-csharp/#chunking-technique) and the [Javascript demo](/2024/02/19/azure-search-openai-javascript/#chunking-technique).

```
self.sentence_endings = [".", "!", "?"]
self.word_breaks = [",", ";", ":", " ", "(", ")", "[", "]", "{", "}", "\t", "\n"]
self.max_section_length = 1000
self.sentence_search_limit = 100
self.section_overlap = 100
```

Like the C# and Javascript demos, it also deals with html tables. If you are writing your own text chunking code, you may want to check out the `textsplitter.py` file to learn more.

### Document loader

The document loader logic is in the [`/scripts/prepdocslib`](https://github.com/Azure-Samples/azure-search-openai-demo/tree/main/scripts/prepdocslib) directory. The document loader provides functionality to load files into blob storage, parse the files into smaller chunks, get vector embeddings for those chunks and add them to an Azure Search index. Here is a high-level description of the files in this loader:

| Name | Description |
|------|-------------|
| `blobmanager.py` | Provides the uploading and removing of files in blob storage. |
| `embeddings.py` | Contains several classes for providing text embeddings for both OpenAI and Azure OpenAI. There is also logic for creating image embeddings using [Azure AI Vision](https://learn.microsoft.com/azure/ai-services/computer-vision/how-to/image-retrieval#call-the-vectorize-image-api). |
| `fileprocessor.py` | A class that encapsulates a parser and a splitter. |
| `filestrategy.py` | Provides functionality to ingest local files or files in data lake storage into Azure Search |
| `integratedvectorizerstrategy.py` | Provides functionality to ingest files in a storage account |
| `jsonparser.py` | Converts json text into a `Page` |
| `listfilestrategy.py` | Contains a few data classes used with file parsing and provides functionality for checking md5 files and listing of files |
| `page.py` | A couple of data class: `Page` and `SplitPage` |
| `parser.py` | Abstract base class for parser objects |
| `pdfparser.py` | Contains a couple of pdf file parsers: `LocalPdfParser` (uses pypdf) and `DocumentAnalysisParser` (uses Azure Document Intelligence) |
| `searchmanager.py` | Provides functionality to create indexes, update or remove sections in indexes. |
| `strategy.py` | Abstract base class for strategy objects. | 
| `textsplitter.py` | Contains an abstract base class for TextSplitter and two implementations: `SentenceTextSplitter` (used for most file types) and `SimpleTextSplitter` (used for .json files) |

The document loader runs as a postprovision hook when AZD is provisioning the application. With the latest version of AZD, you can rerun this hook with `azd hooks run postprovision` if you find you need to load some additional files.

That hook kicks off the `prepdocs.ps1` which loads the environment variables, then calls the `/scripts/prepdocs.py` with several arguments. That file utilizes the utilities in the `prepdocslib` mentioned above, to load all the files under the `/data` directory. Once a file is loaded, a .mf5 file is created - so the second time you run the loader, it will only load files that have changed or don't have a .md5 file.

More information can be found in the [`data_ingestion.md`](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/docs/data_ingestion.md) file.

### OpenAI Tools/Function calling

In the [`chatreadretrieveread.py`](#chatreadretrieveread.py) section above, shows the detail of the setup for the `search_sources` function call. The screenshot below is my attempt at showing all the information:

![OpenAI tools call](/img/2024-02-23_img11.jpg)

1. The function metadata is declared in JSON
2. The call to OpenAI passes the function metadata in the tools parameter
3. When OpenAI responds, there is a `tools_calls` field with a function name of "search_sources"
4. The `get_search_query()` method verifies the function is to be called and parses the **search_query** parameter and value from the JSON passed back from OpenAI

After #4 the search service can be called.

As mentioned in [Simple RAG using Blazor, SQL Server, Azure OpenAI and Function Calling](/2024/02/11/simple-rag-sql-openai-functioncall/) the descriptions provided in the function metadata are important, so you may need to play around with them to get the right flow working for you. More detail can be found here: [How to use function calling with Azure OpenAI Service (Preview)](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling?tabs=python)

### HTTP Protocol for AI Chat apps

This application uses the [HTTP protocol for AI chat apps ](https://github.com/Azure-Samples/ai-chat-app-protocol) for the chat protocol. This may be useful if you find the need to change out backends for your chat applications.

## Other resources

* Videos: [YouTube playlist from RAG Hackathon](https://www.youtube.com/playlist?list=PL5lwDBUC0ag6_dGZst5m3G72ewfwXLcXV)
* [Additional Documentation](https://github.com/Azure-Samples/azure-search-openai-demo/blob/main/docs/README.md)

## Other related demos

- [Azure Search OpenAI Demo C#](/2024/02/14/azure-search-openai-demo-csharp/)
- [Azure Search OpenAI Javascript/Typescript](/2024/02/19/azure-search-openai-javascript/)

If you have a comment please message me @haleyjason on twitter/X.