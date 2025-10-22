using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    // Small POCO for overview results
    public readonly record struct QuoteItem(string Symbol, decimal Price, DateTime AsOfUtc);

    public interface IQuoteProvider
    {
        Task<(decimal price, DateTime asOfUtc)> GetQuoteAsync(string symbol, CancellationToken ct);
        Task<IEnumerable<QuoteItem>> GetMarketAsync(string[] symbols, CancellationToken ct);
    }
}
