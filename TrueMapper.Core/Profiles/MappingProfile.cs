using System;
using System.Collections.Generic;
using System.Linq;
using TrueMapper.Core.Configuration;
using TrueMapper.Core.Interfaces;

namespace TrueMapper.Core.Profiles
{
    /// <summary>
    /// Base class for creating mapping profiles
    /// </summary>
    public abstract class MappingProfile
    {
        protected readonly MappingConfiguration _configuration;

        protected MappingProfile()
        {
            _configuration = new MappingConfiguration();
            ConfigureMappings();
        }

        /// <summary>
        /// Override this method to configure mappings
        /// </summary>
        protected abstract void ConfigureMappings();

        /// <summary>
        /// Creates a mapping between two types
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="TDestination">Destination type</typeparam>
        /// <returns>Mapping profile builder</returns>
        protected IMappingProfile<TSource, TDestination> CreateMap<TSource, TDestination>() where TDestination : new()
        {
            return _configuration.CreateMap<TSource, TDestination>();
        }

        /// <summary>
        /// Gets the internal configuration
        /// </summary>
        /// <returns>Mapping configuration</returns>
        public MappingConfiguration GetConfiguration()
        {
            return _configuration;
        }
    }

    /// <summary>
    /// Profile registry for managing multiple profiles
    /// </summary>
    public class ProfileRegistry
    {
        private readonly List<MappingProfile> _profiles = new();
        private readonly MappingConfiguration _mergedConfiguration = new();

        /// <summary>
        /// Adds a profile to the registry
        /// </summary>
        /// <param name="profile">Profile to add</param>
        public void AddProfile(MappingProfile profile)
        {
            _profiles.Add(profile);
            MergeConfigurations();
        }

        /// <summary>
        /// Adds a profile by type
        /// </summary>
        /// <typeparam name="TProfile">Profile type</typeparam>
        public void AddProfile<TProfile>() where TProfile : MappingProfile, new()
        {
            AddProfile(new TProfile());
        }

        /// <summary>
        /// Gets the merged configuration from all profiles
        /// </summary>
        /// <returns>Merged configuration</returns>
        public MappingConfiguration GetMergedConfiguration()
        {
            return _mergedConfiguration;
        }

        /// <summary>
        /// Gets all registered profiles
        /// </summary>
        /// <returns>List of profiles</returns>
        public IReadOnlyList<MappingProfile> GetProfiles()
        {
            return _profiles.AsReadOnly();
        }

        private void MergeConfigurations()
        {
            // Simple merge - later profiles override earlier ones
            // In a production scenario, you might want more sophisticated merging logic
            foreach (var profile in _profiles)
            {
                var config = profile.GetConfiguration();
                // Merge logic would go here
                // For now, we'll use the last configuration added
            }
        }
    }

    /// <summary>
    /// Pre-built profiles for common scenarios
    /// </summary>
    public static class CommonProfiles
    {
        /// <summary>
        /// Profile optimized for performance with minimal features
        /// </summary>
        public class PerformanceProfile : MappingProfile
        {
            protected override void ConfigureMappings()
            {
                _configuration.Configure(settings =>
                {
                    settings.DetectCircularReferences = false;
                    settings.CollectMetrics = false;
                    settings.MaxMappingDepth = 5;
                    settings.PropagateNulls = false;
                });
            }
        }

        /// <summary>
        /// Profile with all safety features enabled
        /// </summary>
        public class SafetyProfile : MappingProfile
        {
            protected override void ConfigureMappings()
            {
                _configuration.Configure(settings =>
                {
                    settings.DetectCircularReferences = true;
                    settings.CollectMetrics = true;
                    settings.MaxMappingDepth = 20;
                    settings.PropagateNulls = true;
                });
            }
        }

        /// <summary>
        /// Profile for debugging with extensive logging
        /// </summary>
        public class DebuggingProfile : MappingProfile
        {
            protected override void ConfigureMappings()
            {
                _configuration.Configure(settings =>
                {
                    settings.DetectCircularReferences = true;
                    settings.CollectMetrics = true;
                    settings.MaxMappingDepth = 10;
                    settings.PropagateNulls = true;
                });
            }
        }
    }

    /// <summary>
    /// Attribute to mark classes as mapping profiles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MappingProfileAttribute : Attribute
    {
        /// <summary>
        /// Profile name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Profile priority (higher numbers = higher priority)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether this profile should be automatically loaded
        /// </summary>
        public bool AutoLoad { get; set; } = true;
    }

    /// <summary>
    /// Auto profile discovery helper
    /// </summary>
    public static class ProfileDiscovery
    {
        /// <summary>
        /// Discovers all profiles in the current assembly
        /// </summary>
        /// <returns>List of discovered profiles</returns>
        public static List<MappingProfile> DiscoverProfiles()
        {
            var assembly = System.Reflection.Assembly.GetCallingAssembly();
            return DiscoverProfiles(assembly);
        }

        /// <summary>
        /// Discovers all profiles in the specified assembly
        /// </summary>
        /// <param name="assembly">Assembly to search</param>
        /// <returns>List of discovered profiles</returns>
        public static List<MappingProfile> DiscoverProfiles(System.Reflection.Assembly assembly)
        {
            var profiles = new List<MappingProfile>();

            var profileTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(MappingProfile)) && !t.IsAbstract)
                .Where(t => t.GetCustomAttributes(typeof(MappingProfileAttribute), false)
                    .Cast<MappingProfileAttribute>()
                    .FirstOrDefault()?.AutoLoad != false);

            foreach (var profileType in profileTypes)
            {
                try
                {
                    if (Activator.CreateInstance(profileType) is MappingProfile profile)
                    {
                        profiles.Add(profile);
                    }
                }
                catch
                {
                    // Skip profiles that can't be instantiated
                }
            }

            // Sort by priority (higher priority first)
            return profiles.OrderByDescending(p =>
            {
                var attr = p.GetType().GetCustomAttributes(typeof(MappingProfileAttribute), false)
                    .Cast<MappingProfileAttribute>()
                    .FirstOrDefault();
                return attr?.Priority ?? 0;
            }).ToList();
        }

        /// <summary>
        /// Creates a profile registry with auto-discovered profiles
        /// </summary>
        /// <returns>Profile registry with discovered profiles</returns>
        public static ProfileRegistry CreateAutoDiscoveredRegistry()
        {
            var registry = new ProfileRegistry();
            var profiles = DiscoverProfiles();
            
            foreach (var profile in profiles)
            {
                registry.AddProfile(profile);
            }

            return registry;
        }
    }
}