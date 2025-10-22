using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    /// Quote provider using stooq.com (no API key).
    /// US equities generally require ".US" (AAPL.US). Indices: ^DJI, ^SPX, ^NDX.
    public sealed class StooqQuoteProvider : IQuoteProvider
    {
        private readonly HttpClient _http;
        public StooqQuoteProvider(HttpClient http) => _http = http;

        public async Task<(decimal price, DateTime asOfUtc)> GetAsync(string symbol, CancellationToken ct)
        {
            var norm = Normalize(symbol);

            // 1) Try JSON
            try
            {
                var urlJson = $"https://stooq.com/q/l/?s={norm}&f=sd2t2ohlcv&h&e=json";
                using var resp = await _http.GetAsync(urlJson, ct);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                if (doc.RootElement.TryGetProperty("symbols", out var arr) && arr.GetArrayLength() > 0)
                {
                    var row = arr[0];

                    // close can be "N/D" or missing; parse safely
                    string? closeStr = null;
                    if (row.TryGetProperty("close", out var closeEl))
                        closeStr = closeEl.GetString();

                    if (!string.IsNullOrWhiteSpace(closeStr) &&
                        !closeStr.Equals("N/D", StringComparison.OrdinalIgnoreCase) &&
                        decimal.TryParse(closeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceJson))
                    {
                        return (priceJson, DateTime.UtcNow);
                    }
                }
            }
            catch
            {
                // swallow and try CSV fallback
            }

            // 2) Fallback to CSV (often more reliable)
            var urlCsv = $"https://stooq.com/q/l/?s={norm}&f=sd2t2ohlcv&h&e=csv";
            using (var resp = await _http.GetAsync(urlCsv, ct))
            {
                resp.EnsureSuccessStatusCode();
                var csv = await resp.Content.ReadAsStringAsync(ct);
                // Expected first line is header, second line has values
                // ...,"Close",...
                // ...,"227.01",...
                var lines = csv.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2)
                {
                    var header = lines[0].Split(',', StringSplitOptions.TrimEntries);
                    var row = lines[1].Split(',', StringSplitOptions.TrimEntries);

                    int idxClose = Array.FindIndex(header, h => string.Equals(h, "Close", StringComparison.OrdinalIgnoreCase));
                    if (idxClose >= 0 && idxClose < row.Length)
                    {
                        var close = row[idxClose].Trim('"');
                        if (!string.IsNullOrWhiteSpace(close) &&
                            !close.Equals("N/D", StringComparison.OrdinalIgnoreCase) &&
                            decimal.TryParse(close, NumberStyles.Any, CultureInfo.InvariantCulture, out var priceCsv))
                        {
                            return (priceCsv, DateTime.UtcNow);
                        }
                    }
                }
            }

            throw new InvalidOperationException($"No price available for '{symbol}'. Try using '.US' or an index like ^SPX.");
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var s = input.Trim();

            if (s.StartsWith("^")) return s.ToLowerInvariant();   // index (e.g., ^spx)
            if (s.Contains('.')) return s.ToLowerInvariant();   // already has suffix
            return (s + ".us").ToLowerInvariant();                 // default to US equity
        }
    }
}
