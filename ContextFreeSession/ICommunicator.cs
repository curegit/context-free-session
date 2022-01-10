namespace ContextFreeSession.Runtime
{
    internal interface ICommunicator
    {
        public void Send<T>(string to, string label, T value);

        public (string, T) Receive<T>(string from, string label);

        public string Branch(string from);

        public void Close();
    }
}
