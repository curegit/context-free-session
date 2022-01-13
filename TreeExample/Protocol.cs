using ContextFreeSession.Runtime;

public struct Sender : IRole { }

public struct Logger : IRole { }

public struct Receiver : IRole { }

public struct end : ILabel { }

public struct node : ILabel { }

public struct leaf : ILabel { }

public class Sender_Start : Call<Sender_Tree, SendSession<Logger, end, ContextFreeSession.Unit, Eps>>, IStart { public string Role => "Sender"; }

public class Sender_Tree : SendSession<Receiver, node, ContextFreeSession.Unit, Call<Sender_Tree, Call<Sender_Tree, Eps>>, leaf, int, SendSession<Logger, leaf, int, Eps>>, IStart { public string Role => "Sender"; }

public class Logger_Start : Call<Logger_Tree, ReceiveSession<Sender, end, ContextFreeSession.Unit, Eps>>, IStart { public string Role => "Logger"; }

public class Logger_Tree : ReceiveSession<Sender, leaf, int, Call<Logger_Tree_, Eps>>, IStart { public string Role => "Logger"; }

public class Logger_Tree_ : BranchSession<Sender, leaf, Call<Logger_Tree, Call<Logger_Tree_, Eps>>, end, Eps>, IStart { public string Role => "Logger"; }

public class Receiver_Start : Call<Receiver_Tree, Eps>, IStart { public string Role => "Receiver"; }

public class Receiver_Tree : BranchSession<Sender, node, ReceiveSession<Sender, node, ContextFreeSession.Unit, Call<Receiver_Tree, Call<Receiver_Tree, Eps>>>, leaf, ReceiveSession<Sender, leaf, int, Eps>>, IStart { public string Role => "Receiver"; }
