# BabyAGI
> [!CAUTION]
> This is a framework built by Johnny2x2 who has never held a job as a developer. 
> The purpose of this repo is to share ideas and spark discussion and for experienced devs to play with. Not meant for production use. _**This code could destroy your PC or worse. Use with caution.**_ 
---

## Requirements
* Install ChromaDB CLI and run Chroma client

* Put FunctionApplicationTemplate.zip into a safe folder with read/write access for function creation

* Add the Folder location to BabyAGIConfig in program

* OpenAI embedding requires Environment Variable "OPENAI_API_KEY" set

## Basic Principle
* Agent has tool to try to complete task from generated functions
* When the tool is called it will query existing tools to see if It already has one to use
* If no tool is appropriate then the coding agent will generate Functions 
* Generated functions are built C# EXE console applications
* Function descriptions are saved to a vector database as they are generated for later retrieval
* Functions have examples of how to use them in each folder and that information is used to generate input args
* Functions are ran and generated in a loop until a reviewing agent says it has enough information
* Collected Information is then sent to a writer agent who generates the assistant message

## Known Issues

- Without vision certain task probably will not complete (need to add that in and figure out how to deal with image memory).
- Takes some persuasion to get AI to use tool
- Has no persistent memory yet
- Infinite loops are certainly possible and will drain your API account



Inspired by: [yoheinakajima/babyagi](https://github.com/yoheinakajima/babyagi)
