namespace AutoWasmApiGenerator;

/// <summary>
///     Parameter binding type.
/// </summary>
public enum BindingType
{
    /// <summary>
    /// 忽略
    /// </summary>
    Ignore = -1,

    /// <summary>
    ///     从查询字符串中获取值。
    /// </summary>
    FromQuery,

    /// <summary>
    ///     从路由数据中获取值。
    /// </summary>
    FromRoute,

    /// <summary>
    ///     从发布的表单域中获取值。
    /// </summary>
    FromForm,

    /// <summary>
    ///     从请求正文中获取值。
    /// </summary>
    FromBody,

    /// <summary>
    ///     从 HTTP 标头中获取值。
    /// </summary>
    FromHeader,

    /// <summary>
    ///     从服务容器中获取值。
    /// </summary>
    FromServices
}