﻿using LombdaAgentSDK.Agents.Tools;

namespace LombdaAgentSDK.Agents.DataClasses
{
    /// <summary>
    /// Options to modify the response setup
    /// </summary>
    public class ModelResponseOptions
    {
        /// <summary>
        /// Previous Response ID for response API only
        /// </summary>
        public string? PreviousResponseId {  get; set; }

        /// <summary>
        /// Model being used
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Instructions for the model to run
        /// </summary>
        public string Instructions { get; set; }

        /// <summary>
        /// Tools to use during the run
        /// </summary>
        public List<BaseTool> Tools { get; set; } = new List<BaseTool>();

        /// <summary>
        /// Structured Output of type being used
        /// </summary>
        public ModelOutputFormat OutputFormat { get; set; }

        /// <summary>
        /// Reasoning Options
        /// </summary>
        public ModelReasoningOptions ReasoningOptions { get; set; }

        public List<MCPServer> MCPServers = new List<MCPServer>();

        public ModelResponseOptions() { }
    }
}
