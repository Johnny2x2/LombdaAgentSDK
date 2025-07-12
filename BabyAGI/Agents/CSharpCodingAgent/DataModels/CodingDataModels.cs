
using Examples.Demos.FunctionGenerator;

namespace Examples.Demos.CodingAgent
{
    public struct ProgramResult
    {
        public CodeItem[] items { get; set; }
        public ProgramResult(CodeItem[] items)
        {
            this.items = items;
        }
    }

    public struct CodeItem
    {
        public string filePath { get; set; }
        public string code { get; set; }

        public CodeItem(string path, string code)
        {
            filePath = path;
            this.code = code;
        }
    }

    public struct ProgramResultOutput
    {
        public ProgramResult Result { get; set; }
        public string ProgramRequest { get; set; }
        public ProgramResultOutput(ProgramResult result, string request)
        {
            Result = result;
            ProgramRequest = request;
        }
    }

    public struct CodeBuildInfoOutput
    {
        public CodeBuildInfo BuildInfo { get; set; }
        public ProgramResultOutput ProgramResult { get; set; }

        public CodeBuildInfoOutput() { }
        public CodeBuildInfoOutput(CodeBuildInfo info, ProgramResultOutput codeResult)
        {
            BuildInfo = info;
            ProgramResult = codeResult;
        }
    }

    public struct CodeReview
    {
        public string ReviewSummary { get; set; }
        public CodeReviewItem[] Items { get; set; }

        public CodeReview(CodeReviewItem[] item)
        {
            Items = item;
        }
        public override string ToString()
        {
            return $""""
                    From Code Review Summary:
                    {ReviewSummary}

                    Items to fix:

                    {string.Join("\n\n", Items)}

                    """";
        }
    }

    public struct CodeReviewItem
    {
        public string CodePath { get; set; }
        public string CodeError { get; set; }
        public string SuggestedFix { get; set; }
        public CodeReviewItem(string codePath, string codeError, string suggestedFix)
        {
            CodePath = codePath;
            CodeError = codeError;
            SuggestedFix = suggestedFix;
        }

        public override string ToString()
        {
            return $"""

                     File: {CodePath}
                     
                     Had Error: 
                     {CodeError}

                     Suggested Fix:
                     {SuggestedFix}

                     """;
        }
    }
}
