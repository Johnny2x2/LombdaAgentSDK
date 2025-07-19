using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.DataModels
{
    [Description("Enriches the task execution result with additional context, insights, and recommendations for future tasks.")]
    public struct EnrichmentResult
    {
        public string SummaryToEmbed { get; set; }
        public string SummaryToRetrieve { get; set; }
        public MetadataKeyValuePair[] UsefulMetadata { get; set; }
        public bool StoreInLongTermMemory { get; set; }
        public string AdditionalContextNeeded { get; set; }
    }

    public struct MetadataKeyValuePair
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public struct MemoryItem
    {
        public string ToEmbed { get; set; }
        public string SaveSummary { get; set; }
        public MetadataKeyValuePair[] UsefulMetadata { get; set; }
    }
}
