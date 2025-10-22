using System.Drawing;
using System.Windows.Forms;

namespace FinChatRoom
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtNick;
        private TextBox txtHost;
        private TextBox txtPort;
        private Button btnConnect;
        private Button btnDisconnect;

        private ListBox lstLog;
        private TextBox txtInput;
        private Button btnSend;

        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStatus;

        private Panel pnlActions;
        private TextBox txtRoom;
        private Button btnJoin;
        private TextBox txtTargetNick;
        private TextBox txtPmBody;
        private Button btnPm;
        private TextBox txtSymbol;
        private Button btnQuote;
        private Button btnMarket;
        private Button btnHelp;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            txtNick = new TextBox();
            txtHost = new TextBox();
            txtPort = new TextBox();
            btnConnect = new Button();
            btnDisconnect = new Button();
            lstLog = new ListBox();
            txtInput = new TextBox();
            btnSend = new Button();
            statusStrip1 = new StatusStrip();
            toolStatus = new ToolStripStatusLabel();
            pnlActions = new Panel();
            txtRoom = new TextBox();
            btnJoin = new Button();
            txtTargetNick = new TextBox();
            txtPmBody = new TextBox();
            btnPm = new Button();
            txtSymbol = new TextBox();
            btnQuote = new Button();
            btnMarket = new Button();
            btnHelp = new Button();

            SuspendLayout();
            statusStrip1.SuspendLayout();
            pnlActions.SuspendLayout();

            // --- top inputs ---
            txtNick.Location = new Point(17, 17);
            txtNick.Margin = new Padding(4, 5, 4, 5);
            txtNick.Name = "txtNick";
            txtNick.PlaceholderText = "Nickname";
            txtNick.Size = new Size(170, 31);
            txtNick.TabIndex = 9;

            txtHost.Location = new Point(197, 17);
            txtHost.Margin = new Padding(4, 5, 4, 5);
            txtHost.Name = "txtHost";
            txtHost.PlaceholderText = "Host";
            txtHost.Size = new Size(170, 31);
            txtHost.TabIndex = 8;

            txtPort.Location = new Point(377, 17);
            txtPort.Margin = new Padding(4, 5, 4, 5);
            txtPort.Name = "txtPort";
            txtPort.PlaceholderText = "Port";
            txtPort.Size = new Size(84, 31);
            txtPort.TabIndex = 7;

            btnConnect.Location = new Point(514, 15);
            btnConnect.Margin = new Padding(4, 5, 4, 5);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(171, 50);
            btnConnect.TabIndex = 6;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;

            btnDisconnect.Location = new Point(700, 15);
            btnDisconnect.Margin = new Padding(4, 5, 4, 5);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(171, 50);
            btnDisconnect.TabIndex = 5;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;

            // --- log ---
            lstLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstLog.Font = new Font("Consolas", 10F);
            lstLog.ItemHeight = 23;
            lstLog.Location = new Point(17, 70);
            lstLog.Margin = new Padding(4, 5, 4, 5);
            lstLog.Name = "lstLog";
            lstLog.Size = new Size(1327, 510);
            lstLog.TabIndex = 4;

            // --- input ---
            txtInput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtInput.Location = new Point(17, 615);
            txtInput.Margin = new Padding(4, 5, 4, 5);
            txtInput.Name = "txtInput";
            txtInput.PlaceholderText = "Type message here (Enter to send)";
            txtInput.Size = new Size(1215, 31);
            txtInput.TabIndex = 3;
            txtInput.KeyDown += txtInput_KeyDown;

            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSend.Location = new Point(1243, 597);
            btnSend.Margin = new Padding(4, 5, 4, 5);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(171, 50);
            btnSend.TabIndex = 2;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;

            // --- status ---
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStatus });
            statusStrip1.Location = new Point(0, 675);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(1686, 32);
            statusStrip1.TabIndex = 1;

            toolStatus.Name = "toolStatus";
            toolStatus.Size = new Size(119, 25);
            toolStatus.Text = "Disconnected";

            // --- actions panel ---
            pnlActions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlActions.BorderStyle = BorderStyle.FixedSingle;
            pnlActions.Controls.Add(txtRoom);
            pnlActions.Controls.Add(btnJoin);
            pnlActions.Controls.Add(txtTargetNick);
            pnlActions.Controls.Add(txtPmBody);
            pnlActions.Controls.Add(btnPm);
            pnlActions.Controls.Add(txtSymbol);
            pnlActions.Controls.Add(btnQuote);
            pnlActions.Controls.Add(btnMarket);
            pnlActions.Controls.Add(btnHelp);
            pnlActions.Location = new Point(1354, 70);
            pnlActions.Margin = new Padding(4, 5, 4, 5);
            pnlActions.Name = "pnlActions";
            pnlActions.Size = new Size(313, 530);
            pnlActions.TabIndex = 0;

            txtRoom.Location = new Point(14, 17);
            txtRoom.Margin = new Padding(4, 5, 4, 5);
            txtRoom.Name = "txtRoom";
            txtRoom.PlaceholderText = "#room";
            txtRoom.Size = new Size(284, 31);
            txtRoom.TabIndex = 0;

            btnJoin.Location = new Point(14, 65);
            btnJoin.Margin = new Padding(4, 5, 4, 5);
            btnJoin.Name = "btnJoin";
            btnJoin.Size = new Size(286, 47);
            btnJoin.TabIndex = 1;
            btnJoin.Text = "Join Room";
            btnJoin.Click += btnJoin_Click;

            txtTargetNick.Location = new Point(14, 130);
            txtTargetNick.Margin = new Padding(4, 5, 4, 5);
            txtTargetNick.Name = "txtTargetNick";
            txtTargetNick.PlaceholderText = "@nick";
            txtTargetNick.Size = new Size(284, 31);
            txtTargetNick.TabIndex = 2;

            txtPmBody.Location = new Point(14, 178);
            txtPmBody.Margin = new Padding(4, 5, 4, 5);
            txtPmBody.Name = "txtPmBody";
            txtPmBody.PlaceholderText = "message";
            txtPmBody.Size = new Size(284, 31);
            txtPmBody.TabIndex = 3;

            btnPm.Location = new Point(14, 227);
            btnPm.Margin = new Padding(4, 5, 4, 5);
            btnPm.Name = "btnPm";
            btnPm.Size = new Size(286, 47);
            btnPm.TabIndex = 4;
            btnPm.Text = "Send Private Message";
            btnPm.Click += btnPm_Click;

            txtSymbol.Location = new Point(14, 292);
            txtSymbol.Margin = new Padding(4, 5, 4, 5);
            txtSymbol.Name = "txtSymbol";
            txtSymbol.PlaceholderText = "Symbol (e.g., AAPL)";
            txtSymbol.Size = new Size(284, 31);
            txtSymbol.TabIndex = 5;

            btnQuote.Location = new Point(14, 340);
            btnQuote.Margin = new Padding(4, 5, 4, 5);
            btnQuote.Name = "btnQuote";
            btnQuote.Size = new Size(286, 47);
            btnQuote.TabIndex = 6;
            btnQuote.Text = "Get Quote";
            btnQuote.Click += btnQuote_Click;

            btnMarket.Location = new Point(14, 397);
            btnMarket.Margin = new Padding(4, 5, 4, 5);
            btnMarket.Name = "btnMarket";
            btnMarket.Size = new Size(286, 47);
            btnMarket.TabIndex = 7;
            btnMarket.Text = "Market Today";
            btnMarket.Click += btnMarket_Click;

            btnHelp.Location = new Point(14, 453);
            btnHelp.Margin = new Padding(4, 5, 4, 5);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(286, 47);
            btnHelp.TabIndex = 8;
            btnHelp.Text = "Show Commands";
            btnHelp.Click += btnHelp_Click;

            // --- Form layout / controls ---
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1686, 707);
            Controls.Add(pnlActions);
            Controls.Add(statusStrip1);
            Controls.Add(btnSend);
            Controls.Add(txtInput);
            Controls.Add(lstLog);
            Controls.Add(btnDisconnect);
            Controls.Add(btnConnect);
            Controls.Add(txtPort);
            Controls.Add(txtHost);
            Controls.Add(txtNick);
            Margin = new Padding(4, 5, 4, 5);
            MinimumSize = new Size(1276, 546);
            Name = "Form1";
            Text = "FinChatRoom";

            // --- Pink theme---
            this.BackColor = Color.FromArgb(255, 230, 240);  // soft pink
            this.ForeColor = Color.Black;
            this.Font = new Font("Segoe UI", 9F);

            lstLog.BackColor = Color.White;
            lstLog.ForeColor = Color.Black;

            txtInput.BackColor = Color.White;
            txtInput.ForeColor = Color.Black;

            pnlActions.BackColor = Color.FromArgb(255, 220, 235);

            Color pastelPink = Color.FromArgb(255, 170, 200);
            Color pastelRose = Color.FromArgb(255, 130, 180);
            foreach (var b in new[] { btnConnect, btnDisconnect, btnSend, btnJoin, btnPm, btnQuote, btnMarket, btnHelp })
            {
                b.BackColor = pastelPink;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = pastelRose;
                b.ForeColor = Color.Black;
                b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            statusStrip1.BackColor = Color.FromArgb(255, 210, 230);
            toolStatus.ForeColor = Color.Black;
            this.Text = "💬 FinChatRoom 💖";

            // finalize layout
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            pnlActions.ResumeLayout(false);
            pnlActions.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
