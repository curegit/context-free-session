namespace ContextFreeSession.Runtime
{
    internal interface ICommunicator
    {
        public void Send<T>(string to, string label, T value);

        public (string label, T value) Receive<T>(string from, string label);

        public string Peek(string from);

        public void Close();
    }
}
