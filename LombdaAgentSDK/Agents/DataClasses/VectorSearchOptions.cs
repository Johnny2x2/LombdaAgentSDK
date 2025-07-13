using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentSDK.Agents.DataClasses
{
    public class VectorSearchOptions
    {
        //public FileSearchToolRankingOptions RankingOptions { get; set; } = new FileSearchToolRankingOptions();
        public List<string> VectorIDs { get; set; } = new List<string>();

        public int MaxResults { get; set; } = 10;

        public BinaryData Filters { get; set; }

        public VectorSearchOptions(List<string>? vectorDbID = null, int maxResults = 10, BinaryData filters = null)
        {
            VectorIDs = vectorDbID ?? VectorIDs;
            //RankingOptions = options ?? new FileSearchToolRankingOptions();
            MaxResults = maxResults;
            Filters = filters;
        }
    }
}
