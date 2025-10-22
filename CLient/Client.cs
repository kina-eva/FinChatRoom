using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Communication;

namespace Client;

public class ChatClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _nick;

    public ChatClient(string host, int port, string nick)
    {
        _host = host; _port = port; _nick = nick;
    }

    public async Task RunAsync()
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host, _port);
        using var ns = tcp.GetStream();

        await Net.SendAsync(ns, new Hello(_nick));

        Console.WriteLine("Connected. Commands:");
        Console.WriteLine("/join #room | /msg @nick text | /rooms | /quote SYM | <text> (to #general)");

        var cts = new CancellationTokenSource();

        // receive loop
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var msg = await Net.ReceiveAsync(ns, cts.Token);
                    switch (msg)
                    {
                        case ChatMessage cm:
                            Console.WriteLine($"[{cm.Room}] {cm.From}: {cm.Text}");
                            break;
                        case PrivateMessage pm:
                            Console.WriteLine($"[PM from {pm.From}] {pm.Text}");
                            break;
                        case SystemMessage sm:
                            Console.WriteLine($"[SYS] {sm.Text}");
                            break;
                        case QuoteResult qr:
                            Console.WriteLine($"[QUOTE] {qr.Symbol}: {qr.Price} @ {qr.AsOfUtc:HH:mm:ss}Z");
                            break;
                        case ErrorMessage em:
                            Console.WriteLine($"[ERR] {em.Code}: {em.Text}");
                            break;
                        case Joined j:
                            Console.WriteLine($"[SYS] Joined {j.Room} as {j.Nick}");
                            break;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Disconnected: " + ex.Message); }
        });

        // input loop
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null) break;

            if (line.StartsWith("/join "))
            {
                var room = line.Substring(6).Trim();
                await Net.SendAsync(ns, new JoinRoom(room));
            }
            else if (line.StartsWith("/msg "))
            {
                var rest = line.Substring(5);
                var firstSpace = rest.IndexOf(' ');
                if (firstSpace <= 0) continue;
                var to = rest[..firstSpace].TrimStart('@');
                var text = rest[(firstSpace + 1)..];
                await Net.SendAsync(ns, new PrivateMessage(_nick, to, text, DateTime.UtcNow));
            }
            else if (line.StartsWith("/quote "))
            {
                var sym = line.Substring(7).Trim();
                await Net.SendAsync(ns, new GetQuote(sym));
            }
            else if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            else
            {
                await Net.SendAsync(ns, new ChatMessage(_nick, "#general", line, DateTime.UtcNow));
            }
        }

        cts.Cancel();
    }
}
