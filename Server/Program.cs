using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            int port = 8975;
            if (args.Length > 0 && int.TryParse(args[0], out var p)) port = p;

            // Just works: parameterless ctor configures HttpClient.
            IQuoteProvider provider = new HybridQuoteProvider();

            var server = new ChatServer(port, provider);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Console.WriteLine("Starting server. Press Ctrl+C to stop.");
            try
            {
                await server.RunAsync(cts.Token);
            }
            finally
            {
                server.Stop();
                Console.WriteLine("Server stopped.");
            }
        }
    }
}
