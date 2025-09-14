using System;
using System.Collections.Generic;

namespace TrueMapper.Core.Interfaces
{
    /// <summary>
    /// Interface for mapping configuration and setup
    /// </summary>
    public interface IMappingConfiguration
    {
        /// <summary>
        /// Creates a mapping profile between two types
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <returns>Mapping profile builder</returns>
        IMappingProfile<TSource, TDestination> CreateMap<TSource, TDestination>() where TDestination : new();

        /// <summary>
        /// Configures global mapping settings
        /// </summary>
        /// <param name="configAction">Configuration action</param>
        void Configure(Action<IGlobalMappingSettings> configAction);
    }

    /// <summary>
    /// Mapping profile for specific type pair
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    public interface IMappingProfile<TSource, TDestination>
    {
        /// <summary>
        /// Maps a property with custom logic
        /// </summary>
        /// <typeparam name="TMember">Property type</typeparam>
        /// <param name="destinationMember">Destination property selector</param>
        /// <param name="mapExpression">Custom mapping expression</param>
        /// <returns>Current profile for chaining</returns>
        IMappingProfile<TSource, TDestination> ForMember<TMember>(
            System.Linq.Expressions.Expression<Func<TDestination, TMember>> destinationMember,
            Func<TSource, TMember> mapExpression);

        /// <summary>
        /// Adds conditional mapping logic
        /// </summary>
        /// <param name="condition">Condition to evaluate</param>
        /// <param name="trueAction">Action when condition is true</param>
        /// <param name="falseAction">Action when condition is false (optional)</param>
        /// <returns>Current profile for chaining</returns>
        IMappingProfile<TSource, TDestination> When(
            Func<TSource, bool> condition,
            Action<TSource, TDestination> trueAction,
            Action<TSource, TDestination>? falseAction = null);

        /// <summary>
        /// Ignores a specific property during mapping
        /// </summary>
        /// <typeparam name="TMember">Property type</typeparam>
        /// <param name="destinationMember">Property to ignore</param>
        /// <returns>Current profile for chaining</returns>
        IMappingProfile<TSource, TDestination> Ignore<TMember>(
            System.Linq.Expressions.Expression<Func<TDestination, TMember>> destinationMember);

        /// <summary>
        /// Adds a custom transformation step
        /// </summary>
        /// <param name="transformer">Transformation function</param>
        /// <returns>Current profile for chaining</returns>
        IMappingProfile<TSource, TDestination> Transform(Func<TDestination, TDestination> transformer);

        /// <summary>
        /// Gets all member mappings
        /// </summary>
        /// <returns>Collection of member mappings</returns>
        IEnumerable<object> GetMemberMappings();

        /// <summary>
        /// Gets all conditional mappings
        /// </summary>
        /// <returns>Collection of conditional mappings</returns>
        IEnumerable<object> GetConditionalMappings();

        /// <summary>
        /// Gets all ignored members
        /// </summary>
        /// <returns>Set of ignored member names</returns>
        HashSet<string> GetIgnoredMembers();

        /// <summary>
        /// Gets all transformers
        /// </summary>
        /// <returns>Collection of transformers</returns>
        IEnumerable<Func<TDestination, TDestination>> GetTransformers();
    }

    /// <summary>
    /// Global mapping settings
    /// </summary>
    public interface IGlobalMappingSettings
    {
        /// <summary>
        /// Enable or disable circular reference detection
        /// </summary>
        bool DetectCircularReferences { get; set; }

        /// <summary>
        /// Enable or disable performance metrics collection
        /// </summary>
        bool CollectMetrics { get; set; }

        /// <summary>
        /// Maximum depth for nested object mapping
        /// </summary>
        int MaxMappingDepth { get; set; }

        /// <summary>
        /// Enable or disable null value propagation
        /// </summary>
        bool PropagateNulls { get; set; }
    }
}