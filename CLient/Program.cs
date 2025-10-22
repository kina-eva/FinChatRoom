using System.Threading.Tasks;

namespace Client;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var nick = args.Length > 0 ? args[0] : $"user{Random.Shared.Next(1000, 9999)}";
        var client = new ChatClient("127.0.0.1", 8975, nick);
        await client.RunAsync();
    }
}
