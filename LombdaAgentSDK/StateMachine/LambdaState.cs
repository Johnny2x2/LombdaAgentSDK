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
    /// <summary>
    /// Represents a state that executes a lambda function with specified input and output types.
    /// </summary>
    /// <remarks>This class is designed to encapsulate a lambda function, allowing it to be invoked with a
    /// specific input and produce a corresponding output. It supports both synchronous and asynchronous lambda
    /// functions.</remarks>
    /// <typeparam name="TInput">The type of the input parameter for the lambda function.</typeparam>
    /// <typeparam name="TOutput">The type of the output result from the lambda function.</typeparam>
    public class LambdaState<TInput, TOutput> : BaseState<TInput, TOutput>
    {
        /// <summary>
        /// Gets or sets the function that processes input of type <typeparamref name="TInput"/> and produces
        /// output of type <typeparamref name="TOutput"/>.
        /// </summary>
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
