﻿using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Planar.Common
{
    public static class AssemblyLoader
    {
        private static readonly List<string> ExcludedAssemblies = new()
        {
            "Microsoft.Data.SqlClient.dll"
        };

        public static Assembly LoadFromAssemblyPath(string assemblyFullPath, AssemblyLoadContext context = null)
        {
            var fileNameWithOutExtension = Path.GetFileNameWithoutExtension(assemblyFullPath);

            var inCompileLibraries = DependencyContext.Default.CompileLibraries.Any(l => l.Name.Equals(fileNameWithOutExtension, StringComparison.OrdinalIgnoreCase));
            var inRuntimeLibraries = DependencyContext.Default.RuntimeLibraries.Any(l => l.Name.Equals(fileNameWithOutExtension, StringComparison.OrdinalIgnoreCase));

            var assembly = (inCompileLibraries || inRuntimeLibraries)
                ? context.LoadFromAssemblyName(new AssemblyName(fileNameWithOutExtension))
                : LoadAssemblyFile(assemblyFullPath, context);

            if (assembly != null)
            {
                var fileName = Path.GetFileName(assemblyFullPath);
                var directory = Path.GetDirectoryName(assemblyFullPath);
                LoadReferencedAssemblies(assembly, context, fileName, directory);
            }

            return assembly;
        }

        public static AssemblyLoadContext CreateAssemblyLoadContext(string name, bool enableUnload = false)
        {
            var context = new AssemblyLoadContext(name, enableUnload);
            return context;
        }

        private static Assembly LoadAssemblyFile(string filename, AssemblyLoadContext context)
        {
            context ??= AssemblyLoadContext.Default;

            if (IsExcludedAssembly(filename))
            {
                return null;
            }

            using var stream = File.OpenRead(filename);
            var result = context.LoadFromStream(stream);

            //// var result = context.LoadFromAssemblyPath(filename);
            return result;
        }

        private static bool IsExcludedAssembly(string filename)
        {
            var name = new FileInfo(filename).Name;
            var result = ExcludedAssemblies.Any(a => string.Compare(name, a, true) == 0);
            return result;
        }

        private static void LoadReferencedAssemblies(Assembly assembly, AssemblyLoadContext context, string fileName, string directory)
        {
            const string filePattern = "*.dll";
            var filesInDirectory = Directory.GetFiles(directory, filePattern)
                .Where(x => x != fileName)
                .Select(x => new KeyValuePair<string, string>(Path.GetFileNameWithoutExtension(x), x))
                .ToDictionary(k => k.Key, v => v.Value);

            var references = assembly.GetReferencedAssemblies();

            foreach (var reference in references)
            {
                if (filesInDirectory.ContainsKey(reference.Name))
                {
                    var path = filesInDirectory[reference.Name];
                    var loadFileName = Path.GetFileName(path);
                    var loadedAssembly = LoadAssemblyFile(path, context);
                    if (loadedAssembly != null)
                    {
                        LoadReferencedAssemblies(loadedAssembly, context, loadFileName, directory);
                    }
                }
            }
        }
    }
}