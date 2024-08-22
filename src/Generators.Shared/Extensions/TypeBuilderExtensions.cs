using Microsoft.CodeAnalysis;
using System;

namespace Generators.Shared.Builder
{
    internal static class TypeBuilderExtensions
    {
        public static void AddSource(this SourceProductionContext context, CodeFile? codeFile)
        {
            if (codeFile is null) return;
            context.AddSource(codeFile.FileName, codeFile.ToString());
        }
        public static T AddMembers<T>(this T builder, params Node[] members) where T : TypeBuilder
        {
            foreach (var item in members)
            {
                builder.Members.Add(item);
            }
            return builder;
        }
        public static T Modifiers<T>(this T builder, string modifiers) where T : MemberBuilder
        {
            builder.Modifiers = modifiers;
            return builder;
        }

        public static T Attribute<T>(this T builder, params string[] attributes) where T : MemberBuilder
        {
            foreach (var attr in attributes)
            {
                builder.Attributes.Add(attr);
            }
            return builder;
        }

        public static T AttributeIf<T>(this T builder, bool condition, params string[] attributes)
            where T : MemberBuilder
        {
            if (condition)
            {
                builder.Attribute(attributes);
            }
            return builder;
        }

        public static T AddGeneratedCodeAttribute<T>(this T builder, Type generatorType) where T : MemberBuilder
        {
            return builder.Attribute($"""global::System.CodeDom.Compiler.GeneratedCode("{generatorType.FullName}", "{generatorType.Assembly.GetName().Version}")""");
            //return builder;
        }

        public static FieldBuilder MemberType(this FieldBuilder builder, string type)
        {
            builder.MemberType = type;
            return builder;
        }

        public static PropertyBuilder MemberType(this PropertyBuilder builder, string type)
        {
            builder.MemberType = type;
            return builder;
        }

        public static FieldBuilder FieldName(this FieldBuilder builder, string name)
        {
            builder.Name = name;
            return builder;
        }

        public static PropertyBuilder PropertyName(this PropertyBuilder builder, string name)
        {
            builder.Name = name;
            return builder;
        }
    }
}
