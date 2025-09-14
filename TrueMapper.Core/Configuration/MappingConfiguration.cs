using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TrueMapper.Core.Interfaces;

namespace TrueMapper.Core.Configuration
{
    /// <summary>
    /// Implementation of mapping configuration
    /// </summary>
    public class MappingConfiguration : IMappingConfiguration
    {
        private readonly Dictionary<(Type, Type), object> _profiles;
        private readonly IGlobalMappingSettings _globalSettings;

        public MappingConfiguration()
        {
            _profiles = new Dictionary<(Type, Type), object>();
            _globalSettings = new GlobalMappingSettings();
        }

        public IMappingProfile<TSource, TDestination> CreateMap<TSource, TDestination>() where TDestination : new()
        {
            var key = (typeof(TSource), typeof(TDestination));
            if (_profiles.ContainsKey(key))
            {
                return (IMappingProfile<TSource, TDestination>)_profiles[key];
            }

            var profile = new MappingProfile<TSource, TDestination>();
            _profiles[key] = profile;
            return profile;
        }

        public void Configure(Action<IGlobalMappingSettings> configAction)
        {
            configAction(_globalSettings);
        }

        public IGlobalMappingSettings GetGlobalSettings() => _globalSettings;

        public IMappingProfile<TSource, TDestination>? GetProfile<TSource, TDestination>()
        {
            var key = (typeof(TSource), typeof(TDestination));
            return _profiles.TryGetValue(key, out var profile) 
                ? (IMappingProfile<TSource, TDestination>)profile 
                : null;
        }
    }

    /// <summary>
    /// Implementation of global mapping settings
    /// </summary>
    public class GlobalMappingSettings : IGlobalMappingSettings
    {
        public bool DetectCircularReferences { get; set; } = true;
        public bool CollectMetrics { get; set; } = true;
        public int MaxMappingDepth { get; set; } = 10;
        public bool PropagateNulls { get; set; } = true;
    }

    /// <summary>
    /// Implementation of mapping profile for specific type pair
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    public class MappingProfile<TSource, TDestination> : IMappingProfile<TSource, TDestination>
    {
        private readonly List<MemberMapping<TSource, TDestination>> _memberMappings;
        private readonly List<ConditionalMapping<TSource, TDestination>> _conditionalMappings;
        private readonly HashSet<string> _ignoredMembers;
        private readonly List<Func<TDestination, TDestination>> _transformers;

        public MappingProfile()
        {
            _memberMappings = new List<MemberMapping<TSource, TDestination>>();
            _conditionalMappings = new List<ConditionalMapping<TSource, TDestination>>();
            _ignoredMembers = new HashSet<string>();
            _transformers = new List<Func<TDestination, TDestination>>();
        }

        public IMappingProfile<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Func<TSource, TMember> mapExpression)
        {
            var memberName = GetMemberName(destinationMember);
            _memberMappings.Add(new MemberMapping<TSource, TDestination>
            {
                MemberName = memberName,
                MapFunction = (source, dest) =>
                {
                    var value = mapExpression(source);
                    SetMemberValue(dest, memberName, value);
                }
            });
            return this;
        }

        public IMappingProfile<TSource, TDestination> When(
            Func<TSource, bool> condition,
            Action<TSource, TDestination> trueAction,
            Action<TSource, TDestination>? falseAction = null)
        {
            _conditionalMappings.Add(new ConditionalMapping<TSource, TDestination>
            {
                Condition = condition,
                TrueAction = trueAction,
                FalseAction = falseAction
            });
            return this;
        }

        public IMappingProfile<TSource, TDestination> Ignore<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember)
        {
            var memberName = GetMemberName(destinationMember);
            _ignoredMembers.Add(memberName);
            return this;
        }

        public IMappingProfile<TSource, TDestination> Transform(Func<TDestination, TDestination> transformer)
        {
            _transformers.Add(transformer);
            return this;
        }

        public IEnumerable<object> GetMemberMappings() => _memberMappings.Cast<object>();
        public IEnumerable<object> GetConditionalMappings() => _conditionalMappings.Cast<object>();
        public HashSet<string> GetIgnoredMembers() => _ignoredMembers;
        public IEnumerable<Func<TDestination, TDestination>> GetTransformers() => _transformers;

        private string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a member access", nameof(expression));
        }

        private void SetMemberValue(TDestination obj, string memberName, object? value)
        {
            var type = typeof(TDestination);
            var property = type.GetProperty(memberName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
                return;
            }

            var field = type.GetField(memberName);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }

    /// <summary>
    /// Represents a custom member mapping
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    public class MemberMapping<TSource, TDestination>
    {
        public string MemberName { get; set; } = string.Empty;
        public Action<TSource, TDestination> MapFunction { get; set; } = null!;
    }

    /// <summary>
    /// Represents a conditional mapping
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    public class ConditionalMapping<TSource, TDestination>
    {
        public Func<TSource, bool> Condition { get; set; } = null!;
        public Action<TSource, TDestination> TrueAction { get; set; } = null!;
        public Action<TSource, TDestination>? FalseAction { get; set; }
    }
}