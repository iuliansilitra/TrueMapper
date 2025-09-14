using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TrueMapper.Core.Configuration;
using TrueMapper.Core.Converters;
using TrueMapper.Core.Interfaces;
using TrueMapper.Core.Performance;

namespace TrueMapper.Core.Core
{
    /// <summary>
    /// Main implementation of TrueMapper - provides object-to-object mapping functionality
    /// </summary>
    public class TrueMapper : ITrueMapper
    {
        internal readonly MappingConfiguration _configuration;
        private readonly MappingMetrics _metrics;
        private readonly SmartTypeConverter _typeConverter;
        private readonly HashSet<object> _circularReferenceTracker;
        private int _currentDepth;

        /// <summary>
        /// Initializes a new instance of TrueMapper with default configuration
        /// </summary>
        public TrueMapper() : this(new MappingConfiguration())
        {
        }

        /// <summary>
        /// Initializes a new instance of TrueMapper with custom configuration
        /// </summary>
        /// <param name="configuration">Custom mapping configuration</param>
        public TrueMapper(MappingConfiguration configuration)
        {
            _configuration = configuration;
            _metrics = new MappingMetrics();
            _typeConverter = new SmartTypeConverter();
            _circularReferenceTracker = new HashSet<object>(ReferenceEqualityComparer.Instance);
        }

        public TDestination Map<TSource, TDestination>(TSource source) where TDestination : new()
        {
            if (source == null)
            {
                return _configuration.GetGlobalSettings().PropagateNulls ? default! : new TDestination();
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var destination = new TDestination();
                return MapInternal(source, destination);
            }
            finally
            {
                stopwatch.Stop();
                if (_configuration.GetGlobalSettings().CollectMetrics)
                {
                    _metrics.RecordMapping(stopwatch.Elapsed.TotalMilliseconds);
                }
            }
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null || destination == null)
            {
                return destination;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                return MapInternal(source, destination);
            }
            finally
            {
                stopwatch.Stop();
                if (_configuration.GetGlobalSettings().CollectMetrics)
                {
                    _metrics.RecordMapping(stopwatch.Elapsed.TotalMilliseconds);
                }
            }
        }

        public IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source) 
            where TDestination : new()
        {
            if (source == null)
            {
                return Enumerable.Empty<TDestination>();
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                return source.Select(item => Map<TSource, TDestination>(item)).ToList();
            }
            finally
            {
                stopwatch.Stop();
                if (_configuration.GetGlobalSettings().CollectMetrics)
                {
                    _metrics.RecordMapping(stopwatch.Elapsed.TotalMilliseconds);
                }
            }
        }

        public T Clone<T>(T source) where T : new()
        {
            if (source == null)
            {
                return default!;
            }

            return Map<T, T>(source);
        }

        public IMappingMetrics GetMetrics()
        {
            return _metrics;
        }

        private TDestination MapInternal<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null || destination == null)
            {
                return destination;
            }

            // Check for circular references
            if (_configuration.GetGlobalSettings().DetectCircularReferences)
            {
                if (_circularReferenceTracker.Contains(source))
                {
                    _metrics.RecordCircularReference();
                    return destination;
                }
                _circularReferenceTracker.Add(source);
            }

            // Check maximum depth
            _currentDepth++;
            if (_currentDepth > _configuration.GetGlobalSettings().MaxMappingDepth)
            {
                _currentDepth--;
                return destination;
            }

            try
            {
                // Get mapping profile if exists
                var profile = _configuration.GetProfile<TSource, TDestination>();
                
                // Apply custom member mappings first
                if (profile != null)
                {
                    foreach (var memberMapping in profile.GetMemberMappings().Cast<MemberMapping<TSource, TDestination>>())
                    {
                        memberMapping.MapFunction(source, destination);
                    }

                    // Apply conditional mappings
                    foreach (var conditionalMapping in profile.GetConditionalMappings().Cast<ConditionalMapping<TSource, TDestination>>())
                    {
                        if (conditionalMapping.Condition(source))
                        {
                            conditionalMapping.TrueAction(source, destination);
                        }
                        else
                        {
                            conditionalMapping.FalseAction?.Invoke(source, destination);
                        }
                    }
                }

                // Perform automatic property mapping
                MapProperties(source, destination, profile?.GetIgnoredMembers());

                // Apply transformations
                if (profile != null)
                {
                    foreach (var transformer in profile.GetTransformers())
                    {
                        destination = transformer(destination);
                    }
                }

                return destination;
            }
            finally
            {
                _currentDepth--;
                if (_configuration.GetGlobalSettings().DetectCircularReferences)
                {
                    _circularReferenceTracker.Remove(source);
                }
            }
        }

        private void MapProperties<TSource, TDestination>(TSource source, TDestination destination, HashSet<string>? ignoredMembers)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p);

            var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p);

            foreach (var destProp in destinationProperties.Values)
            {
                if (ignoredMembers?.Contains(destProp.Name) == true)
                    continue;

                if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
                {
                    try
                    {
                        var sourceValue = sourceProp.GetValue(source);
                        var convertedValue = ConvertValue(sourceValue, sourceProp.PropertyType, destProp.PropertyType);
                        destProp.SetValue(destination, convertedValue);
                    }
                    catch (Exception)
                    {
                        // Skip property if conversion fails
                        continue;
                    }
                }
            }
        }

        private object? ConvertValue(object? value, Type sourceType, Type destinationType)
        {
            if (value == null)
            {
                return _configuration.GetGlobalSettings().PropagateNulls ? null : GetDefaultValue(destinationType);
            }

            if (sourceType == destinationType || destinationType.IsAssignableFrom(sourceType))
            {
                return value;
            }

            // Handle collections
            if (IsCollection(sourceType) && IsCollection(destinationType))
            {
                return ConvertCollection(value, sourceType, destinationType);
            }

            // Handle complex objects
            if (!IsSimpleType(sourceType) && !IsSimpleType(destinationType))
            {
                return ConvertComplexObject(value, destinationType);
            }

            // Use smart type converter for simple types
            return _typeConverter.Convert(value, destinationType);
        }

        private object? ConvertCollection(object value, Type sourceType, Type destinationType)
        {
            if (value is not IEnumerable sourceEnumerable)
                return null;

            var destElementType = GetCollectionElementType(destinationType);
            var sourceElementType = GetCollectionElementType(sourceType);

            if (destElementType == null || sourceElementType == null)
                return null;

            var convertedItems = new List<object?>();
            foreach (var item in sourceEnumerable)
            {
                var convertedItem = ConvertValue(item, sourceElementType, destElementType);
                convertedItems.Add(convertedItem);
            }

            // Create appropriate collection type
            if (destinationType.IsArray)
            {
                var array = Array.CreateInstance(destElementType, convertedItems.Count);
                for (int i = 0; i < convertedItems.Count; i++)
                {
                    array.SetValue(convertedItems[i], i);
                }
                return array;
            }
            else if (destinationType.IsGenericType)
            {
                var listType = typeof(List<>).MakeGenericType(destElementType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");
                
                if (addMethod != null && list != null)
                {
                    foreach (var item in convertedItems)
                    {
                        addMethod.Invoke(list, new[] { item });
                    }
                }
                
                return list;
            }

            return convertedItems;
        }

        private object? ConvertComplexObject(object value, Type destinationType)
        {
            if (Activator.CreateInstance(destinationType) is not object destination)
                return null;

            var mapMethod = GetType().GetMethod(nameof(MapInternal), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.MakeGenericMethod(value.GetType(), destinationType);

            return mapMethod?.Invoke(this, new[] { value, destination });
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(DateTime) || 
                   type == typeof(decimal) || 
                   type == typeof(Guid) ||
                   type == typeof(TimeSpan) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private static bool IsCollection(Type type)
        {
            return type != typeof(string) && 
                   (type.IsArray || 
                    typeof(IEnumerable).IsAssignableFrom(type));
        }

        private static Type? GetCollectionElementType(Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType)
            {
                var genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    return genericArgs[0];
                }
            }

            return typeof(object);
        }

        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}