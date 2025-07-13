using System.Drawing;

namespace LombdaAgentSDK.Agents.DataClasses
{
    public enum ModelComputerCallAction
    {
        Click,

        DoubleClick,

        Drag,

        KeyPress,

        Move,

        Screenshot,

        Scroll,

        Type,

        Wait,

        Unknown
    }

    public enum MouseButtons { Right, Left, Middle, Back, Forward }

    public class ComputerToolAction
    {
        public ModelComputerCallAction Kind { get; set; } = ModelComputerCallAction.Unknown;
        public Point MoveCoordinates { get; set; }
        public MouseButtons MouseButtonClick { get; set; }
        public bool WasDoubleClick { get; set; } = false;
        public Point StartDragLocation { get; set; }
        public List<string> KeysToPress { get; set; }
        public string TypeText { get; set; }
        public int ScrollHorOffset { get; set; }
        public int ScrollVertOffset { get; set; }
    }

    public class ComputerToolActionDoubleClick : ComputerToolAction
    {
        public ComputerToolActionDoubleClick(int toX, int toY)
        {
            Kind = ModelComputerCallAction.DoubleClick;
            WasDoubleClick = true;
            MouseButtonClick = MouseButtons.Left;
            MoveCoordinates = new Point(toX, toY);
        }
    }

    public class ComputerToolActionClick : ComputerToolAction
    {
        public ComputerToolActionClick(int toX, int toY, MouseButtons button)
        {
            Kind = ModelComputerCallAction.Click;
            WasDoubleClick = false;
            MouseButtonClick = button;
            MoveCoordinates = new Point(toX, toY);
        }
    }

    public class ComputerToolActionDrag: ComputerToolAction
    {
        public ComputerToolActionDrag(int fromX, int fromY, int toX, int toY)
        {
            Kind = ModelComputerCallAction.Drag;
            StartDragLocation = new Point(fromX, fromY);
            MoveCoordinates = new Point(toX, toY);
        }
    }

    public class ComputerToolActionMove : ComputerToolAction
    {
        public ComputerToolActionMove(int toX, int toY)
        {
            Kind = ModelComputerCallAction.Move;
            MoveCoordinates = new Point(toX, toY);
        }
    }

    public class ComputerToolActionKeyPress : ComputerToolAction
    {
        public ComputerToolActionKeyPress(List<string>? keys)
        {
            Kind = ModelComputerCallAction.KeyPress;
            KeysToPress = keys ?? new List<string>();
        }
    }

    public class ComputerToolActionType : ComputerToolAction
    {
        public ComputerToolActionType(string text)
        {
            Kind = ModelComputerCallAction.Type;
            TypeText = text;
        }
    }

    public class ComputerToolActionWait : ComputerToolAction
    {
        public ComputerToolActionWait()
        {
            Kind = ModelComputerCallAction.Wait;
        }
    }

    public class ComputerToolActionScreenShot : ComputerToolAction
    {
        public ComputerToolActionScreenShot()
        {
            Kind = ModelComputerCallAction.Screenshot;
        }
    }

    public class ComputerToolActionScroll: ComputerToolAction
    {
        public ComputerToolActionScroll(int offsetVertical = 0, int offsetHorizontal = 0)
        {
            Kind = ModelComputerCallAction.Scroll;
            ScrollHorOffset = offsetHorizontal;
            ScrollVertOffset = offsetVertical;
        }
    }

    public class ModelComputerCallItem : CallItem
    {
        public ModelStatus Status { get; set; }
        
        public ComputerToolAction Action { get; set; }

        public ModelComputerCallItem(string id, string callId, ModelStatus status, ComputerToolAction action) : base(id, callId)
        {
            Id = id;
            Status = status;
            Action = action;
            CallId = callId;
        }
    }

    public class ModelComputerCallOutputItem : CallItem
    {
        public ModelStatus Status { get; set; }
        public ModelMessageImageFileContent ScreenShot { get; set; }
        public ModelComputerCallOutputItem(string id, string callId,  ModelStatus status, ModelMessageImageFileContent screenShot) : base(id, callId)
        {
            Id = id;
            Status = status;
            CallId = callId;
            ScreenShot = screenShot;
        }
    }

}
