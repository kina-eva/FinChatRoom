using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    /// A provider that tries Yahoo first, then falls back to Stooq CSV.
    /// Works without API keys. Handles equities (AAPL / AAPL.US) and indices (^SPX, ^DJI, ^NDX).
    public sealed class HybridQuoteProvider : IQuoteProvider
    {
        private readonly HttpClient _http;

        // ✅ Convenience ctor so Program can use `new HybridQuoteProvider()`
        public HybridQuoteProvider()
            : this(new HttpClient(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            }))
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("FinChatRoom/1.0 (+https)");
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        // Explicit DI ctor, if you want to pass a shared HttpClient
        public HybridQuoteProvider(HttpClient http) => _http = http;

        public async Task<(decimal price, DateTime asOfUtc)> GetQuoteAsync(string symbol, CancellationToken ct)
        {
            // Try Yahoo first
            try
            {
                var y = await TryYahooAsync(symbol, ct);
                if (y.price >= 0) return y;
            }
            catch { /* fall through to Stooq */ }

            // Then try Stooq CSV
            var s = await TryStooqCsvAsync(symbol, ct);
            if (s.price >= 0) return s;

            throw new InvalidOperationException(
                $"No price for '{symbol}'. Try AAPL, AAPL.US, ^SPX, ^DJI, ^NDX.");
        }

        public async Task<IEnumerable<QuoteItem>> GetMarketAsync(string[] symbols, CancellationToken ct)
        {
            // Default market “basket” if none provided
            var list = (symbols is { Length: > 0 }
                        ? symbols
                        : new[] { "^DJI", "^SPX", "^NDX", "AAPL", "MSFT", "GOOGL" });

            // Fetch sequentially to keep it simple and avoid noisy throttling
            var results = new List<QuoteItem>(list.Length);
            foreach (var sym in list)
            {
                try
                {
                    var (price, asOf) = await GetQuoteAsync(sym, ct);
                    results.Add(new QuoteItem(sym, price, asOf));
                }
                catch
                {
                    // include “N/A” as -1 to signal unavailable
                    results.Add(new QuoteItem(sym, -1m, DateTime.UtcNow));
                }
            }
            return results;
        }

        // ---------- Yahoo ----------
        // https://query1.finance.yahoo.com/v7/finance/quote?symbols=AAPL
        private async Task<(decimal price, DateTime asOfUtc)> TryYahooAsync(string sym, CancellationToken ct)
        {
            var url = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={Uri.EscapeDataString(sym)}";
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            using var s = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("quoteResponse", out var q) ||
                !q.TryGetProperty("result", out var arr) || arr.GetArrayLength() == 0)
                throw new InvalidOperationException("Yahoo: no result.");

            var item = arr[0];

            decimal price = -1m;
            if (item.TryGetProperty("regularMarketPrice", out var p) &&
                p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var dp))
            {
                price = dp;
            }
            else if (item.TryGetProperty("postMarketPrice", out var p2) &&
                     p2.ValueKind == JsonValueKind.Number && p2.TryGetDecimal(out var dp2))
            {
                price = dp2;
            }

            if (price < 0) throw new InvalidOperationException("Yahoo: price missing.");

            DateTime asOf = DateTime.UtcNow;
            if (item.TryGetProperty("regularMarketTime", out var ts) && ts.TryGetInt64(out var unix))
                asOf = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

            return (price, asOf);
        }

        // ---------- Stooq CSV ----------
        // https://stooq.com/q/l/?s=aapl.us&f=sd2t2ohlcv&h&e=csv
        private async Task<(decimal price, DateTime asOfUtc)> TryStooqCsvAsync(string raw, CancellationToken ct)
        {
            var s = NormalizeForStooq(raw);
            var url = $"https://stooq.com/q/l/?s={s}&f=sd2t2ohlcv&h&e=csv";

            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            var csv = await resp.Content.ReadAsStringAsync(ct);

            var lines = csv.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return (-1m, DateTime.UtcNow);

            var header = lines[0].Split(',', StringSplitOptions.TrimEntries);
            var row = lines[1].Split(',', StringSplitOptions.TrimEntries);

            int idxClose = Array.FindIndex(header, h => string.Equals(h, "Close", StringComparison.OrdinalIgnoreCase));
            if (idxClose < 0 || idxClose >= row.Length) return (-1m, DateTime.UtcNow);

            var close = row[idxClose].Trim('"');
            if (string.IsNullOrWhiteSpace(close) || close.Equals("N/D", StringComparison.OrdinalIgnoreCase))
                return (-1m, DateTime.UtcNow);

            if (!decimal.TryParse(close, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                return (-1m, DateTime.UtcNow);

            return (price, DateTime.UtcNow);
        }

        private static string NormalizeForStooq(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var s = input.Trim();
            if (s.StartsWith("^")) return s.ToLowerInvariant();  // index
            if (s.Contains('.')) return s.ToLowerInvariant();     // explicit suffix
            return (s + ".us").ToLowerInvariant();                // default US equity
        }
    }
}
