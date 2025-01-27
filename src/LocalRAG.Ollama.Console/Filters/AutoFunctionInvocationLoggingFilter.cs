using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace Filters;

public class AutoFunctionInvocationLoggingFilter : IAutoFunctionInvocationFilter
{
    
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {

        //logger.LogWarning("ChatHistory: {ChatHistory}", JsonSerializer.Serialize(context.ChatHistory));
        //logger.LogWarning("Function count: {FunctionCount}", context.FunctionCount);

        var functionCalls = FunctionCallContent.GetFunctionCalls(context.ChatHistory.Last()).ToList();


        //functionCalls.ForEach(functionCall
        //    => logger.LogWarning(
        //        "Function call requests: {PluginName}-{FunctionName}({Arguments})",
        //        functionCall.PluginName,
        //        functionCall.FunctionName,
        //        JsonSerializer.Serialize(functionCall.Arguments)));


        await next(context);
    }
}
