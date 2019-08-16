namespace SimpleEventBus.Abstractions
{
    public class Header
    {
        public Header(string headerName, string value)
        {
            HeaderName = headerName;
            Value = value;
        }

        public string HeaderName { get; }

        public string Value { get; }
    }
}
