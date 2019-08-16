using System.Reflection;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    static class ObjectExtensions
    {
        public static void SetPrivateProperty(this object instance, string propertyName, object value)
        {
            instance
                .GetType()
                .GetProperty(propertyName, BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(instance, value);
        }
    }
}
