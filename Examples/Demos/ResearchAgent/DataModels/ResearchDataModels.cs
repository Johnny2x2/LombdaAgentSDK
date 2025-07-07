using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos.ResearchAgent.DataModels
{
    public struct WebSearchPlan
    {
        public WebSearchItem[] items { get; set; }
        public WebSearchPlan(WebSearchItem[] items)
        {
            this.items = items;
        }
    }

    public struct WebSearchItem
    {
        public string reason { get; set; }
        public string query { get; set; }

        public WebSearchItem(string reason, string query)
        {
            this.reason = reason;
            this.query = query;
        }
    }

    public struct ReportData
    {
        public string ShortSummary { get; set; }
        public string FinalReport { get; set; }
        public string[] FollowUpQuestions { get; set; }
        public ReportData(string shortSummary, string finalReport, string[] followUpQuestions)
        {
            this.ShortSummary = shortSummary;
            this.FinalReport = finalReport;
            this.FollowUpQuestions = followUpQuestions;
        }
    }
}
