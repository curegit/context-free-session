namespace BitcoinMiner;

using ContextFreeSession.Runtime;

public struct Client : IRole { }

public struct Miner : IRole { }

public struct Reporter : IRole { }

public struct mining : ILabel { }

public struct end : ILabel { }

public struct found : ILabel { }

public struct done : ILabel { }

public struct result : ILabel { }

public class Client_Start : SendSession<Miner, mining, (byte[], System.Numerics.BigInteger), ReceiveSession<Miner, done, ContextFreeSession.Unit, Call<Client_Start, ReceiveSession<Reporter, result, (System.Numerics.BigInteger, bool), Eps>>>, end, ContextFreeSession.Unit, Eps>, IStart { public string Role => "Client"; }

public class Miner_Start : BranchSession<Client, mining, ReceiveSession<Client, mining, (byte[], System.Numerics.BigInteger), SendSession<Reporter, found, (byte[], System.Numerics.BigInteger), SendSession<Client, done, ContextFreeSession.Unit, Call<Miner_Start, Eps>>>>, end, ReceiveSession<Client, end, ContextFreeSession.Unit, SendSession<Reporter, end, ContextFreeSession.Unit, Eps>>>, IStart { public string Role => "Miner"; }

public class Reporter_Start : BranchSession<Miner, found, ReceiveSession<Miner, found, (byte[], System.Numerics.BigInteger), Call<Reporter_Start, SendSession<Client, result, (System.Numerics.BigInteger, bool), Eps>>>, end, ReceiveSession<Miner, end, ContextFreeSession.Unit, Eps>>, IStart { public string Role => "Reporter"; }
