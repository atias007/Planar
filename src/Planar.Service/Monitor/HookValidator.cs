﻿using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Hook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace Planar.Service.Monitor
{
    internal sealed class HookValidator
    {
        private const string _monitorHookBaseClassName = "Planar.Hook.BaseHook";
        private const string _monitorHookAssemblyContextName = "Planar.Hook";

        public HookValidator(string filename, ILogger logger)
        {
            var path = new FileInfo(filename).Directory?.FullName;
            if (path == null)
            {
                logger.LogWarning("filename {Filename} could not be found", filename);
                return;
            }

            var folder = Path.GetDirectoryName(filename);
            var files = Directory.GetFiles(path, "*.dll").ToList();
            files.Insert(0, filename);

            var assemblyContext = AssemblyLoader.CreateAssemblyLoadContext($"{_monitorHookAssemblyContextName}.{folder}", enableUnload: true);

            foreach (var f in files)
            {
                var types = GetHookTypesFromFile(assemblyContext, f);
                foreach (var t in types)
                {
                    LoadHook(logger, t);
                    if (IsValid) { return; }
                }
            }
        }

        private void LoadHook(ILogger logger, Type t)
        {
            var hook = Activator.CreateInstance(t);

            if (hook == null)
            {
                logger.LogWarning("fail to load monitor hook with type {Type}. Hook instance is null", t.FullName);
                return;
            }

            SetName(hook);
            SetDescription(hook);

            var handleMethod = hook.GetType().GetMethod(nameof(BaseHook.Handle));
            if (handleMethod == null)
            {
                logger.LogWarning("fail to load monitor hook with type {Type}. It does not have a {MethodName} method", t.FullName, nameof(BaseHook.HandleSystem));
                return;
            }

            var handleSystemMethod = hook.GetType().GetMethod(nameof(BaseHook.HandleSystem));
            if (handleSystemMethod == null)
            {
                logger.LogWarning("fail to load monitor hook with type {Type}. It does not have a {MethodName} method", t.FullName, nameof(BaseHook.HandleSystem));
                return;
            }

            IsValid = true;
        }

        private void SetDescription(object hook)
        {
            var descProp = hook.GetType().GetProperty(nameof(Description));
            if (descProp != null)
            {
                try
                {
                    Description = descProp.GetValue(hook) as string ?? string.Empty;
                }
                catch
                {
                    Description = string.Empty;
                }
            }
        }

        private void SetName(object hook)
        {
            var nameProp = hook.GetType().GetProperty(nameof(Name));
            if (nameProp != null)
            {
                try
                {
                    Name = nameProp.GetValue(hook) as string ?? string.Empty;
                }
                catch
                {
                    Name = string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = hook.GetType().Name;
            }
        }

        private static IEnumerable<Type> GetHookTypesFromFile(AssemblyLoadContext assemblyContext, string file)
        {
            var result = new List<Type>();
            IEnumerable<Type> allTypes = new List<Type>();

            try
            {
                var assembly = AssemblyLoader.LoadFromAssemblyPath(file, assemblyContext);
                if (assembly == null) { return allTypes; }
                allTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract);
            }
            catch
            {
                return result;
            }

            foreach (var t in allTypes)
            {
                try
                {
                    var isHook = IsHookType(t);
                    if (isHook)
                    {
                        result.Add(t);
                    }
                }
                catch
                {
                    // *** DO NOTHING --> SKIP TYPE *** //
                }
            }

            return result;
        }

        private static bool IsHookType(Type t)
        {
            if (t.BaseType == null) { return false; }
            if (t.BaseType.FullName == _monitorHookBaseClassName) { return true; }
            return IsHookType(t.BaseType);
        }

        public bool IsValid { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}