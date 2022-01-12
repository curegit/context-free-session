using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace ContextFreeSession.Runtime
{
    public static class Threading
    {
        public static S1 Fork<S1, S2>(Action<S2> f) where S1 : Session, IStart where S2 : Session, IStart
        {
            if (f is null) throw new ArgumentNullException(nameof(f));
            var client = (S1)Activator.CreateInstance(typeof(S1), true)!;
            var server = (S2)Activator.CreateInstance(typeof(S2), true)!;
            var channels = CreateChannels(client.Role, server.Role);
            client.Communicator = new ChannelCommunicator(channels[client.Role].readers, channels[client.Role].writers);
            server.Communicator = new ChannelCommunicator(channels[server.Role].readers, channels[server.Role].writers);
            var threadStart = new ThreadStart(() => f(server));
            var thread = new Thread(threadStart);
            thread.Start();
            return client;
        }

        public static S1 Fork<S1, S2, S3>(Action<S2> f1, Action<S3> f2) where S1 : Session, IStart where S2 : Session, IStart where S3 : Session, IStart
        {
            if (f1 is null) throw new ArgumentNullException(nameof(f1));
            if (f2 is null) throw new ArgumentNullException(nameof(f2));
            var client = (S1)Activator.CreateInstance(typeof(S1), true)!;
            var server1 = (S2)Activator.CreateInstance(typeof(S2), true)!;
            var server2 = (S3)Activator.CreateInstance(typeof(S3), true)!;
            var channels = CreateChannels(client.Role, server1.Role, server2.Role);
            client.Communicator = new ChannelCommunicator(channels[client.Role].readers, channels[client.Role].writers);
            server1.Communicator = new ChannelCommunicator(channels[server1.Role].readers, channels[server1.Role].writers);
            server2.Communicator = new ChannelCommunicator(channels[server2.Role].readers, channels[server2.Role].writers);
            var threadStart1 = new ThreadStart(() => f1(server1));
            var threadStart2 = new ThreadStart(() => f2(server2));
            var thread1 = new Thread(threadStart1);
            var thread2 = new Thread(threadStart2);
            thread1.Start();
            thread2.Start();
            return client;
        }

        public static S1 Fork<S1, S2, S3, S4>(Action<S2> f1, Action<S3> f2, Action<S4> f3) where S1 : Session, IStart where S2 : Session, IStart where S3 : Session, IStart where S4 : Session, IStart
        {
            if (f1 is null) throw new ArgumentNullException(nameof(f1));
            if (f2 is null) throw new ArgumentNullException(nameof(f2));
            if (f3 is null) throw new ArgumentNullException(nameof(f3));
            var client = (S1)Activator.CreateInstance(typeof(S1), true)!;
            var server1 = (S2)Activator.CreateInstance(typeof(S2), true)!;
            var server2 = (S3)Activator.CreateInstance(typeof(S3), true)!;
            var server3 = (S3)Activator.CreateInstance(typeof(S4), true)!;
            var channels = CreateChannels(client.Role, server1.Role, server2.Role, server3.Role);
            client.Communicator = new ChannelCommunicator(channels[client.Role].readers, channels[client.Role].writers);
            server1.Communicator = new ChannelCommunicator(channels[server1.Role].readers, channels[server1.Role].writers);
            server2.Communicator = new ChannelCommunicator(channels[server2.Role].readers, channels[server2.Role].writers);
            server3.Communicator = new ChannelCommunicator(channels[server3.Role].readers, channels[server3.Role].writers);
            var threadStart1 = new ThreadStart(() => f1(server1));
            var threadStart2 = new ThreadStart(() => f2(server2));
            var threadStart3 = new ThreadStart(() => f2(server3));
            var thread1 = new Thread(threadStart1);
            var thread2 = new Thread(threadStart2);
            var thread3 = new Thread(threadStart3);
            thread1.Start();
            thread2.Start();
            thread3.Start();
            return client;
        }

        private static AssociationList<string, (AssociationList<string, ChannelReader<object>> readers, AssociationList<string, ChannelWriter<object>> writers)> CreateChannels(params string[] roles)
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
            };
            var channels = new AssociationList<(string from, string to), (ChannelWriter<object> writer, ChannelReader<object> reader)>();
            foreach (var from in roles)
            {
                foreach (var to in roles)
                {
                    if (from != to)
                    {
                        var channel = Channel.CreateUnbounded<object>(options);
                        channels.Add((from, to), (channel.Writer, channel.Reader));
                    }
                }
            }
            var alist = new AssociationList<string, (AssociationList<string, ChannelReader<object>> readers, AssociationList<string, ChannelWriter<object>> writers)>();
            foreach (var role in roles)
            {
                var readers = new AssociationList<string, ChannelReader<object>>();
                var writers = new AssociationList<string, ChannelWriter<object>>();
                foreach (var other in roles.Where(x => x != role))
                {
                    readers.Add(other, channels[(other, role)].reader);
                    writers.Add(other, channels[(role, other)].writer);
                }
                alist.Add(role, (readers, writers));
            }
            return alist;
        }
    }

    internal class ChannelCommunicator : ICommunicator
    {
        private string? lookaheadLabel = null;

        private readonly AssociationList<string, ChannelReader<object>> readers;

        private readonly AssociationList<string, ChannelWriter<object>> writers;

        public ChannelCommunicator(AssociationList<string, ChannelReader<object>> readers, AssociationList<string, ChannelWriter<object>> writers)
        {
            this.readers = readers;
            this.writers = writers;
        }

        public void Send<T>(string to, string label, T value)
        {
            Task.Run(async () => await writers[to].WriteAsync(label)).Wait();
            Task.Run(async () => await writers[to].WriteAsync(value!)).Wait();
        }

        public (string, T) Receive<T>(string from, string label)
        {
            if (lookaheadLabel is null)
            {
                var l = (string)Task.Run(async () => await readers[from].ReadAsync()).Result;
                return (l, (T)Task.Run(async () => await readers[from].ReadAsync()).Result);
            }
            else
            {
                var l = lookaheadLabel;
                lookaheadLabel = null;
                return (l, (T)Task.Run(async () => await readers[from].ReadAsync()).Result);
            }
        }

        public string Branch(string from)
        {
            lookaheadLabel = (string)Task.Run(async () => await readers[from].ReadAsync()).Result;
            return lookaheadLabel;
        }

        public void Close()
        {
            foreach (var (_, writer) in writers)
            {
                writer.Complete();
            }
        }
    }
}
