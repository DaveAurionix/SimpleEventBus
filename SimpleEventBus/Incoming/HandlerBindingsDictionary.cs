using System.Collections.Generic;

namespace SimpleEventBus.Incoming
{
    class HandlerBindingsDictionary : Dictionary<string, List<HandlerBinding>>
    {
        public void AddBinding(string mappedMessageTypeName, HandlerBinding handlerBinding)
        {
            if (!ContainsKey(mappedMessageTypeName))
            {
                Add(
                    mappedMessageTypeName,
                    new List<HandlerBinding>
                    {
                        handlerBinding
                    });
                return;
            }

            var list = this[mappedMessageTypeName];
            list.Add(handlerBinding);
        }
    }
}
