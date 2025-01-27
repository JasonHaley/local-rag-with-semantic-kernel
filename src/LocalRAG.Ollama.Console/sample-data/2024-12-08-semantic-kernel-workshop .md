# Announcing: A Semantic Kernel Workshop

Friday Bill Wilder, Juan Pablo Garcia Gonzalez and I finally were able to make the workshop we've been working on for the past couple of months public. We had an in person day long event where we used the content, which was received really well. The attendees had some good questions - giving us some really good ideas on how to improve the content.

**Target audience:** .NET developers looking to start using Gen AI in their applications

## Overview

The workshop (currently has a long name which we're planning on shortening soon): [Building RAG-Enabled Apps and AI Agents with Semantic Kernel and Azure AI](https://github.com/bostonazure/rag-vector-agent-semantic-kernel), contains 6 labs and (currently) 2 presentations.

We have both Visual Studio Code and Visual Studio versions of the first 5 labs (I'm working on getting the last lab also available for Visual Studio soon). There is a progression from just getting Semantic Kernel added to the application, creating functions, using plugins, implementing RAG and ending with Agents.

The following is taken from the github site.

### [Lab 0: Can we just access the dang API?](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab0/readme.md)
**Focus:** Accessing APIs and running a simple SK console app. <br/>
**Objectives:** Get local copies of API keys, run a simple SK console app. <br/>
**Additional Exercises:** Experiment with different API endpoints. <br/>
**Further Ideas:** Explore different API authentication methods. <br/>

### [Lab 1: Getting Started with Semantic Kernel](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab1/readme.md)
**Focus:** Adding Semantic Kernel to an application, using Azure OpenAI, and creating prompt functions.<br/>
**Objectives:** Demonstrate how to add Semantic Kernel to an existing application, use Semantic Kernel to chat with the Azure OpenAI LLM, define a prompt function and use it in an application, recognize the need for chat history and how to add it.<br/>
**Additional Exercises:** Experiment with different Temperature values to see their influence.<br/>
**Further Ideas:** Explore different prompt engineering techniques.<br/>

### [Lab 2: Creating Semantic Kernel Plugins](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab2/readme.md)
**Focus:** Creating native plugins and using web search plugins.<br/>
**Objectives:** Implement a plugin with native C# code, use a plugin to give an LLM additional information, create a plugin that uses an LLM to rewrite a user query, utilize a Semantic Kernel plugin to perform a web search.<br/>
**Additional Exercises:** Experiment with different plugin functions.<br/>
**Further Ideas:** Explore different ways to integrate plugins with Semantic Kernel.<br/>

### [Lab 3: Using WebRetrieverPlugin to create a RAG application](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab3/readme.md)
**Focus:** Creating a RAG application using web search results.<br/>
**Objectives:** Build a plugin to combine the rewriting of a user's query and a web search, write a prompt to perform a basic RAG pattern call to an LLM, implement a simple chatbot loop, demonstrate the usefulness of a RAG implementation.<br/>
**Additional Exercises:** Experiment with different web search engines.<br/>
**Further Ideas:** Explore different ways to integrate web search results with Semantic Kernel.<br/>

### [Lab 4: Creating a RAG application to Search a PDF](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab4/readme.md)
**Focus:** Creating a RAG application to search a PDF using a vector store.<br/>
**Objectives:** Configure a vector store to use with the application, read, chunk and ingest a pdf file, implement logic to perform a similarity search on the vector store, create a plugin to perform RAG using the memory store.<br/>
**Additional Exercises:** Experiment with different PDF files.<br/>
**Further Ideas:** Explore different ways to integrate PDF search results with Semantic Kernel.<br/>

### [Lab 5: Putting it all together](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab5/readme.md)
**Focus:** Integrating all previous labs and adding logging and user intent determination.<br/>
**Objectives:** Use filters to add logging and understand the call flows, have the LLM determine which plugin functions to call, create a plugin to determine the user's intent, dynamically control the functions available to the LLM depending on the user's intent.<br/>
**Additional Exercises:** Experiment with different logging techniques.<br/>
**Further Ideas:** Explore different ways to integrate logging and user intent determination with Semantic Kernel.<br/>

### [Lab 6: Semantic Kernel Agent Lab](https://github.com/bostonazure/rag-vector-agent-semantic-kernel/blob/main/labs/lab6/README.md)
**Focus:** Building agents with Semantic Kernel.<br/>
**Objectives:** Create an agent with reasoning capabilities to solve domain-specific requests, build an agent with skills to get the current weather of a city by calling a public API, create a team of agents to collaboratively solve more complex problems.<br/>
**Additional Exercises:** Experiment with different agent skills.<br/>
**Further Ideas:** Explore different ways to integrate agents with Semantic Kernel.<br/>

We are planning to do more in person day long workshops early next year, so if you are interested please reach out to me on twitter @haleyjason (or email - info@jasonhaley.com) and let me know if you are interested in future workshops on AI with .NET.

If you have a comment, please message me @haleyjason on twitter/X.
