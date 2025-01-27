This week I've been researching Text-to-SQL (also known as Natural Language to SQL), below are my study notes to compile all the resources I've found on the topic to date. There is also a [corresponding blog entry](/2024/07/06/study-notes-text-to-sql-code-sample/) that walks through a code example.

> NOTE: I am approaching this topic specifically looking at how it can be used to extend usage scenarios in a RAG application.

# Background

Text-to-SQL (or Natural Language to SQL) is a pattern where the objective is to have an LLM generate SQL statements for a database using natural language. 

# How it Works

Similar to RAG (Retrieval Augmented Generation), Text-to-SQL is achieved by augmenting the prompt passed to the LLM with necessary schema information to help the LLM construct the proper SQL statement for the user request.

> NOTE: Text-to-SQL is just the creation of the SQL, you need to provide a way to execute the SQL returned from the LLM.

Example of a [Semantic Kernel](https://github.com/microsoft/semantic-kernel) prompt:
```xml
<message role="system">
Generate a SQL SELECT query that is compatible with {{$data_platform}} and achieves the OBJECTIVE exclusively using only the tables and views described in "SCHEMA:".

Only generate SQL if the OBJECTIVE can be answered by querying a database with tables described in SCHEMA.
</message>
<message role="system">
Respond with only with valid SQL
</message>
<message role="user">
SCHEMA:
  description: historical record of concerts, stadiums and singers
  tables:
    - stadium:
      columns:
        Stadium_ID:
        Location:
        Name:
        Capacity:
        Highest:
        Lowest:
        Average:
    - singer:
      columns:
        Singer_ID:
        Name:
        Country:
        Song_Name:
        Song_release_year:
        Age:
        Is_male:
    - concert:
      columns:
        concert_ID:
        concert_Name:
        Theme:
        Stadium_ID:
        Year:
    - singer_in_concert:
      columns:
        concert_ID:
        Singer_ID:
        references:
  concert.Stadium_ID: stadium.Stadium_ID
    references:
    singer_in_concert.concert_ID: concert.concert_ID
    singer_in_concert.Singer_ID: singer.Singer_ID

OBJECTIVE: How many heads of the departments are older than 56 ?
</message>
<message role="assistant">
select count(*) department_head_count from head where age > 56
</message>
<message role="user">
SCHEMA:
{{$data_schema}}

OBJECTIVE: {{$data_objective}}
</message>
```
The above prompt is from the [generateQuery.xml](https://github.com/microsoft/kernel-memory/blob/NL2SQL/examples/200-dotnet-nl2sql/nl2sql.config/nl2sql/generateQuery.xml) example in the [kernel-memory project's NL2SQL branch](https://github.com/microsoft/kernel-memory/tree/NL2SQL) on GitHub.

Important things to note about the above prompt:
* **It is a few-shot prompt** - it includes a sample schema and SQL response expected from the provided schema
* **The desired SQL type is specified** - the `$data_platform` parameter in the system message is where you pass the type of SQL you need. For example: Microsoft SQL Server. This is important because of the differences in the SQL syntax supported by databases.
* **Relevant parts of your schema need to be passed to the LLM** - the `$data_schema` is where your yaml formatted schema needs to be plugged into this prompt. It will be up to you to decide what is "relevant".
* **User request is passed to LLM** - in order for the SQL to be generated, the user's natural language data request is passed in using the `$data_objective` parameter

# Use Cases

As of July 2024, these are some use cases I see Text-to-SQL could have. Most of these use cases assume there is also a system that will provide the execution of the generated SQL.

## When an answer is in the database (vs. unstructured data)

This use case would most likely be when Text-to-SQL and RAG are working together. Typical RAG implementations use a retrieval step performing a semantic search or hybrid search to find the relevant context to pass the LLM. Often the semantic search is using embeddings that have been created using unstructured data like documents for example. However, Text-to-SQL could be used in the retrieval step and possibly paired with semantic search to provide a multi-step retrieval.

### Example:
A RAG system that has employee data (like performance reviews) and their corresponding embeddings.

If a user requested: **please provide the performance data for the top 3 best performing sales agents for the quarter**

Text-to-SQL could be used to query a sales database and retrieve the top 3 best performing sales agents, then those results could be passed to the retrieval system to look up relevant information on those 3 agents.

By using Text-to-SQL in this manner, the RAG system would be improved and be able to answer a new type of user requests.

## Data exploration and analysis

This use case may help someone get up to speed with the contents and structure of a database and not require them to have top notch SQL skills to do so. On the flip side, it could help speed up the learning of how to write specific SQL statements on a database.

### Example:

A new employee, experienced with SQL, but does not know the details or exact contents of a database - could become more productive creating needed SQL statements for product features by just providing natural language requests describing what they need.

## Replace simple reports

This use case is for when simple data extracts need to be done for reporting type purposes. This could the data needed for presentations, emails, etc.

### Example:

A sales manager needs to put together a quarterly report and doesn't have the reports needed to extract the data from the CRM or Sales database. The sales manager could use Text-to-SQL to get the necessary data without needing to use IT or developer resources to do so.

> NOTE: Complicated and well formatted reports are still not going to be possible with Text-to-SQL

# Benefits

Besides making databases more accessible for non-technical users, there are some benefits I currently am looking into related to extending RAG applications.

* LLMs like ChatGPT 4 are pretty good at creating valid SQL queries for simple to intermediate scenarios
* Could be a replacement for simple reports or data extracts
* Doesn't need embeddings for a normal database query to be useful in a RAG scenario
* Can help provide general querying tools for LLM applications

# Challenges

Complex queries and security are the biggest challenges.

* Need to provide views or simple schemas for accurate generated SQL
* Read-only databases and managing application permissions is necessary to prevent unwanted data exposure or damaging SQL run on a database
* Authorization is possible, but will need to be designed in for the cases when you need to limit data access by the user
* Getting the columns in a result can be inconsistent, but improved by modifying the prompt
* Does not provide any sort of semantic searching capability

# Conclusion

Text-to-SQL is a powerful tool that can be used when a database query is the right choice to answer a question. One such scenario may be to extend a RAG application to use a database during the retrieval phase.

If you are curious about Text-to-SQL I encourage you to check out the references below for more detail or reach out to me on twitter @haleyjason.

I personally think Text-to-SQL has a place in a RAG application, though I do see it as more of a general purpose and exploratory tool and think the tools/functions in my RAG applications will instead use the data accessing you would find in a normal business application.

# References

This is a list of resources I have found useful for learning about Text-to-SQL.

* [Chat with SQL and Tabular Databases using LLM Agents (DON'T USE RAG)](https://www.youtube.com/watch?v=ZtltjSjFPDg&t=2175s) and the related GitHub repo: [https://github.com/Farzad-R/Advanced-QA-and-RAG-Series](https://github.com/Farzad-R/Advanced-QA-and-RAG-Series) (Python)
* [Discover LlamaIndex: Joint Text to SQL and Semantic Search](https://www.youtube.com/watch?v=ZIvcVJGtCrY) - code in video is Python
* [RAG on structured data with PostgreSQL](https://techcommunity.microsoft.com/t5/microsoft-developer-community/rag-on-structured-data-with-postgresql/ba-p/4164456) and the related GitHub [https://github.com/Azure-Samples/rag-postgres-openai-python/tree/main/scripts](https://github.com/Azure-Samples/rag-postgres-openai-python/tree/main/scripts)
* [Use natural language to execute SQL queries](https://devblogs.microsoft.com/semantic-kernel/use-natural-language-to-execute-sql-queries/) and [Semantic Kernel: Natural Language to SQL](https://aka.ms/sk-nlsql-blog-video) and its related GitHub repo branch: [https://github.com/microsoft/kernel-memory/tree/NL2SQL/examples/200-dotnet-nl2sql](https://github.com/microsoft/kernel-memory/tree/NL2SQL/examples/200-dotnet-nl2sql)
* [Use AI to chat with a relational SQL database with natural language](https://www.youtube.com/watch?v=hw6oTjw9_Ro&t=183s) and the related GitHub repo: [https://github.com/alex-wolf-ps/chat-sql-data-demo](https://github.com/alex-wolf-ps/chat-sql-data-demo) (C#)
* [Use AI to chat with a SQL Database 2.0 - powerful new features!](https://www.youtube.com/watch?v=REw3y_Jv3Ig) and the related GitHub repo: [https://github.com/alex-wolf-ps/dbchatpro](https://github.com/alex-wolf-ps/dbchatpro) (C#)

If you have a comment, please message me @haleyjason on twitter/X.
