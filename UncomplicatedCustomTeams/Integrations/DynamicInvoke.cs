using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Integrations
{
    public static class DynamicInvoke
    {
        private static readonly Dictionary<string, MethodInfo> _methods = [];
        private static readonly Dictionary<string, Type> _types = [];
        private static readonly Dictionary<string, Assembly> _assemblies = [];

        public static MethodInfo GetMethod(string assemblyName, string typeName, string methodName)
        {
            string key = $"{typeName}.{methodName}";

            if (_methods.TryGetValue(key, out MethodInfo method))
                return method;

            if (!_assemblies.TryGetValue(assemblyName, out Assembly assembly))
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains(assemblyName));

                if (assembly != null)
                    _assemblies[assemblyName] = assembly;
            }

            if (assembly == null) return null;

            if (!_types.TryGetValue(typeName, out Type type))
            {
                type = assembly.GetType(typeName);
                if (type != null) _types[typeName] = type;
            }

            if (type == null) return null;

            method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == methodName);

            if (method != null)
                _methods[key] = method;
            else
                LogManager.Warn($"Method '{methodName}' not found in '{typeName}'!");

            return method;
        }

        public static PropertyInfo GetProperty(string assemblyName, string typeName, string propertyName)
        {
            if (!_assemblies.TryGetValue(assemblyName, out Assembly assembly))
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.Contains(assemblyName));
                if (assembly != null) _assemblies[assemblyName] = assembly;
            }

            if (assembly == null) return null;

            Type type = assembly.GetType(typeName);
            return type?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }
    }
}