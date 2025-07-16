using BabyAGI;
using BabyAGI.Agents;
using BabyAGI.Agents.ResearchAgent;
using BabyAGI.BabyAGIStateMachine.States;
using Examples.Demos.FunctionGenerator;
using Examples.Demos.ProjectCodingAgent;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;


CodingProjectsAgent codingAgent = new("C:\\Users\\johnl\\source\\repos\\FunctionApplications");

await codingAgent.RunProjectCodingAgent("Create a C# console application to get the results from a given website link");
//BabyAGIRunner babyAGI = new();
//await babyAGI.RunAGI();



