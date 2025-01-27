# Announcing: Build a RAG App using Semantic Kernel, Entity Framework and Azure SQL DB Hands-on-Lab

A couple of weeks ago after [Azure SQL DB announced their public preview of native vector support](https://devblogs.microsoft.com/azure-sql/exciting-announcement-public-preview-of-native-vector-support-in-azure-sql-database/), I put a hands on lab together to help people get started with the new feature and EF Core extension. Bill Wilder and I did mentioned it in our presentations on [December 12](https://jasonhaley.com/2024/11/12/november-2024-virtual-boston-azure-meetup/) and [December 15](https://jasonhaley.com/2024/11/15/november-2024-virtual-boston-azure-lunchtime-meetup/), but I haven't had a chance to announce it for those of you who missed our presentations.

## Overview

The hands on lab: [Build a RAG App using Semantic Kernel, Entity Framework and Azure SQL DB](https://github.com/JasonHaley/develop-rag-app-sk-ef-azure-sql-db) provides a step-by-step walk through creating a RAG application using SQL Azure DB, EntityFramework Core and Semantic Kernel. I used VS Code for the steps, but if you are an experienced .NET developer using Visual Studio, you'll be able to translate pretty easy and use VS instead. If I get some time soon, I'll provide a VS version.

There are two parts (like with most RAG apps): the part about getting the data setup and the part about building the application that uses the data and an LLM.

* [Introduction and prerequisites](https://github.com/JasonHaley/develop-rag-app-sk-ef-azure-sql-db?tab=readme-ov-file#build-a-rag-app-using-semantic-kernel-entity-framework-and-azure-sql-db) - gives an overview and things you'll need to get started
* [Part 1: Start with the data](https://github.com/JasonHaley/develop-rag-app-sk-ef-azure-sql-db/blob/main/part1-1.md#part-1-start-with-the-data) - creates the database, overview of what is provided, setup the database in the code, EF Core configuration to utilize the VECTOR data type, parsing and saving a pdf file to the database
* [Part 2: Create the Retriever, Add Semantic Kernel and Plugins](https://github.com/JasonHaley/develop-rag-app-sk-ef-azure-sql-db/blob/main/part2-1.md#part-2-create-the-retriever-add-semantic-kernel-and-plugins) - creates the code to perform the similarity search using EF Core and Azure SQL DB, create and configure Azure OpenAI, add Semantic Kernel, creates a plugin and walks through creating the RAG chat bot console app.

## The Final Solution

One thing I did not point out in the lab is the final solution is included in the repo as well. It is in the [/src/end folder](https://github.com/JasonHaley/develop-rag-app-sk-ef-azure-sql-db/tree/main/src/end). My assumption is, the type of developer that will actually download and do the lab, will notice that folder and figure out it is the solution. However - if you don't have time to do the lab and just want to see the final code, it is there.

## Question for you:

If you are interested in having additional labs to help you learn how to create different Gen AI functionality in C# - please reach out to me on twitter @haleyjason (or email - info@jasonhaley.com) and let me know what you are looking for. 

Also, I am an independent consultant (in case you didn't know) and am available to help you with GenAI using .NET - contact me if you need help with getting a Gen AI project started.

If you have a comment, please message me @haleyjason on twitter/X.
