using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Communication; // uses your IMessage types + Net.SendAsync/ReceiveAsync

namespace Server
{
    public sealed class ChatServer
    {
        private readonly int _port;
        private readonly IQuoteProvider _quotes;

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        // nick -> client ctx
        private readonly ConcurrentDictionary<string, ClientCtx> _clients = new(StringComparer.OrdinalIgnoreCase);

        // room -> set of nicks
        private readonly ConcurrentDictionary<string, HashSet<string>> _rooms =
            new(StringComparer.OrdinalIgnoreCase);

        public ChatServer(int port, IQuoteProvider quotes)
        {
            _port = port;
            _quotes = quotes ?? throw new ArgumentNullException(nameof(quotes));
            _rooms.TryAdd("#general", new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        public async Task RunAsync(CancellationToken externalCt = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            var ct = _cts.Token;

            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            Info($"Server listening on 127.0.0.1:{_port}");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var tcp = await _listener.AcceptTcpClientAsync(ct);
                    _ = HandleClientAsync(tcp, ct);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                try { _listener?.Stop(); } catch { }
            }
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
        }

        // ---------------- client handling ----------------
        private async Task HandleClientAsync(TcpClient tcp, CancellationToken ct)
        {
            string? nick = null;
            ClientCtx? ctx = null;

            try
            {
                using var stream = tcp.GetStream();

                // Expect Hello first
                var hello = await Net.ReceiveAsync(stream, ct);
                if (hello is not Hello h)
                {
                    await Net.SendAsync(stream, new ErrorMessage("PROTO", "Expected Hello first."));
                    return;
                }

                nick = MakeUniqueNick(h.Nick);
                ctx = new ClientCtx(nick, tcp, stream) { CurrentRoom = "#general" };
                _clients.TryAdd(nick, ctx);
                JoinRoomInternal(nick, "#general");

                await Net.SendAsync(stream, new SystemMessage($"Welcome {nick}! Joined #general."));
                await BroadcastToRoomAsync("#general", new SystemMessage($"{nick} joined #general"));
                Info($"Client '{nick}' connected.");

                // receive loop
                while (!ct.IsCancellationRequested)
                {
                    var msg = await Net.ReceiveAsync(stream, ct);
                    if (msg is null) break;

                    switch (msg)
                    {
                        case JoinRoom j:
                            {
                                var newRoom = NormalizeRoom(j.Room);
                                var old = ctx.CurrentRoom ?? "#general";
                                if (!string.Equals(old, newRoom, StringComparison.OrdinalIgnoreCase))
                                {
                                    LeaveRoomInternal(nick, old);
                                    JoinRoomInternal(nick, newRoom);
                                    ctx.CurrentRoom = newRoom;

                                    await Net.SendAsync(stream, new Joined(nick, newRoom));
                                    await BroadcastToRoomAsync(newRoom, new SystemMessage($"{nick} joined {newRoom}"));
                                }
                                break;
                            }

                        case ChatMessage cm:
                            {
                                var room = ctx.CurrentRoom ?? "#general";
                                // rebroadcast to that room
                                await BroadcastToRoomAsync(room, new ChatMessage(cm.From, room, cm.Text, cm.SentUtc));
                                break;
                            }

                        case PrivateMessage pm:
                            {
                                if (_clients.TryGetValue(pm.To, out var target))
                                {
                                    await Net.SendAsync(target.Stream, pm);   // deliver
                                    await Net.SendAsync(ctx.Stream, pm);      // echo to sender
                                }
                                else
                                {
                                    await Net.SendAsync(ctx.Stream,
                                        new ErrorMessage("USER_NOT_FOUND", $"User '{pm.To}' not found."));
                                }
                                break;
                            }

                        case GetQuote g:
                            {
                                try
                                {
                                    var (price, asOf) = await _quotes.GetQuoteAsync(g.Symbol, ct);
                                    var room = ctx.CurrentRoom ?? "#general";
                                    await BroadcastToRoomAsync(room, new SystemMessage($"{nick} requested quote {g.Symbol}"));
                                    await BroadcastToRoomAsync(room, new QuoteResult(g.Symbol, price, asOf));
                                }
                                catch (Exception ex)
                                {
                                    await Net.SendAsync(ctx.Stream, new ErrorMessage("QUOTE_ERR", ex.Message));
                                }
                                break;
                            }

                        case GetMarketOverview m:
                            {
                                try
                                {
                                    var room = ctx.CurrentRoom ?? "#general";
                                    await BroadcastToRoomAsync(room, new SystemMessage($"{nick} requested market overview"));

                                    var items = await _quotes.GetMarketAsync(m.Symbols, ct);

                                    // To keep compatibility with all clients, broadcast a header and individual QuoteResult lines
                                    await BroadcastToRoomAsync(room, new SystemMessage("[MARKET] ----"));
                                    foreach (var it in items)
                                    {
                                        if (it.Price >= 0)
                                            await BroadcastToRoomAsync(room, new QuoteResult(it.Symbol, it.Price, it.AsOfUtc));
                                        else
                                            await BroadcastToRoomAsync(room, new SystemMessage($"[MARKET] {it.Symbol} : N/A"));
                                    }
                                    await BroadcastToRoomAsync(room, new SystemMessage("[MARKET] ----"));
                                }
                                catch (Exception ex)
                                {
                                    await Net.SendAsync(ctx.Stream, new ErrorMessage("MARKET_ERR", ex.Message));
                                }
                                break;
                            }

                        default:
                            await Net.SendAsync(ctx.Stream,
                                new ErrorMessage("UNSUPPORTED", $"Unsupported message {msg.GetType().Name}."));
                            break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error($"Client '{nick ?? "?"}' error: {ex.Message}");
            }
            finally
            {
                if (nick is not null)
                {
                    var left = ctx?.CurrentRoom ?? "#general";
                    LeaveRoomInternal(nick, left);
                    _clients.TryRemove(nick, out _);
                    try { ctx?.Dispose(); } catch { }

                    _ = BroadcastToRoomAsync(left, new SystemMessage($"{nick} left {left}"));
                    Info($"Client '{nick}' disconnected.");
                }
            }
        }

        // ---------------- rooms & broadcast ----------------
        private static string NormalizeRoom(string room)
        {
            if (string.IsNullOrWhiteSpace(room)) return "#general";
            room = room.Trim();
            return room.StartsWith("#") ? room : "#" + room;
        }

        private string MakeUniqueNick(string requested)
        {
            var baseNick = string.IsNullOrWhiteSpace(requested) ? "user" : requested.Trim();
            var nick = baseNick;
            var i = 1;

            while (_clients.ContainsKey(nick))
            {
                nick = baseNick + i.ToString();
                i++;
            }
            return nick;
        }

        private void JoinRoomInternal(string nick, string room)
        {
            room = NormalizeRoom(room);
            var set = _rooms.GetOrAdd(room, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            lock (set) set.Add(nick);
        }

        private void LeaveRoomInternal(string nick, string room)
        {
            room = NormalizeRoom(room);
            if (_rooms.TryGetValue(room, out var set))
            {
                lock (set) set.Remove(nick);
            }
        }

        private async Task BroadcastToRoomAsync(string room, IMessage message)
        {
            room = NormalizeRoom(room);
            if (!_rooms.TryGetValue(room, out var nicks)) return;

            List<Task> sends = new();
            lock (nicks)
            {
                foreach (var n in nicks)
                {
                    if (_clients.TryGetValue(n, out var c))
                        sends.Add(Net.SendAsync(c.Stream, message));
                }
            }

            try { await Task.WhenAll(sends); } catch { /* best effort */ }
        }

        // ---------------- logging ----------------
        private static void Info(string s) => Console.WriteLine($"[INFO] {s}");
        private static void Error(string s) => Console.WriteLine($"[ERR ] {s}");

        // ---------------- ctx ----------------
        private sealed class ClientCtx : IDisposable
        {
            public string Nick { get; }
            public TcpClient Tcp { get; }
            public NetworkStream Stream { get; }
            public string CurrentRoom { get; set; } = "#general";

            public ClientCtx(string nick, TcpClient tcp, NetworkStream stream)
            {
                Nick = nick;
                Tcp = tcp;
                Stream = stream;
            }

            public void Dispose()
            {
                try { Stream.Dispose(); } catch { }
                try { Tcp.Close(); } catch { }
            }
        }
    }
}
