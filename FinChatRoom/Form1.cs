using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Communication;

namespace FinChatRoom
{
    public partial class Form1 : Form
    {
        private TcpClient? _tcp;
        private NetworkStream? _ns;
        private CancellationTokenSource? _rxCts;

        // active room for public messages
        private string _currentRoom = "#general";

        public Form1()
        {
            InitializeComponent();

            txtHost.Text = "127.0.0.1";
            txtPort.Text = "8975";
            txtNick.Text = $"gui{Random.Shared.Next(100, 999)}";
            txtRoom.Text = "#trading";
            txtTargetNick.Text = "@nick";
            txtPmBody.Text = "message";
            txtSymbol.Text = "AAPL";

            UpdateUi(false);
        }

        // ----- Connect / Disconnect -----
        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;

                _tcp = new TcpClient();
                await _tcp.ConnectAsync(txtHost.Text.Trim(), int.Parse(txtPort.Text.Trim()));
                _ns = _tcp.GetStream();

                await Net.SendAsync(_ns, new Hello(txtNick.Text.Trim()));

                _rxCts = new CancellationTokenSource();
                _ = ReceiveLoop(_rxCts.Token);

                _currentRoom = "#general";
                UpdateUi(true);
                toolStatus.Text = $"Connected | Room: {_currentRoom}";
                PrintHelp();
                Append("[TIP] Try: /quote AAPL   |   /join #trading   |   /msg @someone hello");
            }
            catch (Exception ex)
            {
                Append("[ERR] " + ex.Message);
                btnConnect.Enabled = true;
            }
        }

        private void btnDisconnect_Click(object? sender, EventArgs e)
        {
            try { _rxCts?.Cancel(); _ns?.Close(); _tcp?.Close(); } catch { }
            _currentRoom = "#general";
            UpdateUi(false);
            Append("[SYS] Disconnected.");
        }

        // ----- Receiver loop -----
        private async Task ReceiveLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _ns is not null)
                {
                    var msg = await Net.ReceiveAsync(_ns, ct);
                    if (msg is null) break;

                    switch (msg)
                    {
                        case SystemMessage sm:
                            Append($"[SYS] {sm.Text}");
                            break;

                        case ErrorMessage em:
                            Append($"[ERR] {em.Code}: {em.Text}");
                            break;

                        case Joined j:
                            {
                                Append($"[SYS] Joined {j.Room} as {j.Nick}");
                                // Only switch my client’s active room if I was the joiner
                                var me = txtNick.Text.Trim();
                                if (string.Equals(j.Nick, me, StringComparison.OrdinalIgnoreCase))
                                {
                                    _currentRoom = j.Room;
                                    if (!IsDisposed)
                                        BeginInvoke(new Action(() =>
                                            toolStatus.Text = $"Connected | Room: {_currentRoom}"));
                                }
                                break;
                            }

                        case ChatMessage cm:
                            Append($"[{cm.Room}] {cm.From}: {cm.Text}");
                            break;

                        case PrivateMessage pm:
                            Append($"[PM {pm.From}] {pm.Text}");
                            break;

                        case QuoteResult qr:
                            Append($"[QUOTE] {qr.Symbol}: {qr.Price} @ {qr.AsOfUtc:HH:mm:ss}Z");
                            break;

                        case MarketOverviewResult mo:
                            Append("[MARKET] ----");
                            foreach (var q in mo.Quotes.OrderBy(q => q.Symbol))
                            {
                                var p = q.Price >= 0 ? q.Price.ToString() : "N/A";
                                Append($"[MARKET] {q.Symbol,-8} {p}");
                            }
                            Append("[MARKET] ----");
                            break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Append("[ERR] " + ex.Message);
            }
            finally
            {
                if (!IsDisposed) BeginInvoke(new Action(() => UpdateUi(false)));
            }
        }

        // ----- Send actions -----
        private async void btnSend_Click(object? sender, EventArgs e)
        {
            if (_ns is null) return;

            var text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                if (text.StartsWith("/quote ", StringComparison.OrdinalIgnoreCase))
                {
                    var sym = text[7..].Trim();
                    if (sym.Length > 0)
                        await Net.SendAsync(_ns, new GetQuote(sym));
                }
                else if (string.Equals(text, "/market", StringComparison.OrdinalIgnoreCase))
                {
                    await Net.SendAsync(_ns, new GetMarketOverview(Array.Empty<string>()));
                }
                else if (text.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = text[5..];
                    var p = rest.IndexOf(' ');
                    if (p > 0)
                    {
                        var to = rest[..p].Trim().TrimStart('@');
                        var body = rest[(p + 1)..].Trim();
                        if (body.Length > 0)
                            await Net.SendAsync(_ns, new PrivateMessage(txtNick.Text.Trim(), to, body, DateTime.UtcNow));
                    }
                }
                else if (text.StartsWith("/join ", StringComparison.OrdinalIgnoreCase))
                {
                    var room = text[6..].Trim();
                    if (!room.StartsWith("#")) room = "#" + room;
                    await Net.SendAsync(_ns, new JoinRoom(room));
                    // Do not set _currentRoom here; wait for server 'Joined'
                }
                else
                {
                    // Default: send to current room
                    await Net.SendAsync(_ns, new ChatMessage(txtNick.Text.Trim(), _currentRoom, text, DateTime.UtcNow));
                }

                txtInput.Clear();
            }
            catch (Exception ex)
            {
                Append("[ERR] " + ex.Message);
            }
        }

        private async void btnJoin_Click(object? sender, EventArgs e)
        {
            if (_ns is null) return;
            var room = txtRoom.Text.Trim();
            if (string.IsNullOrWhiteSpace(room)) return;
            if (!room.StartsWith("#")) room = "#" + room;
            await Net.SendAsync(_ns, new JoinRoom(room));
        }

        private async void btnPm_Click(object? sender, EventArgs e)
        {
            if (_ns is null) return;
            var to = txtTargetNick.Text.Trim().TrimStart('@');
            var body = txtPmBody.Text.Trim();
            if (to.Length == 0 || body.Length == 0) return;

            await Net.SendAsync(_ns, new PrivateMessage(txtNick.Text.Trim(), to, body, DateTime.UtcNow));
            txtPmBody.Clear();
        }

        private async void btnQuote_Click(object? sender, EventArgs e)
        {
            if (_ns is null) return;
            var sym = txtSymbol.Text.Trim();
            if (sym.Length == 0) return;
            await Net.SendAsync(_ns, new GetQuote(sym));
        }

        private async void btnMarket_Click(object? sender, EventArgs e)
        {
            if (_ns is null) return;
            await Net.SendAsync(_ns, new GetMarketOverview(Array.Empty<string>()));
        }

        private void btnHelp_Click(object? sender, EventArgs e) => PrintHelp();

        // ----- UI helpers -----
        private void UpdateUi(bool isConnected)
        {
            txtHost.Enabled = txtPort.Enabled = txtNick.Enabled = !isConnected;
            btnConnect.Enabled = !isConnected;
            btnDisconnect.Enabled = isConnected;
            btnSend.Enabled = isConnected;
            txtInput.Enabled = isConnected;
            pnlActions.Enabled = isConnected;

            toolStatus.Text = isConnected ? $"Connected | Room: {_currentRoom}" : "Disconnected";
        }

        private void PrintHelp()
        {
            Append("Commands:");
            Append("  /join #room           ? join or create a room");
            Append("  /msg @nick <text>     ? private message");
            Append("  /quote SYM            ? single quote  (e.g., AAPL or AAPL.US, ^SPX)");
            Append("  /market               ? market overview (DJI, SPX, NDX + AAPL/MSFT/GOOGL)");
            Append("  <text>                ? chat to current room");
            Append("Hints:");
            Append("  • US stocks often need .US (AAPL.US).");
            Append("  • Indices use ^ prefix: ^DJI  ^SPX  ^NDX.");
            Append("Quick actions are on the right →");
        }

        private void Append(string line)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action<string>(Append), line); return; }
            lstLog.Items.Add(line);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        private void txtInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnSend.PerformClick();
            }
        }
    }
}
