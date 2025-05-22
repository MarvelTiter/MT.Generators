#if NET6_0_OR_GREATER
using System.Text.Json;

namespace AutoWasmApiGenerator;

/// <summary>
/// 
/// </summary>
public class AutoWasmApiGeneratorJsonHelper
{
    /// <summary>
    /// 
    /// </summary>
    public static readonly JsonSerializerOptions Option = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static readonly JsonSerializerOptions TupleOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        PropertyNamingPolicy = null
    };
}
#endif