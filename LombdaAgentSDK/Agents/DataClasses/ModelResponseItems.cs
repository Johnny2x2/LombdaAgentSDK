namespace LombdaAgentSDK.Agents.DataClasses
{
    public enum ModelStatus
    {
        InProgress,
        Completed,
        Incomplete
    }

    public class ModelItem
    {
        public string Id { get; set; }
        public ModelItem(string id) { 
            Id = id;
        }
    }

    public class CallItem : ModelItem
    {
        public string CallId { get; set; }
        public CallItem(string id, string callId) :base(id)
        {
            Id = id;
            CallId = callId;
        }
    }

    public enum ModelWebSearchingStatus
    {
        InProgress,
        Searching,
        Completed,
        Failed
    }

    public class ModelWebCallItem : ModelItem
    {
        private string query = "";
        public ModelWebSearchingStatus Status { get; set; }
        public string Query { get => query; set => query = value; }

        public ModelWebCallItem(string id, ModelWebSearchingStatus status) : base(id)
        {
            Id = id;
            Status = status;
        }
    }

    public class ModelReasoningItem : ModelItem
    {
        public string? EncryptedContent { get; set; } = "";
        public List<string> Summary { get; set; } = new List<string>();
        public ModelReasoningItem(string id, string[]? summary = null) : base(id)
        {
            Id = id;
            Summary = summary == null ? Summary : [.. summary];
        }
    }

    public class FileSearchCallContent
    {
        public FileSearchCallContent()
        {
        }

        internal FileSearchCallContent(string fileId, string text, string filename, float? score)
        {
            FileId = fileId;
            Text = text;
            Filename = filename;
            Score = score;
        }

        public string FileId { get; set; }

        public string Text { get; set; }

        public string Filename { get; set; }

        public float? Score { get; set; }
    }

    public class ModelFileSearchCallItem : ModelItem
    {
        public List<string> Queries { get; set; } = new List<string>();

        public ModelStatus Status { get; set; }

        public List<FileSearchCallContent> Results { get; set; } = new List<FileSearchCallContent>();

        public ModelFileSearchCallItem(string id, List<string> queries, ModelStatus status, List<FileSearchCallContent> results) : base(id)
        {
            Id = id;
            Queries = queries ?? throw new ArgumentNullException(nameof(queries), "Queries cannot be null");
            Status = status;
            Results = results;
        }
    }

    public class ModelFunctionCallItem : CallItem
    {
        public string CallId { get; set; }
        public string FunctionName { get; set; }
        public BinaryData? FunctionArguments { get; set; }
        public ModelStatus Status { get; set; }
        public ModelFunctionCallItem(string id, string callId, string functionName, ModelStatus status, BinaryData? functionArguments = null) : base(id, callId)
        {
            CallId = callId;
            Id = id;
            Status = status;
            FunctionName = functionName;
            FunctionArguments = functionArguments;
        }
    }

    public class ModelFunctionCallOutputItem : CallItem
    {
        public string CallId { get; set; }
        public string FunctionOutput { get; set; }
        public string FunctionName { get; set; }
        public ModelStatus Status { get; set; }
        public ModelFunctionCallOutputItem(string id, string callId, string functionOutput, ModelStatus status, string functionName) : base(id, callId)
        {
            CallId = callId;
            Id = id;
            Status = status;
            FunctionOutput = functionOutput;
            FunctionName = functionName;
        }
    }



    public class ModelMessageItem : ModelItem
    {
        public List<ModelMessageContent> Content { get; set; } = new List<ModelMessageContent>();
        public string Role { get; set; }
        public ModelStatus Status { get; set; }
        public ModelMessageItem(string id, string role, List<ModelMessageContent> content, ModelStatus status) :base(id)
        {
            Id = id;
            Status = status;
            Role = role;
            Content = content;
        }

        public string? Text => ((ModelMessageTextContent?)Content.LastOrDefault(mess => mess is ModelMessageTextContent))?.Text;
    }
}
