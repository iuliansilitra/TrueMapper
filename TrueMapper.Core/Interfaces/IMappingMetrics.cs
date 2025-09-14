using System;

namespace TrueMapper.Core.Interfaces
{
    /// <summary>
    /// Performance metrics for mapping operations
    /// </summary>
    public interface IMappingMetrics
    {
        /// <summary>
        /// Total number of mapping operations performed
        /// </summary>
        long TotalMappings { get; }

        /// <summary>
        /// Average time per mapping operation in milliseconds
        /// </summary>
        double AverageMappingTime { get; }

        /// <summary>
        /// Total time spent on all mapping operations in milliseconds
        /// </summary>
        double TotalMappingTime { get; }

        /// <summary>
        /// Number of circular references detected
        /// </summary>
        long CircularReferencesDetected { get; }

        /// <summary>
        /// Memory usage statistics
        /// </summary>
        IMappingMemoryStats MemoryStats { get; }

        /// <summary>
        /// Resets all metrics
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Memory usage statistics for mapping operations
    /// </summary>
    public interface IMappingMemoryStats
    {
        /// <summary>
        /// Peak memory usage during mapping operations in bytes
        /// </summary>
        long PeakMemoryUsage { get; }

        /// <summary>
        /// Current memory usage in bytes
        /// </summary>
        long CurrentMemoryUsage { get; }

        /// <summary>
        /// Number of garbage collections triggered during mapping
        /// </summary>
        int GarbageCollections { get; }
    }
}