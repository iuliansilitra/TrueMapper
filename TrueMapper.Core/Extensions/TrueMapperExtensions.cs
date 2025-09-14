using System.Collections.Generic;
using TrueMapper.Core.Configuration;
using TrueMapper.Core.Core;
using TrueMapper.Core.Interfaces;

namespace TrueMapper.Core.Extensions
{
    /// <summary>
    /// Extension methods for easy TrueMapper usage
    /// </summary>
    public static class TrueMapperExtensions
    {
        private static readonly Core.TrueMapper _defaultMapper = new();

        /// <summary>
        /// Maps current object to destination type using default mapper
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public static TDestination MapTo<TSource, TDestination>(this TSource source) where TDestination : new()
        {
            return _defaultMapper.Map<TSource, TDestination>(source);
        }

        /// <summary>
        /// Maps current object to destination type using default mapper
        /// </summary>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public static TDestination MapTo<TDestination>(this object source) where TDestination : new()
        {
            var sourceType = source?.GetType() ?? typeof(object);
            var mapMethod = typeof(Core.TrueMapper).GetMethod(nameof(Core.TrueMapper.Map))
                ?.MakeGenericMethod(sourceType, typeof(TDestination));
            return (TDestination)(mapMethod?.Invoke(_defaultMapper, new[] { source }) ?? Activator.CreateInstance<TDestination>());
        }

        /// <summary>
        /// Maps current object to existing destination instance using default mapper
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <param name="source">Source object</param>
        /// <param name="destination">Existing destination object</param>
        /// <returns>Updated destination object</returns>
        public static TDestination MapTo<TSource, TDestination>(this TSource source, TDestination destination)
        {
            return _defaultMapper.Map(source, destination);
        }

        /// <summary>
        /// Maps a collection to destination type using default mapper
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <param name="source">Source collection</param>
        /// <returns>Mapped destination collection</returns>
        public static IEnumerable<TDestination> MapTo<TSource, TDestination>(this IEnumerable<TSource> source) 
            where TDestination : new()
        {
            return _defaultMapper.Map<TSource, TDestination>(source);
        }

        /// <summary>
        /// Creates a deep clone of the current object using default mapper
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="source">Source object to clone</param>
        /// <returns>Deep cloned object</returns>
        public static T DeepClone<T>(this T source) where T : new()
        {
            return _defaultMapper.Clone(source);
        }

        /// <summary>
        /// Gets the default mapper instance
        /// </summary>
        /// <returns>Default TrueMapper instance</returns>
        public static ITrueMapper GetDefaultMapper()
        {
            return _defaultMapper;
        }

        /// <summary>
        /// Configures the default mapper
        /// </summary>
        /// <param name="configAction">Configuration action</param>
        public static void ConfigureDefault(System.Action<IMappingConfiguration> configAction)
        {
            configAction(_defaultMapper._configuration);
        }
    }

    /// <summary>
    /// Fluent configuration extensions
    /// </summary>
    public static class FluentConfigurationExtensions
    {
        /// <summary>
        /// Starts fluent configuration for TrueMapper
        /// </summary>
        /// <param name="mapper">TrueMapper instance</param>
        /// <returns>Fluent configurator</returns>
        public static FluentConfigurator Configure(this Core.TrueMapper mapper)
        {
            return new FluentConfigurator(mapper._configuration);
        }
    }

    /// <summary>
    /// Fluent configurator for easy setup
    /// </summary>
    public class FluentConfigurator
    {
        private readonly MappingConfiguration _configuration;

        internal FluentConfigurator(MappingConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a mapping profile with fluent syntax
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <returns>Mapping profile</returns>
        public IMappingProfile<TSource, TDestination> CreateMap<TSource, TDestination>() 
            where TDestination : new()
        {
            return _configuration.CreateMap<TSource, TDestination>();
        }

        /// <summary>
        /// Configures global settings with fluent syntax
        /// </summary>
        /// <param name="configAction">Configuration action</param>
        /// <returns>Current configurator for chaining</returns>
        public FluentConfigurator WithGlobalSettings(System.Action<IGlobalMappingSettings> configAction)
        {
            _configuration.Configure(configAction);
            return this;
        }

        /// <summary>
        /// Enables circular reference detection
        /// </summary>
        /// <param name="enabled">Enable or disable</param>
        /// <returns>Current configurator for chaining</returns>
        public FluentConfigurator WithCircularReferenceDetection(bool enabled = true)
        {
            _configuration.Configure(settings => settings.DetectCircularReferences = enabled);
            return this;
        }

        /// <summary>
        /// Enables performance metrics collection
        /// </summary>
        /// <param name="enabled">Enable or disable</param>
        /// <returns>Current configurator for chaining</returns>
        public FluentConfigurator WithMetrics(bool enabled = true)
        {
            _configuration.Configure(settings => settings.CollectMetrics = enabled);
            return this;
        }

        /// <summary>
        /// Sets maximum mapping depth
        /// </summary>
        /// <param name="maxDepth">Maximum depth</param>
        /// <returns>Current configurator for chaining</returns>
        public FluentConfigurator WithMaxDepth(int maxDepth)
        {
            _configuration.Configure(settings => settings.MaxMappingDepth = maxDepth);
            return this;
        }

        /// <summary>
        /// Configures null value propagation
        /// </summary>
        /// <param name="propagate">Whether to propagate nulls</param>
        /// <returns>Current configurator for chaining</returns>
        public FluentConfigurator WithNullPropagation(bool propagate = true)
        {
            _configuration.Configure(settings => settings.PropagateNulls = propagate);
            return this;
        }
    }
}