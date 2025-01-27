This past month I've been focusing on Graph RAG. This entry is an attempt to capture some lessons learned and a place to itemize all the resources I've found useful.

## Related posts:
* [Study Notes: Graph RAG - Property Graph RAG (The Notebook)](/2024/08/06/study-notes-graph-rag1-code-sample-notebook/)
* [Study Notes: Graph RAG - Property Graph RAG (The Projects)](/2024/08/16/study-notes-graph-rag1-code-sample-projects/)

> NOTE: My approach to this topic was to find a way to improve a typical RAG implementation that only uses vector similarity searching.

# Background

For the past 5 or 6 months, I've casually been reading about Graph RAG but not taken a deep dive into it - until this past month. The majority of the videos, articles and code samples out there labeled as "Graph RAG" are about **Knowledge or Property Graph RAG**. They typically include a graph database (Neo4j is popular), often include embeddings and are a way to store structured data as entities with relationships (and labels) as well as additional text or data. Like with typical RAG there are two stages: ingestion of data into the graph (done before inference) and retrieval (which is done when a user makes a request). Examples usually include extracting entities and relationships from unstructured data (ie. documents) using an LLM for the ingestion phase. For the retrieval phase almost all examples use either LangChain or LlamaIndex, very few show how to do it yourself.

Recently, Microsoft released [a paper](https://arxiv.org/pdf/2404.16130) and [a python library](https://github.com/microsoft/graphrag) named GraphRAG which I'll cover in Part 2 of this topic - in order to distinguish the difference between Microsoft's "GraphRAG", I'll be referring to [Knowledge Graph](https://en.wikipedia.org/wiki/Knowledge_graph) and [Property Graph](https://en.wikipedia.org/wiki/Property_graph) RAG as "Property Graph RAG".

# How it Works

Before getting into how it works, first I'll define some terms that will be used often throughout the rest of this entry:

| Term       | Definition       |
|----------------|----------------|
| Context | The information created mostly from the retrieval system to be passed to the LLM in a RAG application. |
| Vector/Embeddings RAG or Baseline RAG| Includes a retrieval system that is typically a semantic search (with optionally a hybrid search) to extract context to pass to an LLM for answering a user's question.  |
| Graph RAG | Includes a retrieval system often like Vector/Embeddings RAG but also is able to provide a sub-graph of related entities to provide additional context that is beyond a semantic match. |
| Text-to-Cypher | Much like [Text-to-SQL](/2024/07/05/study-notes-text-to-sql/) for relational databases, this is a pattern designed to have an LLM generate valid cypher syntax used to query a graph database to answer a user's question. | 
| Triplet | A triplet is an Entity -> Relationship -> Entity set used to capture how two entities are related. |

As Ben Lorica mentions in his blog [GraphRAG Design Patterns, Challenges, Recommendations](https://gradientflow.substack.com/p/graphrag-design-patterns-challenges) there are several Graph RAG architectures being used today. Here I generalize a bit and mention some of the different options I've seen. I'd recommend reading Ben's blog for a better description of different patterns he's seen.

## Ingestion Phase

The goal of the ingestion phase is to populate the graph with entities and relationships that describe the related (and usually unstructured) data. Unless you are already using a graph database, you can easily go down a rabbit hole in this phase learning about [graph theory](https://en.wikipedia.org/wiki/Graph_theory), [directed graphs](https://en.wikipedia.org/wiki/Directed_graph), [Resource Description Framework (RDF)](https://en.wikipedia.org/wiki/Resource_Description_Framework), etc. For me, I hadn't used a graph database before this past month and its been a long time since I read [Sedgewick's Algorithms in C++ Part 5: Graph Algorithms](https://www.amazon.com/Algorithms-Part-Graph-Robert-Sedgewick-ebook/dp/B001XT60YC) ... so I ended up spending way more time on this phase that I had thought I would. I ended choosing [Neo4j](https://neo4j.com/) for a graph database.

If you use python, then you should check out [LangChain's Neo4j integration](https://python.langchain.com/v0.2/docs/integrations/graphs/neo4j_cypher/) or [LlamaIndex's Neo4j Graph Store](https://docs.llamaindex.ai/en/stable/examples/index_structs/knowledge_graph/Neo4jKGIndexDemo/). If you use C#, currently there is no hope for you ... **just kidding!** (I mostly use C# myself). There just isn't a lot out there right now for code examples (this is summer 2024). My code example is in C# and and uses [Semantic Kernel](https://github.com/microsoft/semantic-kernel) - it's in my Github repo: [semantic-kernel-getting-started](https://github.com/JasonHaley/semantic-kernel-getting-started/tree/main/samples/demos/PropertyGraphRAG).

Of the resource's I've found on this ingestion phase, they fall into two categories:

* Populating a graph from a known schema
* Populating a graph and creating the schema on the fly

### Known Schema

For a good example of this, I'd recommend starting with Open AI's Cookbook: [RAG with a Graph database](https://cookbook.openai.com/examples/rag_with_graph_db) ([notebook](https://github.com/openai/openai-cookbook/blob/main/examples/RAG_with_graph_db.ipynb) in Github). It loads a dataset using pandas and neo4j.

Another good example is [KnowledgeGraph-Q&A-and-RAG-with-TabularData](https://github.com/Farzad-R/Advanced-QA-and-RAG-Series/tree/main/KnowledgeGraph-Q%26A-and-RAG-with-TabularData) which has a YouTube video to go along with it: [Chat and RAG with Tabular Databases Using Knowledge Graph and LLM Agents](https://www.youtube.com/watch?v=3NP1llvtrbI).

The idea with this approach is you know the data schema and can just add the data into the graph. I do think property graph RAG is most beneficial in this scenario because you are able to craft the retrieval system to best work with your data.

### Creating the Schema on the Fly

For a good example of this, LlamaIndex has a really good series of Youtube videos you can watch: [Property Graphs](https://www.youtube.com/playlist?list=PLTZkGHtR085ZYstpcTFWqP27D-SPZe6EZ), the code is on github at: [PropertyGraphs with LlamaIndex and MistralAI](https://github.com/mistralai/cookbook/blob/main/third_party/LlamaIndex/propertygraphs).

Or check this notebook: [Covert any text into a KG ](https://github.com/projectwilsen/KnowledgeGraphLLM/blob/main/tutorial/2_convert_any_text_into_a_kg/notebook.ipynb) using LangChain.

With this category of populating the graph, you can have an LLM parse (normally unstructured) data and extract the entities and relationships for you.

Example of a Semantic Kernel prompt that does this:

```XML
<message role="user">Please extract up to {{$maxTripletsPerChunk}} knowledge triplets from the provied text.
{{$preamble}}
Each triplet should be in the form of (head, relation, tail) with their respective types.
######################
ONTOLOGY:
Entity Types: {{$entityTypes}}
Relation Types: {{$relationTypes}}
    
Use these entity types and relation types as a starting point, introduce new types if necessary based on the context.
    
GUIDELINES:
- Output in JSON format: [{""head"": """", ""head_type"": """", ""relation"": """", ""tail"": """", ""tail_type"": """"}]
- Use the full form for entities (ie., 'Artificial Intelligence' instead of 'AI')
- Keep entities and relation names concise (3-5 words max)
- Break down complex phrases into multiple triplets
- Ensure the knowledge graph is coherent and easily understandable
######################
EXAMPLE:
Text: Jason Haley, chief engineer of Jason Haley Consulting, wrote a new blog post titled 'Study Notes: GraphRAG - Property Graphs' about creating a property graph RAG system using Semantic Kernel. 
Output:
[{""head"": ""Jason Haley"", ""head_type"": ""PERSON"", ""relation"": ""WORKS_FOR"", ""tail"": ""Jason Haley Consulting"", ""tail_type"": ""COMPANY""},
    {""head"": ""Study Notes: GraphRAG - Property Graphs"", ""head_type"": ""BLOG_POST"", ""relation"": ""WRITTEN_BY"", ""tail"": ""Jason Haley"", ""tail_type"": ""PERSON""},
    {""head"": ""Study Notes: GraphRAG - Property Graphs"", ""head_type"": ""BLOG_POST"", ""relation"": ""POSTED_ON"", ""tail"": ""Monday August 5, 2024"", ""tail_type"": ""PERSON""},
    {""head"": ""property graph RAG system"", ""head_type"": ""SOFTWARE_SYSTEM"", ""relation"": ""USES"", ""tail"": ""Semantic Kernel"", ""tail_type"": ""TECHNOLOGY""}]
######################
Text: {{$text}}
######################
Output:</message>
```

The above prompt was influenced by LangChain and LlamaIndex, though I'm starting to move it more towards the prompt Microsoft's GraphRAG uses to extract entities.

> **Lesson Learned:**
> As I implement the code sample for this topic, I found the more I modified this prompt to know more about the specific data I was parsing, the more effective the resulting graph was. Leaving me with the thought that one generic prompt for all scenarios is probably not a good idea.

## Retrieval Phase

The goal of the retrieval phase with property graph RAG, is to add entity relationship information to the context that a typical RAG system can't.

### What additional information is added to the context?

This is best explained by comparing what is in the context of typical RAG application to property graph RAG application.

**Typical RAG Context**

* A system message directing the LLM to follow the rules of your RAG system
* Usually 3 - 5 chunks of content that was retrieved using an embeddings similarity search
* The user's question

**Graph RAG Context**
* A system message directing the LLM to follow the rules of your RAG system
* ***Additional metadata about the content - usually represented as a list of multiple related entity relationships to help provide more context to the LLM***
* Usually 3 - 5 chunks of content that was retrieved using an embeddings similarity search
* The user's question

As Ben Lorica mentions in his [blog](https://gradientflow.substack.com/p/graphrag-design-patterns-challenges), there are several different architectures for Graph RAG but they all share the idea of providing additional metadata (information about the context) that typical RAG does not, thought they can get that information in different ways.

# Use Cases

As of August 2023, these are some use cases I see for property graph RAG. Most of these are edge cases for typical RAG applications.

## A RAG system that can answer questions about a global context 

This use case is when you need to get answers that require a global understanding of the material and not just relevant chunks text.

### Example:

[The 18 Mistakes That Kill Startups](https://paulgraham.com/startupmistakes.html) by Paul Graham was my original challenge for property graph RAG. There are 18 items in that essay - but you need to have access to the complete essay in order to answer the question: *What are the 18 mistakes that kill a startup*. Typical RAG will chop that essay up into pieces for creating embeddings on chunks, then the retrieval system will return only 3 - 5 chunks in the context. For example, if there were 18 chunks - there is no way the LLM can answer the question from the context alone. 

## A RAG system that can help narrow down results in noisy data

This use case would be a scenario where there is a lot of similar data in a RAG system or for some reason your embeddings are not helping you find the best results.

### Example:

If you have a system that contains a lot of similar information, a similarity search will return a lot of results that are close in the embedding space. If you only get to add 3 - 5 of those results to the context, you may not be able to provide enough relevant context to answer the user's question.

## A RAG system that can connect insights a normal RAG system can't

This use case refers to how RAG systems depend on similarity or hybrid searches of underlying data. They know about the related neighbors or text matches but don't have a way to get other types of relationships.

### Example:

The best example of this is the [Six Degrees of Kevin Bacon](https://en.wikipedia.org/wiki/Six_Degrees_of_Kevin_Bacon) game. If you have a property graph RAG application you can ask questions about related topics and get enough context to the LLM to provide an answer.

# Benefits

The benefit of property graph RAG over typical RAG is how it can provide a richer context to the LLM:

* By adding entity + relation triplet information to give the LLM more context, you get a lot more value from fewer tokens than just adding more chunks of text.
* Global concepts in your data can be added to the context using entity + relation triplets.
* Related information that can't be found by a text or vector search can be connected and surfaced in the context using relationship searches.
* Graph databases can hold full text and vector indexes providing a singular database that can keep the data.

# Challenges

There are challenges with property graph RAG on both the ingestion and retrieval phases.

* When having an LLM generate a schema and populate it, the entities and relationship names are not always what you want.
    * Some prompt engineering can help, but it may be hard to have one size fit all prompts.
* When using an LLM to extract entities during ingestion, you will be sending the full text of everything you are indexing at least once - so it ***can be expensive***.
* Once you start testing the retrieval system, you many find you still need to clean the extraction phase up a bit. Leading to reworking the data loading (which can be expensive).
* Initially you may not notice property graph RAG providing much better results than typical RAG
    * This was my case until I started expanding the types of questions I was putting through the RAG application
* Triplets add a lot of context, but you may need to modify your final request prompt to better utilize their value in the context and work around some of your use cases.
* Getting specific counts, totals or summation type answers are just as hard as with typical RAG.

# Conclusion

Property graph RAG really shows the benefits of connecting structured data with a RAG system. The biggest reasons to go with property graph RAG are the three use cases mentioned above: ability to provide answers about global knowledge, ability to focus when the data is noisy and the ability to answer questions about concepts that are related but can't be found in a similarity search.

Personally I think adding structured data to a RAG application is going to start becoming the norm in order to broaden the feature set available and property graph RAG is just the beginning of that.

If you are curious about property graph RAG I encourage you to check out the references below for more detail or reach out to me on twitter @haleyjason.

# Resources
This list only includes the property graph RAG resources I have found to be useful, the Microsoft GraphRAG resources will be in part 2:

## Graph RAG
* (short) Youtube:[Google Keynote (Google I/O ‘24)](https://www.youtube.com/clip/UgkxQEI1Z_LcuArE4Olm_V3tnb-QYXOQZgPX)
* Youtube: [Unleashing the Synergy of LLMs and Knowledge Graphs](https://www.youtube.com/watch?v=cIvDlmq24R8&t=278s)
* Blog: [GraphRAG: Design Patterns, Challenges, Recommendations](https://gradientflow.substack.com/p/graphrag-design-patterns-challenges)
* Open AI's Cookbook: [RAG with a Graph database](https://cookbook.openai.com/examples/rag_with_graph_db) ([notebook](https://github.com/openai/openai-cookbook/blob/main/examples/RAG_with_graph_db.ipynb) in Github)
* Youtube: [Chat and RAG with Tabular Databases Using Knowledge Graph and LLM Agents](https://www.youtube.com/watch?v=3NP1llvtrbI&t=1950s), related Github: [KnowledgeGraph-Q&A-and-RAG-with-TabularData](https://github.com/Farzad-R/Advanced-QA-and-RAG-Series/tree/main/KnowledgeGraph-Q%26A-and-RAG-with-TabularData)
* Youtube: [Realtime Powerful RAG Pipeline using Neo4j(Knowledge Graph Db) and Langchain](https://www.youtube.com/watch?v=Ik8gNjJ-13I)
* Youtube playlist: [Property Graphs](https://www.youtube.com/playlist?list=PLTZkGHtR085ZYstpcTFWqP27D-SPZe6EZ), related Github: [PropertyGraphs with LlamaIndex and MistralAI](https://github.com/mistralai/cookbook/blob/main/third_party/LlamaIndex/propertygraphs)
* Youtube: [The easiest way to chat with Knowledge Graph using LLMs (python tutorial)](https://www.youtube.com/watch?v=KMXQ4SVLwmo&t=329s) related notebook: [KnowledgeGraphLLM](https://github.com/projectwilsen/KnowledgeGraphLLM/blob/main/tutorial/2_convert_any_text_into_a_kg/notebook.ipynb)
* Github: [Using a Knowledge Graph to implement a DevOps RAG application](https://github.com/tomasonjo/blogs/blob/master/llm/devops_rag.ipynb)
* Deeplearning.ai course: [Knowledge Graphs for RAG](https://learn.deeplearning.ai/courses/knowledge-graphs-rag/lesson/1/introduction)
* Youtube: [Convert Any Text into a Knowledge Graph](https://www.youtube.com/watch?v=1RXBnF9dj14), related Github: [knowledge_graph](https://github.com/rahulnyk/knowledge_graph)
* Blog: [How to Convert Any Text Into a Graph of Concepts](https://towardsdatascience.com/how-to-convert-any-text-into-a-graph-of-concepts-110844f22a1a)
* Youtube: [LlamaIndex Webinar: Advanced RAG with Knowledge Graphs (with Tomaz from Neo4j)](https://www.youtube.com/watch?v=LDh5MdR-CPQ)
* Blog: [Customizing property graph index in LlamaIndex](https://www.llamaindex.ai/blog/customizing-property-graph-index-in-llamaindex)
* Youtube: [Langchain + Graph RAG + GPT-4o Python Project: Easy AI/Chat for your Website](https://www.youtube.com/watch?v=HPmO1UZwfHc&t=37s)
* Youtube: [Chat With Knowledge Graph Data | Improved RAG](https://www.youtube.com/watch?v=GU0LZ8JzdEM)
* Youtube: [NODES 2023: Knowledge Graph-Based Chatbot](https://www.youtube.com/watch?v=x9GNBKwiNbo)
* Notebook: [Enhancing RAG-based applications accuracy by constructing and leveraging knowledge graphs](https://github.com/tomasonjo/blogs/blob/master/llm/enhancing_rag_with_graph.ipynb)
* Blog: [Graph RAG vs. Vector RAG vs. Text2Cypher](https://www.nebula-graph.io/posts/graph-RAG)
* Blog: [Enhancing RAG with Knowledge Graphs: Blueprints, Hurdles, and Guidelines](https://gradientflow.substack.com/p/graphrag-design-patterns-challenges)
* Youtube: [AIQCON Conference: Kirk Marple discusses GraphRAG and Graphlit](https://www.youtube.com/watch?v=kAj2E_nNcr8)

## Neo4j
* Blog: [Neo4j with C#, Part 1 — Intro!](https://medium.com/c-sharp-progarmming/neo4j-with-c-part-1-intro-f43eba15e75c)
* Youtube: [Contextual and Semantic Information Retrieval using LLMs and Knowledge Graphs](https://www.youtube.com/watch?v=abhPWdbJ4bE)
* Discord: [GraphRAG Discord](http://discord.gg/graphrag)
* Free Ebook: [Building Knowledge Graphs: A Practitioner's Guide](https://neo4j.com/knowledge-graphs-practitioners-guide/)
* Blog: [RAG (Retrieval Augmented Generation) Demo](https://neo4j.com/labs/genai-ecosystem/rag-demo/)
* Docs: [Neo4j Vector Index and Search](https://neo4j.com/labs/genai-ecosystem/vector-search/)
* Blog: [Neo4j Links on GenAI](https://neo4j.com/labs/genai-ecosystem/)
* Youtube: [Azure OpenAI Neo4j Demo](https://www.youtube.com/watch?v=3PO-erAP6R4)
* Github: [neo4j-generative-ai-azure](https://github.com/neo4j-partners/neo4j-generative-ai-azure)
* Youtube: [Graph ML: Build Knowledge Graphs using Generative AI and LLMs](https://www.youtube.com/watch?v=sK61Y22wvr8), related Github: [neo4j-generative-ai-google-cloud](https://github.com/neo4j-partners/neo4j-generative-ai-google-cloud)
* Course: [Introduction to Vector Indexes and Unstructured Data](https://graphacademy.neo4j.com/courses/llm-vectors-unstructured/)
* Course: [Neo4j & LLM Fundamentals](https://graphacademy.neo4j.com/courses/llm-fundamentals/)
* Youtube: [RAG with a Neo4j Knowledge Graph: How it Works and How to Set It Up](https://www.youtube.com/watch?v=ftlZ0oeXYRE&t=44s)

If you have a comment, please message me @haleyjason on twitter/X.
