﻿using LlmTornado.Responses.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentSDK.Agents.DataClasses
{
    public delegate void StreamingCallbacks(ModelStreamingEvents streamingResult);
    public enum ModelStreamingStatus
    {
        InProgress,
        Completed,
        Failed,
        Canceled,
        Queued,
        Incomplete
    }
    public enum ModelStreamingEventType
    {
        Created,
        InProgress,
        Failed,
        Incomplete,
        Completed,
        OutputItemAdded,
        OutputItemDone,
        ContentPartAdded,
        ContentPartDone,
        OutputTextDelta,
        OutputTextAnnotationAdded,
        TextDone,
        RefusalDelta,
        RefusalDone,
        FunctionCallDelta,
        FunctionCallDone,
        FileSearchInProgress,
        FileSearchSearching,
        FileSearchDone,
        CodeInterpreterCodeDelta,
        CodeInterpreterCodeDone,
        CodeInterpreterIntepreting,
        CodeInterpreterCompleted,
        ReasoningPartAdded,
        ReasoningPartDone,
        Error
    }

    /// <summary>
    /// Base class for model streaming events.
    /// </summary>
    public class ModelStreamingEvents : EventArgs
    {
        public string? ResponseId { get; set; } = string.Empty;
        public int SequenceId { get; set; } = 1;
        public ModelStreamingEventType EventType { get; set; } = ModelStreamingEventType.Created;
        public ModelStreamingStatus Status { get; set; } = ModelStreamingStatus.InProgress;

        public ModelStreamingEvents(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress)
        {
            SequenceId = seqNum;
            EventType = ModelStreamingEventType.Created;
            Status = ModelStreamingStatus.InProgress;
            ResponseId = responseId;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingCreatedEvent : ModelStreamingEvents
    {
        public ModelStreamingCreatedEvent(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress):base(seqNum, responseId, type, status)
        {

        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingInProgressEvent : ModelStreamingEvents
    {
        public ModelStreamingInProgressEvent(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress) : base(seqNum, responseId, type, status)
        {

        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingCompletedEvent : ModelStreamingEvents
    {
        public ModelStreamingCompletedEvent(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress) : base(seqNum, responseId, type, status)
        {

        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingFailedEvent : ModelStreamingEvents
    {
        public string? ErrorMessage { get; set; } = string.Empty;
        public string? ErrorCode { get; set; } = string.Empty;

        public ModelStreamingFailedEvent(int seqNum, string responseId = "", string errorMessage = "", string errorCode = "") : base(seqNum, responseId, ModelStreamingEventType.Failed, ModelStreamingStatus.Failed)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingIncompleteEvent : ModelStreamingEvents
    {
        public string? Reason { get; set; } = string.Empty;

        public ModelStreamingIncompleteEvent(int seqNum, string responseId = "", string reason=""): base(seqNum, responseId, ModelStreamingEventType.Incomplete, ModelStreamingStatus.Incomplete)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingErrorEvent : ModelStreamingEvents
    {
        public string? ErrorMessage { get; set; } = string.Empty;
        public string? ErrorCode { get; set; } = string.Empty;

        public ModelStreamingErrorEvent(int seqNum, string responseID = "", string errorMessage = "",string errorCode = ""): base(seqNum, responseID, ModelStreamingEventType.Error, ModelStreamingStatus.Failed)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingOutputTextDeltaEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public int ContentPartIndex { get; set; } = 0;
        public string? ItemId { get; set; } = null;
        public string? DeltaText { get; set; } = null;
        public ModelStreamingOutputTextDeltaEvent(int seqNum, int outputIndex, int contentIndex,string content, string itemId = "", string responseID = "") : base(seqNum, responseID, ModelStreamingEventType.Error, ModelStreamingStatus.Failed)
        {
            OutputIndex = outputIndex;
            ContentPartIndex = contentIndex;
            ItemId = itemId;
            DeltaText = content;
        }
    }

    public class ModelStreamingOutputTextDoneEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public int ContentPartIndex { get; set; } = 0;
        public string? ItemId { get; set; } = null; 
        public string? DeltaText { get; set; } = null;
        public ModelStreamingOutputTextDoneEvent(int seqNum, int outputIndex, int contentIndex, string content, string itemId = "", string responseID = "") : base(seqNum, responseID, ModelStreamingEventType.Error, ModelStreamingStatus.Failed)
        {
            OutputIndex = outputIndex;
            ContentPartIndex = contentIndex;
            ItemId = itemId;
            DeltaText = content;
        }
    }

    public class StreamingModelItem
    {
        public string? Id { get; set; }
        public ModelStreamingStatus Status { get; set; } = ModelStreamingStatus.InProgress;
        public ModelContentType type { get; set; } = ModelContentType.OutputText;
        public string Role { get; set; } = "assistant";
        public ModelMessageContent[]? Content { get; set; }
    }
    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingOutputItemAddedEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public StreamingModelItem? OutputItem { get; set; } = null;

        public ModelStreamingOutputItemAddedEvent(int seqNum, int index, string responseID = "") : base(seqNum,responseID, ModelStreamingEventType.OutputItemAdded, ModelStreamingStatus.InProgress)
        {
            OutputIndex = index;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingOutputItemDoneEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public StreamingModelItem? OutputItem { get; set; } = null;

        public ModelStreamingOutputItemDoneEvent(int seqNum, int index, string responseID = "") : base(seqNum, responseID,ModelStreamingEventType.OutputItemDone, ModelStreamingStatus.Completed)
        {
            OutputIndex = index;

        }
    }


    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingContentPartAddEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public int ContentPartIndex { get; set; } = 0;
        public ModelContentType ContentPartType { get; set; } = ModelContentType.OutputText;
        public string? ContentPartText { get; set; } = null;
        //Need annotation for the content part

        public ModelStreamingContentPartAddEvent(int seqNum, int outputIndex, int contentPartIndex, ModelContentType contentPartType, string? contentPartText = null, string responseID = "")
            :base(seqNum, responseID, ModelStreamingEventType.ContentPartAdded, ModelStreamingStatus.InProgress)
        { 
            OutputIndex = outputIndex;
            ContentPartIndex = contentPartIndex;
            ContentPartType = contentPartType;
            ContentPartText = contentPartText;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingContentPartDoneEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public int ContentPartIndex { get; set; } = 0;
        public ModelContentType ContentPartType { get; set; } = ModelContentType.OutputText;
        public string? ContentPartText { get; set; } = null;
        //Need annotation for the content part

        public ModelStreamingContentPartDoneEvent(int seqNum, int outputIndex, int contentPartIndex, ModelContentType contentPartType, string? contentPartText = null, string responseID = "")
            : base(seqNum, responseID, ModelStreamingEventType.ContentPartDone, ModelStreamingStatus.Completed)
        {
            OutputIndex = outputIndex;
            ContentPartIndex = contentPartIndex;
            ContentPartType = contentPartType;
            ContentPartText = contentPartText;
        }
    }

    ///Annotation add event
    ///
    public class ModelStreamingQueuedEvent : ModelStreamingEvents
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ModelStreamingQueuedEvent(int seqNum, DateTime createdAt, DateTime updatedAt, string responseID = "")
            : base(seqNum, responseID, ModelStreamingEventType.Created, ModelStreamingStatus.Queued)
        {
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingReasoningPartDoneEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public int SummaryPartIndex { get; set; } = 0;
        public string? DeltaText { get; set; } = null;
        public string ItemId { get; set; }
        //Need annotation for the content part
        public ModelStreamingReasoningPartDoneEvent(int seqNum, int outputIndex, int summaryPartIndex, string itemId, string reasoningText = "", string responseID = "")
            : base(seqNum, responseID, ModelStreamingEventType.ReasoningPartDone, ModelStreamingStatus.InProgress)
        {
            OutputIndex = outputIndex;
            SummaryPartIndex = summaryPartIndex;
            DeltaText = reasoningText;
        }
    }

    /// <summary>
    /// Provides data for streaming callback events.
    /// </summary>
    public class ModelStreamingReasoningPartAddedEvent : ModelStreamingEvents
    {
        public int OutputIndex { get; set; } = 0;
        public int SummaryPartIndex { get; set; } = 0;
        public string? DeltaText { get; set; } = null;
        public string ItemId { get; set; }
        //Need annotation for the content part
        public ModelStreamingReasoningPartAddedEvent(int seqNum, int outputIndex, int summaryPartIndex, string itemId, string reasoningText = "", string responseID = "")
            : base(seqNum, responseID, ModelStreamingEventType.ReasoningPartAdded, ModelStreamingStatus.Completed)
        {
            OutputIndex = outputIndex;
            SummaryPartIndex = summaryPartIndex;
            DeltaText = reasoningText;
            ItemId = itemId;
        }
    }
}
