using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenMapperGenerator.ReflectMapper
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MapperOptions
    {
        private MapperOptions()
        {

        }
        private static readonly Lazy<MapperOptions> lazyInstance = new(new MapperOptions());
        /// <summary>
        /// 
        /// </summary>
        public static MapperOptions Instance => lazyInstance.Value;

        private readonly ConcurrentDictionary<(Type Source, Type Target), object> mapperProfiles = new();
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="profile"></param>
        public void AddProfile<TSource, TTarget>(MapperProfile<TSource, TTarget> profile)
        {
            mapperProfiles.TryAdd((typeof(TSource), typeof(TTarget)), profile);
        }

        public void AddProfile<TProfile>()
            where TProfile : new()
        {
            var profileType = typeof(TProfile);
            var genericArgs = profileType.BaseType?.GetGenericArguments();
            if (genericArgs?.Length == 2)
            {
                var key = (genericArgs[0], genericArgs[1]);
                mapperProfiles.TryAdd(key, new TProfile());
            }
            else
            {
                throw new ArgumentException("The provided profile does not inherit from MapperProfile<TSource, TTarget>.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="configAction"></param>
        public void ConfigProfile<TSource, TTarget>(Action<MapperProfile<TSource, TTarget>> configAction)
        {
            var key = (typeof(TSource), typeof(TTarget));
            if (mapperProfiles.TryGetValue(key, out var existingProfileObj) && existingProfileObj is MapperProfile<TSource, TTarget> existingProfile)
            {
                configAction(existingProfile);
            }
            else
            {
                var newProfile = new MapperProfile<TSource, TTarget>();
                configAction(newProfile);
                mapperProfiles.TryAdd(key, newProfile);
            }
        }

        internal bool TryGetProfile<TSource, TTarget>([NotNullWhen(true)] out MapperProfile<TSource, TTarget>? profile)
        {
            var key = (typeof(TSource), typeof(TTarget));
            if (mapperProfiles.TryGetValue(key, out var existingProfileObj) && existingProfileObj is MapperProfile<TSource, TTarget> existingProfile)
            {
                profile = existingProfile;
                return true;
            }
            profile = null;
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    public class MapperProfile<TSource, TTarget>
    {
        private readonly List<MappingConfiguration> configurations = [];
        private readonly List<(Type, string)> constructorParameters = [];
        private readonly List<string> ignoreMembers = [];
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSourceMember"></typeparam>
        /// <typeparam name="TTargetMember"></typeparam>
        /// <param name="sourceMemberExpression"></param>
        /// <param name="destinationMemberExpression"></param>
        public void ForMember<TSourceMember, TTargetMember>(
             Expression<Func<TTarget, TTargetMember>> destinationMemberExpression
            , Expression<Func<TSource, TSourceMember>> sourceMemberExpression
            )
        {
            configurations.Add(new MappingConfiguration
            {
                SourceExpression = sourceMemberExpression,
                DestinationExpression = destinationMemberExpression
            });
        }

        /// <summary>
        /// 配置构造函数参数映射
        /// </summary>
        /// <param name="parameters"></param>
        public void ForConstructor(params Expression<Func<TSource, object?>>[] parameters)
        {
            constructorParameters.Clear();
            foreach (var item in parameters)
            {
                var member = ExpressionHelper.GetMemberTypeAndNameFromLambda(item);
                constructorParameters.Add(member);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        public void ForIgnores(params Expression<Func<TSource, object?>>[] parameters)
        {
            foreach (var item in parameters)
            {
                var member = ExpressionHelper.GetMemberInfoFromLambda(item);
                if (member is not null)
                {
                    ignoreMembers.Add(member.Name);
                }
            }
        }

        internal IReadOnlyList<MappingConfiguration> GetConfigurations()
        {
            return configurations.AsReadOnly();
        }

        internal IReadOnlyList<(Type, string)> GetConstructorParameters()
        {
            return constructorParameters.AsReadOnly();
        }

        internal IReadOnlyList<string> GetIgnoreMembers()
        {
            return ignoreMembers.AsReadOnly();
        }

        internal class MappingConfiguration
        {
            public LambdaExpression? SourceExpression { get; set; }
            public LambdaExpression? DestinationExpression { get; set; }
        }
    }
}
