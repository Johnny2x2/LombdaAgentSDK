using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentSDK.Agents.DataClasses
{
    /// <summary>
    /// Vector search options to enable openAI vector searching
    /// </summary>
    public class VectorSearchOptions
    {
        //Removed until i have time to add in
        //public FileSearchToolRankingOptions RankingOptions { get; set; } = new FileSearchToolRankingOptions();

        /// <summary>
        /// Vector DBs you want to search in
        /// </summary>
        public List<string> VectorIDs { get; set; } = new List<string>();

        /// <summary>
        /// Max output results from search default: 10
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Filters to apply to vector search (have not used yet)
        /// </summary>
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
