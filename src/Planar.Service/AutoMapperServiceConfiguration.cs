using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Planar.Service
{
    internal static class AutoMapperServiceConfiguration
    {
        public static IServiceCollection AddAutoMapperProfiles([NotNull] this IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) { return services; }

            var profiles = RetrieveProfiles(assemblies);
            if (profiles == null || profiles.Count == 0)
            {
                return services;
            }

            var provider = services.BuildServiceProvider();

            services.AddSingleton<IMapper>(new Mapper(new MapperConfiguration(cfg =>
            {
                foreach (var p in profiles)
                {
                    var instance = ActivatorUtilities.CreateInstance(provider, p) as Profile;
                    cfg.AddProfile(instance);
                }
            })));

            return services;
        }

        /// <summary>
        /// Scan all referenced assemblies to retrieve all `Profile` types.
        /// </summary>
        /// <returns>A collection of <see cref="AutoMapper.Profile"/> types.</returns>
        private static List<Type> RetrieveProfiles(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                return [];
            }

            var loadedProfiles = ExtractProfiles(assemblies);
            return loadedProfiles;
        }

        private static List<Type> ExtractProfiles(IEnumerable<Assembly> assemblies)
        {
            var profiles = new List<Type>();
            foreach (var assembly in assemblies)
            {
                var assemblyProfiles = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Profile)));
                profiles.AddRange(assemblyProfiles);
            }

            return profiles;
        }
    }
}