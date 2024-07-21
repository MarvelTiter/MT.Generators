using Generators.Shared.Builder;
using Generators.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Generators.Shared.Builder
{
    internal static class MethodBuilderExtensions
    {
        public static T MethodName<T>(this T builder, string name) where T : MethodBase
        {
            builder.Name = name;
            return builder;
        }
        public static T Generic<T>(this T builder,params TypeParameterInfo[] types) where T : MemberBuilder
        {
            if (types.Length > 0)
            {
                builder.IsGeneric = true;
                foreach (var item in types)
                {
                    builder.TypeArguments.Add(item);
                }
            }
            return builder;
        }
        public static MethodBuilder ReturnType(this MethodBuilder builder, string returnType)
        {
            builder.ReturnType = returnType;
            return builder;
        }
        public static MethodBuilder Async(this MethodBuilder builder)
        {
            builder.IsAsync = true;
            return builder;
        }

        public static MethodBuilder Lambda(this MethodBuilder builder, string body)
        {
            builder.IsLambdaBody = true;
            builder.Body.Add(body);
            return builder;
        }

        public static T AddBody<T>(this T builder, params string[] body) where T : MethodBase
        {
            foreach (var item in body)
            {
                builder.Body.Add(item);
            }
            return builder;
        }

        public static T AddBody<T>(this T builder, params Statement[] body) where T : MethodBase
        {
            foreach (var item in body)
            {
                builder.Body.Add(item);
            }
            return builder;
        }

        public static T AddParameter<T>(this T builder, params string[] parameters)
            where T : MethodBase
        {
            foreach (var item in parameters)
            {
                builder.Parameters.Add(item);
            }
            return builder;
        }
    }
}
