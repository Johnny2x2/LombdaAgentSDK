namespace LombdaAgentSDK.Agents
{
    //Used to define the output format of a model for structured outputs.
    public class ModelOutputFormat
    {
        public string JsonSchemaFormatName { get; set; }
        public BinaryData JsonSchema { get; set; }
        public bool JsonSchemaIsStrict { get; set; } = true;
        public string FormatDescription { get; set; }

        public ModelOutputFormat() { }

        public ModelOutputFormat(string jsonSchemaFormatName, BinaryData jsonSchema, bool jsonSchemaIsStrict, string formatDescription = "")
        {
            JsonSchemaFormatName = jsonSchemaFormatName;
            JsonSchema = jsonSchema;
            JsonSchemaIsStrict = jsonSchemaIsStrict;
            FormatDescription = formatDescription;
        }   
    }
}
