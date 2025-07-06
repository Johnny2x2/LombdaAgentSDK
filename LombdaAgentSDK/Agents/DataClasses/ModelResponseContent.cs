namespace LombdaAgentSDK.Agents.DataClasses
{
    public enum ModelContentType
    {
        Unknown,
        InputText,
        InputImage,
        InputFile,
        OutputText,
        Refusal,
    }

    public class ModelMessageContent
    {
        public ModelContentType ContentType { get; set; }
    }

    public class ModelMessageTextContent : ModelMessageContent
    {
        public string? Text { get; set; }
    }

    public class ModelMessageResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    public class ModelMessageUserResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageUserResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }
    public class ModelMessageSystemResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageSystemResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    public class ModelMessageAssistantResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageAssistantResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    public class ModelMessageDeveloperResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageDeveloperResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    public class ModelMessageRequestTextContent : ModelMessageTextContent
    {
        public ModelMessageRequestTextContent(string text)
        {
            ContentType = ModelContentType.InputText;
            Text = text;
        }
    }

    public class ModelMessageRefusalContent : ModelMessageTextContent
    {
        public ModelMessageRefusalContent(string text)
        {
            ContentType = ModelContentType.InputText;
            Text = text;
        }
    }

    public class ModelMessageImageFileContent : ModelMessageFileContent
    {
        public string MediaType { get; set; }
        public string ImageURL { get; set; }

        public ModelMessageImageFileContent() { ContentType = ModelContentType.InputImage; }

        public ModelMessageImageFileContent(BinaryData imageData, string mediaType)
        {
            DataBytes = imageData;
            ContentType = ModelContentType.InputImage;
            MediaType = mediaType;
            string base64EncodedData = Convert.ToBase64String(imageData.ToArray());
            string dataUri = $"data:{MediaType};base64,{base64EncodedData}";
            ImageURL = dataUri;
        }

        public ModelMessageImageFileContent(string imageUrl)
        {
            ContentType = ModelContentType.InputImage;

            ImageURL = imageUrl;
        }

        public ModelMessageImageFileContent CreateModelImageContentByFileID(string fileId)
        {
            ModelMessageImageFileContent content = new();
            FileId = fileId;
            return content;
        }
    }

    public class ModelMessageImageUrlContent : ModelMessageFileContent
    {
        public Uri ImageUrl { get; set; }
        public ModelMessageImageUrlContent(Uri imageData)
        {
            ContentType = ModelContentType.InputImage;
            ImageUrl = imageData;
        }
    }

    public class ModelMessageFileContent : ModelMessageContent
    {
        public string? FileName { get; set; }

        public BinaryData DataBytes { get; set; }
        public string FileId { get; set; }

        public ModelMessageFileContent(string fileName = "", BinaryData data = null, string mediaType = "")
        {
            ContentType = ModelContentType.InputFile;
            DataBytes = data;
            FileName = fileName;
        }

        public ModelMessageFileContent CreateFileContentByID(string fileID, string fileName = null)
        {
            ModelMessageFileContent content = new();
            content.ContentType = ModelContentType.InputFile;
            FileId = fileID;
            return content;
        }
    }
}
