using BabyAGI;
using BabyAGI.Agents;
using BabyAGI.Agents.ResearchAgent;
using BabyAGI.BabyAGIStateMachine.States;
using Examples.Demos.FunctionGenerator;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.Agents.Tools;
using LombdaAgentSDK.StateMachine;

BATaskCreationState taskCreationState = new();
taskCreationState.CurrentStateMachine = new StateMachine<string, TaskBreakdownResult>();
var tasks = await taskCreationState.Invoke("Create a simple web application that allows users to register and login.");
Console.WriteLine("Task Breakdown Result:");
//BabyAGIRunner babyAGI = new();
//await babyAGI.RunAGI();



