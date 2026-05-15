using AutoWasmApiGenerator;
using Microsoft.JSInterop;

namespace Blazor.Test.Client.Aop
{
    public class HttpExceptionHandler(IJSRuntime js) : GeneratedApiClientDelegatingHandler
    {
        public override async Task OnExceptionAsync(ExceptionContext context)
        {
            try
            {
                await js.InvokeVoidAsync("alert", context.Exception.Message);
            }
            catch (JSException)
            {

            }
            finally
            {
                context.Handled = true;
            }

        }
    }
}
