using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            var destinationType = typeof(TDestination);
            
            // If source is a collection (but not string which implements IEnumerable)
            if (source is IEnumerable enumerable && source is not string)
            {
                // If destination is a collection type -> map collection to collection
                if (IsCollectionType(destinationType))
                {
                    return MapToCollectionType<TDestination>(enumerable, destinationType);
                }
                // If destination is not a collection -> this is an error
                else
                {
                    throw new ArgumentException($"Cannot map collection to non-collection type {destinationType.Name}. Use MapTo<List<{destinationType.Name}>>() instead or call MapTo<{destinationType.Name}>() on individual items.");
                }
            }
            else
            {
                // Source is a single object
                if (IsCollectionType(destinationType))
                {
                    throw new ArgumentException($"Cannot map single object to collection type {destinationType.Name}. Source must be a collection to map to {destinationType.Name}.");
                }
                // Single object to single object mapping
                return _defaultMapper.Map<TDestination>(source);
            }
        }

        private static bool IsCollectionType(Type type)
        {
            // Arrays
            if (type.IsArray) return true;
            
            // Generic collections
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                return genericDef == typeof(List<>) ||
                       genericDef == typeof(IList<>) ||
                       genericDef == typeof(ICollection<>) ||
                       genericDef == typeof(IEnumerable<>) ||
                       genericDef == typeof(HashSet<>) ||
                       genericDef == typeof(Queue<>) ||
                       genericDef == typeof(Stack<>) ||
                       genericDef == typeof(LinkedList<>) ||
                       genericDef == typeof(ObservableCollection<>) ||
                       genericDef == typeof(Collection<>) ||
                       genericDef == typeof(ConcurrentBag<>) ||
                       genericDef == typeof(ConcurrentQueue<>) ||
                       genericDef == typeof(ConcurrentStack<>);
            }
            
            return false;
        }

        private static TDestinationCollection MapToCollectionType<TDestinationCollection>(IEnumerable source, Type destinationType)
        {
            
            // Handle arrays (T[])
            if (destinationType.IsArray)
            {
                var itemType = destinationType.GetElementType()!;
                var listResult = MapCollectionToList(source, itemType);
                var array = Array.CreateInstance(itemType, listResult.Count);
                for (int i = 0; i < listResult.Count; i++)
                {
                    array.SetValue(listResult[i], i);
                }
                return (TDestinationCollection)(object)array;
            }
            
            // Handle generic collections
            if (destinationType.IsGenericType)
            {
                var itemType = destinationType.GetGenericArguments()[0];
                var listResult = MapCollectionToList(source, itemType);
                var genericTypeDefinition = destinationType.GetGenericTypeDefinition();
                
                // List<T>
                if (genericTypeDefinition == typeof(List<>))
                {
                    return (TDestinationCollection)(object)CreateTypedList(listResult, itemType);
                }
                
                // HashSet<T>
                if (genericTypeDefinition == typeof(HashSet<>))
                {
                    var hashSetType = typeof(HashSet<>).MakeGenericType(itemType);
                    var hashSet = Activator.CreateInstance(hashSetType)!;
                    var addMethod = hashSetType.GetMethod("Add")!;
                    foreach (var item in listResult)
                    {
                        addMethod.Invoke(hashSet, new[] { item });
                    }
                    return (TDestinationCollection)hashSet;
                }
                
                // LinkedList<T>
                if (genericTypeDefinition == typeof(LinkedList<>))
                {
                    var linkedListType = typeof(LinkedList<>).MakeGenericType(itemType);
                    var linkedList = Activator.CreateInstance(linkedListType)!;
                    var addLastMethod = linkedListType.GetMethod("AddLast", new[] { itemType })!;
                    foreach (var item in listResult)
                    {
                        addLastMethod.Invoke(linkedList, new[] { item });
                    }
                    return (TDestinationCollection)linkedList;
                }
                
                // Queue<T>
                if (genericTypeDefinition == typeof(Queue<>))
                {
                    var queueType = typeof(Queue<>).MakeGenericType(itemType);
                    var queue = Activator.CreateInstance(queueType)!;
                    var enqueueMethod = queueType.GetMethod("Enqueue")!;
                    foreach (var item in listResult)
                    {
                        enqueueMethod.Invoke(queue, new[] { item });
                    }
                    return (TDestinationCollection)queue;
                }
                
                // Stack<T>
                if (genericTypeDefinition == typeof(Stack<>))
                {
                    var stackType = typeof(Stack<>).MakeGenericType(itemType);
                    var stack = Activator.CreateInstance(stackType)!;
                    var pushMethod = stackType.GetMethod("Push")!;
                    // Push in reverse order to maintain original order when popped
                    for (int i = listResult.Count - 1; i >= 0; i--)
                    {
                        pushMethod.Invoke(stack, new[] { listResult[i] });
                    }
                    return (TDestinationCollection)stack;
                }
                
                // ObservableCollection<T>
                if (genericTypeDefinition == typeof(ObservableCollection<>))
                {
                    var observableType = typeof(ObservableCollection<>).MakeGenericType(itemType);
                    var typedList = CreateTypedList(listResult, itemType);
                    var observable = Activator.CreateInstance(observableType, typedList)!;
                    return (TDestinationCollection)observable;
                }
                
                // Collection<T>
                if (genericTypeDefinition == typeof(Collection<>))
                {
                    var collectionType = typeof(Collection<>).MakeGenericType(itemType);
                    var typedList = CreateTypedList(listResult, itemType);
                    var collection = Activator.CreateInstance(collectionType, typedList)!;
                    return (TDestinationCollection)collection;
                }
                
                // ConcurrentBag<T>
                if (genericTypeDefinition == typeof(ConcurrentBag<>))
                {
                    var bagType = typeof(ConcurrentBag<>).MakeGenericType(itemType);
                    var bag = Activator.CreateInstance(bagType)!;
                    var addMethod = bagType.GetMethod("Add")!;
                    foreach (var item in listResult)
                    {
                        addMethod.Invoke(bag, new[] { item });
                    }
                    return (TDestinationCollection)bag;
                }
                
                // ConcurrentQueue<T>
                if (genericTypeDefinition == typeof(ConcurrentQueue<>))
                {
                    var concurrentQueueType = typeof(ConcurrentQueue<>).MakeGenericType(itemType);
                    var concurrentQueue = Activator.CreateInstance(concurrentQueueType)!;
                    var enqueueMethod = concurrentQueueType.GetMethod("Enqueue")!;
                    foreach (var item in listResult)
                    {
                        enqueueMethod.Invoke(concurrentQueue, new[] { item });
                    }
                    return (TDestinationCollection)concurrentQueue;
                }
                
                // ConcurrentStack<T>
                if (genericTypeDefinition == typeof(ConcurrentStack<>))
                {
                    var concurrentStackType = typeof(ConcurrentStack<>).MakeGenericType(itemType);
                    var concurrentStack = Activator.CreateInstance(concurrentStackType)!;
                    var pushRangeMethod = concurrentStackType.GetMethod("PushRange", new[] { itemType.MakeArrayType() });
                    if (pushRangeMethod != null)
                    {
                        var array = Array.CreateInstance(itemType, listResult.Count);
                        for (int i = 0; i < listResult.Count; i++)
                        {
                            array.SetValue(listResult[listResult.Count - 1 - i], i); // Reverse order
                        }
                        pushRangeMethod.Invoke(concurrentStack, new object[] { array });
                    }
                    return (TDestinationCollection)concurrentStack;
                }
                
                // Interface types - return List<T> cast to interface
                if (destinationType.IsInterface)
                {
                    var typedList = CreateTypedList(listResult, itemType);
                    return (TDestinationCollection)(object)typedList;
                }
                
                // Generic collections with constructors that accept IEnumerable<T>
                try
                {
                    var typedList = CreateTypedList(listResult, itemType);
                    var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
                    var constructor = destinationType.GetConstructor(new[] { enumerableType });
                    if (constructor != null)
                    {
                        var instance = constructor.Invoke(new object[] { typedList });
                        return (TDestinationCollection)instance!;
                    }
                }
                catch { }
                
                // Generic collections with parameterless constructors + Add method
                try
                {
                    var instance = Activator.CreateInstance(destinationType)!;
                    var addMethod = destinationType.GetMethod("Add", new[] { itemType });
                    if (addMethod != null)
                    {
                        foreach (var item in listResult)
                        {
                            addMethod.Invoke(instance, new[] { item });
                        }
                        return (TDestinationCollection)instance;
                    }
                }
                catch { }
                
                // Fallback: return List<T> cast to destination type
                var fallbackList = CreateTypedList(listResult, itemType);
                return (TDestinationCollection)(object)fallbackList;
            }
            
            throw new NotSupportedException($"Collection type {destinationType.Name} is not supported. Supported types include: List<T>, T[], HashSet<T>, Queue<T>, Stack<T>, ObservableCollection<T>, ConcurrentBag<T>, and other collections with Add methods or IEnumerable<T> constructors.");
        }

        private static object CreateTypedList(List<object> items, Type itemType)
        {
            var listType = typeof(List<>).MakeGenericType(itemType);
            var typedList = Activator.CreateInstance(listType)!;
            var addMethod = listType.GetMethod("Add")!;
            
            foreach (var item in items)
            {
                addMethod.Invoke(typedList, new[] { item });
            }
            
            return typedList;
        }

        private static List<object> MapCollectionToList(IEnumerable source, Type itemType)
        {
            var result = new List<object>();
            var mapMethod = _defaultMapper.GetType().GetMethod("Map", new[] { typeof(object) })
                ?.MakeGenericMethod(itemType);
                
            foreach (var item in source)
            {
                if (item != null && mapMethod != null)
                {
                    var mappedItem = mapMethod.Invoke(_defaultMapper, new object[] { item });
                    if (mappedItem != null)
                    {
                        result.Add(mappedItem);
                    }
                }
                else
                {
                    // Handle null items - preserve them as null in result
                    result.Add(null!);
                }
            }
            return result;
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