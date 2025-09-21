using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace LocalOllamaTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Setup DI (optional but helpful)
            var services = new ServiceCollection();

            // Add a memory distributed cache (in‑memory for demo)
            services.AddSingleton(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

            // Add a chat client implementation; e.g. using Ollama or OpenAI
            // Here, we pretend we have OllamaChatClient package
            services.AddSingleton<IChatClient>(sp =>
            {
                var chatClient = new OllamaApiClient(new Uri("http://localhost:11434"), "gpt-oss:20b");
                // Build a pipeline with toolWeather‑invocation and caching
                var builder = new ChatClientBuilder(chatClient)
                    .UseDistributedCache(sp.GetRequiredService<MemoryDistributedCache>())
                    .UseFunctionInvocation();

                return builder.Build();
            });

            var serviceProvider = services.BuildServiceProvider();
            var chatClient = serviceProvider.GetRequiredService<IChatClient>();

            // Use AIFunction / AIFunctionFactory to wrap that. Description attribute helps.
            var toolWeather = AIFunctionFactory.Create(
                (Func<string, string>) GetCurrentWeather,
                name: "GetCurrentWeather",
                description: "Gets the current weather for a specified city"
            );

            var toolActivites = AIFunctionFactory.Create(
                (Func<string, List<string>>) GetActivities,
                name: "GetActivities",
                description: "Recommends activites todo based on the current weather"
            );

            // Maintain conversation history
            List<ChatMessage> chatHistory = new();

            Console.WriteLine("GPT-OSS Chat - Type 'exit' to quit");
            Console.WriteLine();

            // Define reasoning effort level
            string effortLevel = "high";  // could be "low", "medium", "high"

            // Build options including the custom property
            var chatOptions = new ChatOptions
            {
                Tools = new[] { toolWeather, toolActivites },
                AdditionalProperties = new AdditionalPropertiesDictionary()
            };
            chatOptions.AdditionalProperties.Add("reasoning_effort", effortLevel);

            // Prompt user for input in a loop
            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();

                if (userInput?.ToLower() == "exit")
                    break;

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                // Add user message to chat history
                chatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, userInput));

                // Stream the AI response and display in real time
                Console.Write("Assistant: ");
                var assistantResponse = "";

                ChatResponse response = await chatClient.GetResponseAsync(chatHistory, chatOptions);

                // Get a count of the messages in the response
                var messageCount = response.Messages.Count;

                if (messageCount == 1)
                {
                    // Get the first message
                    var chatMessage = response.Messages[0];

                    foreach (var content in chatMessage.Contents)
                    {
                        var type = content.GetType();
                        var typeName = type.Name; // Get the name of the type

                        if (typeName == "TextReasoningContent")
                        {
                            // cast to TextReasoningContent
                            var reasoningContent = content as TextReasoningContent;
                            var reasoningContentText = reasoningContent!.Text;

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"\nReasoning: {reasoningContentText}");
                            Console.ResetColor();
                        }
                    }
                }
                else
                {
                    foreach (var msg in response.Messages)
                    {
                        foreach (var content in msg.Contents)
                        {
                            var type = content.GetType();
                            var typeName = type.Name; // Get the name of the type

                            if (typeName == "TextReasoningContent")
                            {
                                // cast to TextReasoningContent
                                var reasoningContent = content as TextReasoningContent;
                                var reasoningContentText = reasoningContent!.Text;

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"\nReasoning: {reasoningContentText}");
                                Console.ResetColor();
                            }
                            else if (typeName == "FunctionCallContent")
                            {
                                // cast to FunctionCallContent
                                var functionContent = content as FunctionCallContent;
                                var functionContentName = functionContent!.Name;
                                var functionContentArguments = functionContent!.Arguments;
                                // Write out the functionContentArguments Dictionary as a string
                                var functionContentArgumentsString = System.Text.Json.JsonSerializer.Serialize(functionContentArguments);

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"\nFunction: Name - {functionContentName}");
                                Console.WriteLine($"\nFunction: Arguments - {functionContentArgumentsString}");
                                Console.ResetColor();
                            }
                        } 
                    }
                }


                // Write the Asssitant response
                Console.WriteLine(response.Text);
                assistantResponse = response.Text;
                // Append assistant message to chat history
                chatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, assistantResponse));
                
                Console.WriteLine();
            }
        }

        // Define a toolWeather: a method to get current weather
        static string GetCurrentWeather(string city)
        {
            // In real ‑ call weather API; here just fake
            return $"The weather in {city} is {(DateTime.Now.Second % 2 == 0 ? "sunny" : "rainy")}.";
        }

        // Define a toolActivities: a method to get activities based on weather
        static List<string> GetActivities(string weather)
        {
            if (weather == "sunny")
            {
                return new List<string> { "go for a walk", "have a picnic", "play outdoor sports" };
            }
            else
            {
                return new List<string> { "read a book", "watch a movie", "visit a museum" };
            }
        }
    }
}
