using System;

namespace AutoWasmApiGenerator
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WebMethodAttribute : Attribute
    {
        public WebMethod Method { get; set; } = WebMethod.Post;
        public string? Route { get; set; }
        public bool AllowAnonymous { get; set; }
        public bool Authorize { get; set; }
    }
}
