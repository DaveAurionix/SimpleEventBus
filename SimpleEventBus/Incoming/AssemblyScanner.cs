using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleEventBus.Incoming
{
    static class AssemblyScanner
    {
        public static IEnumerable<Type> GetHandlersInAssembly(Assembly handlersAssembly)
            => handlersAssembly
                .GetTypes()
                .Where(IsHandler);

        private static bool IsHandler(Type type)
            => !type.IsAbstract
                && type.GetInterfaces()
                    .Any(IsHandlerInterface);

        private static bool IsHandlerInterface(Type type)
            => type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IHandles<>)
                && type.IsConstructedGenericType;

        public static IEnumerable<Type> AllMessageTypesHandledBy(Type possibleHandlerType)
            => possibleHandlerType
                .GetInterfaces()
                .Where(IsHandlerInterface)
                .Select(
                    handlesInterface => handlesInterface.GenericTypeArguments[0])
                .Distinct();
    }
}
