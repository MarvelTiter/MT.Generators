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
    public static readonly JsonSerializerOptions Option = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    /// <summary>
    /// 
    /// </summary>
    public static readonly JsonSerializerOptions TupleOption = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        PropertyNamingPolicy = null
    };
}
#endif