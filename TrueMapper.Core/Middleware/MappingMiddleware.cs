using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrueMapper.Core.Middleware
{
    /// <summary>
    /// Context for mapping operations passed through middleware pipeline
    /// </summary>
    public class MappingContext
    {
        public object Source { get; set; } = null!;
        public object Destination { get; set; } = null!;
        public Type SourceType { get; set; } = null!;
        public Type DestinationType { get; set; } = null!;
        public Dictionary<string, object> Properties { get; set; } = new();
        public bool IsCancelled { get; set; }
        public string? CancellationReason { get; set; }
    }

    /// <summary>
    /// Delegate for mapping middleware
    /// </summary>
    /// <param name="context">Mapping context</param>
    /// <param name="next">Next middleware in pipeline</param>
    public delegate Task MappingMiddleware(MappingContext context, Func<Task> next);

    /// <summary>
    /// Middleware pipeline for mapping operations
    /// </summary>
    public class MappingPipeline
    {
        private readonly List<MappingMiddleware> _middlewares = new();

        /// <summary>
        /// Adds middleware to the pipeline
        /// </summary>
        /// <param name="middleware">Middleware to add</param>
        public void Use(MappingMiddleware middleware)
        {
            _middlewares.Add(middleware);
        }

        /// <summary>
        /// Executes the middleware pipeline
        /// </summary>
        /// <param name="context">Mapping context</param>
        /// <param name="finalAction">Final action to execute after all middleware</param>
        public async Task ExecuteAsync(MappingContext context, Func<Task> finalAction)
        {
            await ExecuteMiddleware(context, 0, finalAction);
        }

        private async Task ExecuteMiddleware(MappingContext context, int index, Func<Task> finalAction)
        {
            if (context.IsCancelled)
                return;

            if (index >= _middlewares.Count)
            {
                await finalAction();
                return;
            }

            var middleware = _middlewares[index];
            await middleware(context, () => ExecuteMiddleware(context, index + 1, finalAction));
        }
    }

    /// <summary>
    /// Built-in middleware for validation
    /// </summary>
    public static class ValidationMiddleware
    {
        /// <summary>
        /// Creates validation middleware that checks for null sources
        /// </summary>
        public static MappingMiddleware NullCheck()
        {
            return async (context, next) =>
            {
                if (context.Source == null)
                {
                    context.IsCancelled = true;
                    context.CancellationReason = "Source object is null";
                    return;
                }
                await next();
            };
        }

        /// <summary>
        /// Creates validation middleware with custom validation logic
        /// </summary>
        /// <param name="validator">Custom validation function</param>
        /// <param name="errorMessage">Error message when validation fails</param>
        public static MappingMiddleware Custom(Func<MappingContext, bool> validator, string errorMessage)
        {
            return async (context, next) =>
            {
                if (!validator(context))
                {
                    context.IsCancelled = true;
                    context.CancellationReason = errorMessage;
                    return;
                }
                await next();
            };
        }
    }

    /// <summary>
    /// Built-in middleware for logging mapping operations
    /// </summary>
    public static class LoggingMiddleware
    {
        /// <summary>
        /// Creates logging middleware with custom logger
        /// </summary>
        /// <param name="logger">Logger function</param>
        public static MappingMiddleware Create(Action<string> logger)
        {
            return async (context, next) =>
            {
                var startTime = DateTime.UtcNow;
                logger($"Starting mapping: {context.SourceType.Name} -> {context.DestinationType.Name}");
                
                try
                {
                    await next();
                    var duration = DateTime.UtcNow - startTime;
                    logger($"Completed mapping: {context.SourceType.Name} -> {context.DestinationType.Name} in {duration.TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    var duration = DateTime.UtcNow - startTime;
                    logger($"Failed mapping: {context.SourceType.Name} -> {context.DestinationType.Name} in {duration.TotalMilliseconds}ms. Error: {ex.Message}");
                    throw;
                }
            };
        }
    }

    /// <summary>
    /// Built-in middleware for caching mapping results
    /// </summary>
    public static class CachingMiddleware
    {
        private static readonly Dictionary<string, object> _cache = new();

        /// <summary>
        /// Creates caching middleware with custom key generator
        /// </summary>
        /// <param name="keyGenerator">Function to generate cache keys</param>
        /// <param name="ttlMinutes">Time to live in minutes (0 for no expiry)</param>
        public static MappingMiddleware Create(Func<MappingContext, string> keyGenerator, int ttlMinutes = 0)
        {
            return async (context, next) =>
            {
                var key = keyGenerator(context);
                
                if (_cache.TryGetValue(key, out var cachedResult))
                {
                    if (cachedResult is CacheEntry entry)
                    {
                        if (ttlMinutes == 0 || DateTime.UtcNow - entry.Timestamp < TimeSpan.FromMinutes(ttlMinutes))
                        {
                            context.Destination = entry.Value;
                            return;
                        }
                        else
                        {
                            _cache.Remove(key);
                        }
                    }
                }

                await next();

                if (!context.IsCancelled && context.Destination != null)
                {
                    _cache[key] = new CacheEntry
                    {
                        Value = context.Destination,
                        Timestamp = DateTime.UtcNow
                    };
                }
            };
        }

        private class CacheEntry
        {
            public object Value { get; set; } = null!;
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Clears the entire cache
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Built-in middleware for transformation
    /// </summary>
    public static class TransformationMiddleware
    {
        /// <summary>
        /// Creates transformation middleware that applies before mapping
        /// </summary>
        /// <param name="sourceTransformer">Source transformation function</param>
        public static MappingMiddleware PreTransform(Func<object, object> sourceTransformer)
        {
            return async (context, next) =>
            {
                context.Source = sourceTransformer(context.Source);
                await next();
            };
        }

        /// <summary>
        /// Creates transformation middleware that applies after mapping
        /// </summary>
        /// <param name="destinationTransformer">Destination transformation function</param>
        public static MappingMiddleware PostTransform(Func<object, object> destinationTransformer)
        {
            return async (context, next) =>
            {
                await next();
                if (!context.IsCancelled && context.Destination != null)
                {
                    context.Destination = destinationTransformer(context.Destination);
                }
            };
        }

        /// <summary>
        /// Creates conditional transformation middleware
        /// </summary>
        /// <param name="condition">Condition to check</param>
        /// <param name="transformer">Transformation to apply if condition is true</param>
        public static MappingMiddleware ConditionalTransform(
            Func<MappingContext, bool> condition, 
            Func<object, object> transformer)
        {
            return async (context, next) =>
            {
                await next();
                if (!context.IsCancelled && condition(context) && context.Destination != null)
                {
                    context.Destination = transformer(context.Destination);
                }
            };
        }
    }
}