using System;

namespace Communication
{
    public interface IMessage { }

    public abstract record MessageBase(string Type) : IMessage;

    // Session / system
    public record Hello(string Nick) : MessageBase(nameof(Hello));
    public record SystemMessage(string Text) : MessageBase(nameof(SystemMessage));
    public record ErrorMessage(string Code, string Text) : MessageBase(nameof(ErrorMessage));

    // Rooms / chat
    public record JoinRoom(string Room) : MessageBase(nameof(JoinRoom));
    public record LeaveRoom(string Room) : MessageBase(nameof(LeaveRoom));
    public record Joined(string Nick, string Room) : MessageBase(nameof(Joined));
    public record ChatMessage(string From, string Room, string Text, DateTime Utc)
        : MessageBase(nameof(ChatMessage));
    public record PrivateMessage(string From, string To, string Text, DateTime Utc)
        : MessageBase(nameof(PrivateMessage));
    public record RoomList(string[] Rooms) : MessageBase(nameof(RoomList));

    // Finance
    public record GetQuote(string Symbol) : MessageBase(nameof(GetQuote));
    public record QuoteResult(string Symbol, decimal Price, DateTime AsOfUtc)
        : MessageBase(nameof(QuoteResult));

    public record GetMarketOverview(string[] Symbols) : MessageBase(nameof(GetMarketOverview));
    public record MarketOverviewResult(QuoteResult[] Quotes) : MessageBase(nameof(MarketOverviewResult));
}
