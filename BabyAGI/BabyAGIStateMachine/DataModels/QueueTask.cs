using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine.DataModels
{
    public class QueueTask
    {
        public bool IsTaskQueued { get; set; } = false;
        public string Task { get; set; } = "";
        public QueueTask() { }
        public QueueTask(string task)
        {
            Task = task;
            IsTaskQueued = true;
        }
    }
}
