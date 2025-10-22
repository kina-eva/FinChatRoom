using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Communication
{
    public static class Net
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static async Task SendAsync<T>(NetworkStream stream, T message, CancellationToken ct = default)
            where T : IMessage
        {
            var json = JsonSerializer.Serialize(message!, message!.GetType(), JsonOpts);
            var payload = Encoding.UTF8.GetBytes(json);

            var header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);

            await stream.WriteAsync(header, 0, 4, ct);
            await stream.WriteAsync(payload, 0, payload.Length, ct);
            await stream.FlushAsync(ct);
        }

        public static async Task<IMessage?> ReceiveAsync(NetworkStream stream, CancellationToken ct = default)
        {
            var header = new byte[4];
            await ReadExactAsync(stream, header, ct);
            int len = BinaryPrimitives.ReadInt32BigEndian(header);
            if (len <= 0 || len > 10_000_000) throw new IOException("Invalid frame length.");

            var buf = new byte[len];
            await ReadExactAsync(stream, buf, ct);
            var json = Encoding.UTF8.GetString(buf);

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var typeProp))
                throw new InvalidDataException("Missing 'type' field.");
            var type = typeProp.GetString();

            return type switch
            {
                nameof(Hello) => JsonSerializer.Deserialize<Hello>(json, JsonOpts),
                nameof(SystemMessage) => JsonSerializer.Deserialize<SystemMessage>(json, JsonOpts),
                nameof(ErrorMessage) => JsonSerializer.Deserialize<ErrorMessage>(json, JsonOpts),
                nameof(JoinRoom) => JsonSerializer.Deserialize<JoinRoom>(json, JsonOpts),
                nameof(LeaveRoom) => JsonSerializer.Deserialize<LeaveRoom>(json, JsonOpts),
                nameof(Joined) => JsonSerializer.Deserialize<Joined>(json, JsonOpts),
                nameof(ChatMessage) => JsonSerializer.Deserialize<ChatMessage>(json, JsonOpts),
                nameof(PrivateMessage) => JsonSerializer.Deserialize<PrivateMessage>(json, JsonOpts),
                nameof(RoomList) => JsonSerializer.Deserialize<RoomList>(json, JsonOpts),
                nameof(GetQuote) => JsonSerializer.Deserialize<GetQuote>(json, JsonOpts),
                nameof(QuoteResult) => JsonSerializer.Deserialize<QuoteResult>(json, JsonOpts),
                nameof(GetMarketOverview) => JsonSerializer.Deserialize<GetMarketOverview>(json, JsonOpts),
                nameof(MarketOverviewResult) => JsonSerializer.Deserialize<MarketOverviewResult>(json, JsonOpts),
                _ => throw new NotSupportedException($"Unknown message type '{type}'.")
            };
        }

        private static async Task ReadExactAsync(NetworkStream s, byte[] buffer, CancellationToken ct)
        {
            int read = 0;
            while (read < buffer.Length)
            {
                int n = await s.ReadAsync(buffer, read, buffer.Length - read, ct);
                if (n == 0) throw new IOException("Disconnected.");
                read += n;
            }
        }
    }
}
