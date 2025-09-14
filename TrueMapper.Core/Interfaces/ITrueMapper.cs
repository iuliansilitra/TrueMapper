using System.Collections.Generic;

namespace TrueMapper.Core.Interfaces
{
    /// <summary>
    /// Main interface for TrueMapper - provides object-to-object mapping functionality
    /// </summary>
    public interface ITrueMapper
    {
        /// <summary>
        /// Maps an object of type TSource to TDestination
        /// </summary>
        /// <typeparam name="TSource">Source object type</typeparam>
        /// <typeparam name="TDestination">Destination object type</typeparam>
        /// <param name="source">Source object to map from</param>
        /// <returns>Mapped destination object</returns>
        TDestination Map<TSource, TDestination>(TSource source) where TDestination : new();

        /// <summary>
        /// Maps an object of type TSource to an existing TDestination instance
        /// </summary>
        /// <typeparam name="TSource">Source object type</typeparam>
        /// <typeparam name="TDestination">Destination object type</typeparam>
        /// <param name="source">Source object to map from</param>
        /// <param name="destination">Existing destination object to map to</param>
        /// <returns>Updated destination object</returns>
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

        /// <summary>
        /// Maps a collection of objects from TSource to TDestination
        /// </summary>
        /// <typeparam name="TSource">Source object type</typeparam>
        /// <typeparam name="TDestination">Destination object type</typeparam>
        /// <param name="source">Collection of source objects</param>
        /// <returns>Collection of mapped destination objects</returns>
        IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source) where TDestination : new();

        /// <summary>
        /// Creates a deep clone of an object
        /// </summary>
        /// <typeparam name="T">Type of object to clone</typeparam>
        /// <param name="source">Object to clone</param>
        /// <returns>Deep cloned object</returns>
        T Clone<T>(T source) where T : new();

        /// <summary>
        /// Gets performance metrics for mapping operations
        /// </summary>
        /// <returns>Performance metrics</returns>
        IMappingMetrics GetMetrics();

        /// <summary>
        /// Maps an object to TDestination (AutoMapper-style with single generic)
        /// </summary>
        /// <typeparam name="TDestination">Destination object type</typeparam>
        /// <param name="source">Source object to map from</param>
        /// <returns>Mapped destination object</returns>
        TDestination Map<TDestination>(object source) where TDestination : new();

        /// <summary>
        /// Maps a collection to List&lt;TDestination&gt; (AutoMapper-style with single generic)
        /// </summary>
        /// <typeparam name="TDestination">Destination object type</typeparam>
        /// <param name="source">Collection of source objects</param>
        /// <returns>List of mapped destination objects</returns>
        List<TDestination> Map<TDestination>(IEnumerable<object> source) where TDestination : new();
    }
}