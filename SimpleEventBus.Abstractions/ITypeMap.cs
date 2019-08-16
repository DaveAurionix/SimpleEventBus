using System;

namespace SimpleEventBus.Abstractions
{
    public interface ITypeMap
    {
        string GetNameForType(Type type);
        Type GetTypeByName(string name);
    }
}
