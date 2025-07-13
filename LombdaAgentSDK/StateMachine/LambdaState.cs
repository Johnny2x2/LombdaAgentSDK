using LlmTornado.Moderation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LombdaAgentSDK.StateMachine
{
    public delegate U LambdaMethod<T,U>(T input);

    public class LambdaState<TInput, TOutput> : BaseState<TInput, TOutput>
    {
        public LambdaMethod<TInput, TOutput> Function { get; set; }

        public LambdaState(LambdaMethod<TInput, TOutput> method)
        {
            Function = method;
        }

        public override async Task<TOutput> Invoke(TInput args)
        {
            
            object? result;
            if (AsyncHelpers.IsGenericTask(Function.Method.ReturnType, out Type taskResultType))
            {
                // Method is async, invoke and await
                var task = (Task)Function.DynamicInvoke(args)!;
                await task.ConfigureAwait(false);
                // Get the Result property from the Task
                result = taskResultType.GetProperty("Result")?.GetValue(task);
            }
            result = Function.DynamicInvoke(args);
            return (TOutput)result!;
        }
    }
}
