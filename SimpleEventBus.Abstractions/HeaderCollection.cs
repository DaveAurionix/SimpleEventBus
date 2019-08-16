using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SimpleEventBus.Abstractions
{
    public class HeaderCollection : KeyedCollection<string, Header>
    {
        public HeaderCollection()
            : base(StringComparer.OrdinalIgnoreCase)
        {            
        }

        public HeaderCollection(IEnumerable<Header> headers)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            if (headers != null)
            {
                AddRange(headers);
            }
        }

        public string GetValueOrDefault(string headerName)
        {
            if (Contains(headerName))
            {
                return this[headerName].Value;
            }

            return null;
        }

        public void Add(string headerName, string value)
            => Add(new Header(headerName, value));

        public void AddIfMissing(Header item)
        {
            if (Contains(item.HeaderName))
            {
                return;
            }

            Add(item);
        }

        public void AddOrReplaceWith(Header item)
        {
            if (Contains(item.HeaderName))
            {
                Remove(item.HeaderName);
            }

            Add(item);
        }

        public void AddRange(IEnumerable<Header> headers)
        {
            foreach (var header in headers)
            {
                Add(header);
            }
        }

        protected override string GetKeyForItem(Header item)
            => item.HeaderName;
    }
}
