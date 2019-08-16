using SimpleEventBus.Abstractions;
using System;
using System.Collections.Concurrent;

namespace SimpleEventBus
{
    public class FullNameTypeMap : ITypeMap
    {
        private static readonly ConcurrentDictionary<string, Type> _cache = new ConcurrentDictionary<string, Type>();

        public static FullNameTypeMap Instance => new FullNameTypeMap();

        public string GetNameForType(Type type)
            => type.FullName;

        public Type GetTypeByName(string name)
            => _cache.GetOrAdd(
                name,
                findTypeWithName =>
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var type = assembly.GetType(findTypeWithName, false);
                        if (type != null)
                        {
                            return type;
                        }
                    }

                    throw new InvalidOperationException(
                        $"Could not find a type with the name \"{findTypeWithName}\" in any of the assemblies loaded into the AppDomain.");
                });
    }
}
