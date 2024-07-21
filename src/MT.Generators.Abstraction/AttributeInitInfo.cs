using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MT.Generators.Abstraction
{
    public class AttributeInitInfo
    {
        public string AttributeType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = [];
        public AttributeInitInfo(string type)
        {
            AttributeType = type;
        }
        string ArgumentString => string.Join(", ", Parameters.Select(kv => $"{kv.Key} = {(kv.Value.GetType() == typeof(string) ? $"\"{kv.Value}\"" : $"{kv.Value}")}"));
        public override string ToString()
        {
            return $"""
                {AttributeType}{(Parameters.Count > 0 ? $"({ArgumentString})" : "")}
                """;
        }
    }
}
