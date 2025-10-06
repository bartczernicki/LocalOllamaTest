# Local Ollama with Microsoft Extensions AI

## About 

Quick demo of using a local AI model (open-weights) served by Ollama with the Microsoft Extensions AI framework for .NET.  
Instructions to get started here: https://devblogs.microsoft.com/dotnet/gpt-oss-csharp-ollama/  

This demo extends the basic demo to include:  
- Visual Studio 2026, .NET 10, SLNX Project Type
- Multiple Tool Calls
- Sets custom properties for high reasoning effort
- Provides AI explainability, by surfacing both internal reasoning and internal tool calls.

Example of the prompt:  
**_"Based on the weather in New York city, what types of activities do you recommend? Use the decision intelligence framework to make the recommendations."_**  

Expected Results from the Prompt:
- Show interal Reasoning effort
- Explaing why/which tools to call 
- Surface the Function tool calls after reasoning and Properly pass in the parameters
- Apply the Decision Intelligence steps for the recommendation 

<img style="display: block; margin: auto;" width ="700px" src="https://raw.githubusercontent.com/bartczernicki/LocalOllamaTest/refs/heads/master/LocalOllamaTest/Images/LocalAI.png">
<br/>  
