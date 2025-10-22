using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Communication;

/*

namespace Server
{
    public sealed class ChatServer
    {
        private readonly int _port;
        private readonly IQuoteProvider _quotes;
        private readonly CancellationTokenSource _cts = new();

        // nick -> client context
        private readonly ConcurrentDictionary<string, ClientCtx> _clients = new();
        // room -> set of nicks (represented by dictionary-as-set)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _rooms = new();

        // events (Delegates/Events)
        public event Action<string>? Info;
        public event Action<string>? Error;

        public ChatServer(int port, IQuoteProvider quotes)
        {
            _port = port;
            _quotes = quotes;
        }

        public async Task RunAsync()
        {
            using var listener = new TcpListener(IPAddress.Loopback, _port);
            listener.Start();
            Log($"Server listening on 127.0.0.1:{_port}");

            while (!_cts.IsCancellationRequested)
            {
                TcpClient tcp;
                try
                {
                    tcp = await listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch (OperationCanceledException) { break; }

                _ = HandleClientAsync(tcp); // fire & forget
            }
        }

        public void Stop() => _cts.Cancel();

        // ================= core per-client loop =================
        private async Task HandleClientAsync(TcpClient tcp)
        {
            string? nick = null;

            try
            {
                tcp.NoDelay = true;
                using var ns = tcp.GetStream();

                // First message must be Hello
                if (await Communication.Net.ReceiveAsync(ns) is not Hello hello)
                    throw new InvalidDataException("Expected Hello as first message.");

                nick = MakeUniqueNick(hello.Nick);
                var ctx = new ClientCtx(nick, tcp, ns);
                _clients[nick] = ctx;

                Log($"Client '{nick}' connected.");
                JoinRoomInternal(nick, "#general");

                await Communication.Net.SendAsync(ns, new SystemMessage($"Welcome {nick}! Joined #general."));
                await BroadcastAsync(new SystemMessage($"{nick} joined #general"));

                // Main message loop
                while (tcp.Connected && !_cts.IsCancellationRequested)
                {
                    var msg = await Communication.Net.ReceiveAsync(ns, _cts.Token);
                    if (msg is null) break;

                    switch (msg)
                    {
                        case JoinRoom jr:
                            {
                                var room = NormalizeRoom(jr.Room);
                                JoinRoomInternal(nick, room);
                                await Communication.Net.SendAsync(ns, new Joined(nick, room));
                                break;
                            }

                        case LeaveRoom lr:
                            {
                                var room = NormalizeRoom(lr.Room);
                                LeaveRoomInternal(nick, room);
                                break;
                            }

                        case ChatMessage cm:
                            {
                                var room = NormalizeRoom(cm.Room);
                                await BroadcastToRoomAsync(room,
                                    cm with { From = nick, Room = room, Utc = DateTime.UtcNow });
                                break;
                            }

                        case PrivateMessage pm:
                            {
                                var to = pm.To?.Trim();
                                if (string.IsNullOrWhiteSpace(to) || !_clients.TryGetValue(to, out var toCtx))
                                {
                                    await Communication.Net.SendAsync(ns,
                                        new ErrorMessage("USER_NOT_FOUND", $"User '{to}' not found."));
                                    break;
                                }
                                await Communication.Net.SendAsync(toCtx.Stream,
                                    pm with { From = nick, Utc = DateTime.UtcNow });
                                break;
                            }

                        case GetQuote gq:
                            {
                                try
                                {
                                    var (price, when) = await _quotes.GetAsync(gq.Symbol, _cts.Token);
                                    await Communication.Net.SendAsync(ns,
                                        new QuoteResult(gq.Symbol.ToUpperInvariant(), price, when));
                                }
                                catch (Exception ex)
                                {
                                    await Communication.Net.SendAsync(ns,
                                        new ErrorMessage("QUOTE_ERR", ex.Message));
                                }
                                break;
                            }

                        case GetMarketOverview mo:
                            {
                                var symbols = (mo.Symbols is { Length: > 0 })
                                              ? mo.Symbols
                                              : new[] { "^spx", "^dji", "^ndq", "AAPL", "MSFT", "GOOGL" };

                                var tasks = symbols.Select(async s =>
                                {
                                    try
                                    {
                                        var (p, asOf) = await _quotes.GetAsync(s, _cts.Token);
                                        return new QuoteResult(s.ToUpperInvariant(), p, asOf);
                                    }
                                    catch
                                    {
                                        return new QuoteResult(s.ToUpperInvariant(), -1m, DateTime.UtcNow);
                                    }
                                }).ToArray();

                                var results = await Task.WhenAll(tasks);
                                await Communication.Net.SendAsync(ns, new MarketOverviewResult(results));
                                break;
                            }

                        default:
                            await Communication.Net.SendAsync(ns,
                                new ErrorMessage("UNKNOWN", "Unknown command."));
                            break;
                    }
                }
            }
            catch (OperationCanceledException) { /* server stopping */ // }
     //       catch (IOException) { /* client disconnected */ }
     /*       catch (Exception ex)
            {
                Err($"Client loop error: {ex.Message}");
            }
            finally
            {
                if (nick is not null && _clients.TryRemove(nick, out var ctx))
                {
                    ctx.Dispose();
                    foreach (var kv in _rooms)
                        kv.Value.TryRemove(nick, out _);

                    await BroadcastAsync(new SystemMessage($"{nick} left."));
                    Log($"Client '{nick}' disconnected.");
                }
            }
        }

        // ================= helpers =================
        private static string NormalizeRoom(string room)
        {
            if (string.IsNullOrWhiteSpace(room)) return "#general";
            room = room.Trim();
            return room.StartsWith("#") ? room : "#" + room;
        }

        private void JoinRoomInternal(string nick, string room)
        {
            var set = _rooms.GetOrAdd(room, _ => new ConcurrentDictionary<string, byte>());
            set[nick] = 1;
        }

        private void LeaveRoomInternal(string nick, string room)
        {
            if (_rooms.TryGetValue(room, out var set))
                set.TryRemove(nick, out _);
        }

        private async Task BroadcastAsync(IMessage msg)
        {
            foreach (var kv in _clients)
            {   */
     //           try { await Communication.Net.SendAsync(kv.Value.Stream, msg); }
   //             catch { /* ignore send errors */ }
 //           }
 //       }

/*
        private async Task BroadcastToRoomAsync(string room, IMessage msg)
        {
            if (!_rooms.TryGetValue(room, out var set)) return;
            foreach (var nick in set.Keys)
            {
                if (_clients.TryGetValue(nick, out var ctx))
                {
                    try { await Communication.Net.SendAsync(ctx.Stream, msg); }
                    catch { }
                }
            }
        }

        private string MakeUniqueNick(string desired)
        {
            var baseNick = string.IsNullOrWhiteSpace(desired) ? "user" : desired.Trim();
            var nick = baseNick;
            int i = 1;
            while (_clients.ContainsKey(nick))
                nick = $"{baseNick}{i++}";
            return nick;
        }

        private void Log(string s)
        {
            Info?.Invoke(s);
            try
            {
                File.AppendAllText("server.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {s}{Environment.NewLine}");
            }
            catch { /* ignore IO */ // }
   /*     }

        private void Err(string s)
        {
            Error?.Invoke(s);
            try
            {
                File.AppendAllText("server.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR {s}{Environment.NewLine}");
            }
            catch { }
        }
*/
/*
        private sealed class ClientCtx : IDisposable
        {
            public string Nick { get; }
            public TcpClient Tcp { get; }
            public NetworkStream Stream { get; }
            public ClientCtx(string nick, TcpClient tcp, NetworkStream stream)
            {
                Nick = nick; Tcp = tcp; Stream = stream;
            }
            public void Dispose()
            {
                try { Stream.Close(); } catch { }
                try { Tcp.Close(); } catch { }
            }
        }
    }
}


*/