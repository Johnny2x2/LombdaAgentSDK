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


CodingProjectsAgent codingAgent = new("C:\\Users\\jlomba\\source\\GeneratedProjects");

await codingAgent.RunProjectCodingAgent("Can you fix my state machine?","State Machine Flow Application");
//BabyAGIRunner babyAGI = new();
//await babyAGI.RunAGI();



