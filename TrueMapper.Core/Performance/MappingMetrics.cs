using System;
using TrueMapper.Core.Interfaces;

namespace TrueMapper.Core.Performance
{
    /// <summary>
    /// Implementation of mapping performance metrics
    /// </summary>
    public class MappingMetrics : IMappingMetrics
    {
        private long _totalMappings;
        private double _totalMappingTime;
        private long _circularReferencesDetected;
        private readonly MappingMemoryStats _memoryStats;

        public MappingMetrics()
        {
            _memoryStats = new MappingMemoryStats();
        }

        public long TotalMappings => _totalMappings;

        public double AverageMappingTime =>
            _totalMappings > 0 ? _totalMappingTime / _totalMappings : 0;

        public double TotalMappingTime => _totalMappingTime;

        public long CircularReferencesDetected => _circularReferencesDetected;

        public IMappingMemoryStats MemoryStats => _memoryStats;

        public void RecordMapping(double elapsedMilliseconds)
        {
            _totalMappings++;
            _totalMappingTime += elapsedMilliseconds;
        }

        public void RecordCircularReference()
        {
            _circularReferencesDetected++;
        }

        public void Reset()
        {
            _totalMappings = 0;
            _totalMappingTime = 0;
            _circularReferencesDetected = 0;
            _memoryStats.Reset();
        }
    }

    /// <summary>
    /// Implementation of mapping memory statistics
    /// </summary>
    public class MappingMemoryStats : IMappingMemoryStats
    {
        private long _peakMemoryUsage;
        private int _garbageCollections;

        public long PeakMemoryUsage => _peakMemoryUsage;

        public long CurrentMemoryUsage => GC.GetTotalMemory(false);

        public int GarbageCollections => _garbageCollections;

        public void UpdateMemoryUsage()
        {
            var currentMemory = CurrentMemoryUsage;
            if (currentMemory > _peakMemoryUsage)
            {
                _peakMemoryUsage = currentMemory;
            }
        }

        public void RecordGarbageCollection()
        {
            _garbageCollections++;
        }

        public void Reset()
        {
            _peakMemoryUsage = 0;
            _garbageCollections = 0;
        }
    }
}