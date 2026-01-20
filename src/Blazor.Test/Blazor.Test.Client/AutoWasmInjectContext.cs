using AutoInjectGenerator;

namespace Blazor.Test.Client
{

    [AutoInjectContext]
    public static partial class AutoWasmInjectContext
    {
        [AutoInjectConfigurationAttribute(Include = "WASM")]
        public static partial void AutoWasm(this IServiceCollection services);
    }
}
