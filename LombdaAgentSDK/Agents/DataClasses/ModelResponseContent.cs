namespace LombdaAgentSDK.Agents.DataClasses
{
    /// <summary>
    /// Current supported model message content type
    /// </summary>
    public enum ModelContentType
    {
        Unknown,
        InputText,
        InputImage,
        InputFile,
        OutputText,
        Refusal,
    }

    /// <summary>
    /// Base class for the message content items
    /// </summary>
    public class ModelMessageContent
    {
        public ModelContentType ContentType { get; set; }
    }

    /// <summary>
    /// Base text message content
    /// </summary>
    public class ModelMessageTextContent : ModelMessageContent
    {
        public string? Text { get; set; }
    }

    /// <summary>
    /// Generic text message content
    /// </summary>
    public class ModelMessageResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    /// <summary>
    /// User text message content
    /// </summary>
    public class ModelMessageUserResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageUserResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    /// <summary>
    /// System text message content
    /// </summary>
    public class ModelMessageSystemResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageSystemResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    /// <summary>
    /// Assistant text message content
    /// </summary>
    public class ModelMessageAssistantResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageAssistantResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }

    /// <summary>
    /// Dev text message content
    /// </summary>
    public class ModelMessageDeveloperResponseTextContent : ModelMessageTextContent
    {
        public ModelMessageDeveloperResponseTextContent(string response)
        {
            ContentType = ModelContentType.OutputText;
            Text = response;
        }
    }
    /// <summary>
    /// User Request Text message content
    /// </summary>
    public class ModelMessageRequestTextContent : ModelMessageTextContent
    {
        public ModelMessageRequestTextContent(string input)
        {
            ContentType = ModelContentType.InputText;
            Text = input;
        }
    }

    /// <summary>
    /// Refusal message content
    /// </summary>
    public class ModelMessageRefusalContent : ModelMessageTextContent
    {
        public ModelMessageRefusalContent(string text)
        {
            ContentType = ModelContentType.InputText;
            Text = text;
        }
    }

    /// <summary>
    /// Image File message content with binary data or image url
    /// </summary>
    public class ModelMessageImageFileContent : ModelMessageFileContent
    {
        /// <summary>
        /// type of media e.g: png, jpg
        /// </summary>
        public string MediaType { get; set; }
        /// <summary>
        /// Complete dataUri
        /// </summary>
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

        /// <summary>
        /// Get file by file ID into message stream
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public ModelMessageImageFileContent CreateModelImageContentByFileID(string fileId)
        {
            ModelMessageImageFileContent content = new();
            FileId = fileId;
            return content;
        }
    }

    /// <summary>
    /// Image message content by url
    /// </summary>
    public class ModelMessageImageUrlContent : ModelMessageFileContent
    {
        public Uri ImageUrl { get; set; }
        public ModelMessageImageUrlContent(Uri imageData)
        {
            ContentType = ModelContentType.InputImage;
            ImageUrl = imageData;
        }
    }

    /// <summary>
    /// Generic file message content
    /// </summary>
    public class ModelMessageFileContent : ModelMessageContent
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        public string? FileName { get; set; }
        /// <summary>
        /// File Binary Data
        /// </summary>
        public BinaryData DataBytes { get; set; }
        /// <summary>
        /// File ID to reference
        /// </summary>
        public string FileId { get; set; }

        public ModelMessageFileContent(string fileName = "", BinaryData data = null, string mediaType = "")
        {
            ContentType = ModelContentType.InputFile;
            DataBytes = data;
            FileName = fileName;
        }
        /// <summary>
        /// Create a file message content by using the fileId
        /// </summary>
        /// <param name="fileID"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public ModelMessageFileContent CreateFileContentByID(string fileId, string fileName = null)
        {
            ModelMessageFileContent content = new();
            content.ContentType = ModelContentType.InputFile;
            FileId = fileId;
            return content;
        }
    }
}
