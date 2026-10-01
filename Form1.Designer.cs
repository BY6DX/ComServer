namespace ComServer;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _connectionGroup = new GroupBox();
        _connectionPanel = new TableLayoutPanel();
        _portLabel = new Label();
        _portCombo = new ComboBox();
        _refreshButton = new Button();
        _baudLabel = new Label();
        _baudCombo = new ComboBox();
        _dataBitsLabel = new Label();
        _dataBitsCombo = new ComboBox();
        _stopBitsLabel = new Label();
        _stopBitsCombo = new ComboBox();
        _parityLabel = new Label();
        _parityCombo = new ComboBox();
        _tcpPortLabel = new Label();
        _tcpPortBox = new NumericUpDown();
        _startButton = new Button();
        _mainLayout = new TableLayoutPanel();
        _mainSplit = new SplitContainer();
        _logPanel = new Panel();
        _logBox = new RichTextBox();
        _logToolbar = new FlowLayoutPanel();
        _displayLabel = new Label();
        _hexCheck = new CheckBox();
        _timestampCheck = new CheckBox();
        _autoScrollCheck = new CheckBox();
        _encodingLabel = new Label();
        _encodingCombo = new ComboBox();
        _clearButton = new Button();
        _saveButton = new Button();
        _sendGroup = new GroupBox();
        _sendLayout = new TableLayoutPanel();
        _sendModeCombo = new ComboBox();
        _sendButton = new Button();
        _sendBox = new TextBox();
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _portStatusLabel = new ToolStripStatusLabel();
        _clientStatusLabel = new ToolStripStatusLabel();
        _rxStatusLabel = new ToolStripStatusLabel();
        _txStatusLabel = new ToolStripStatusLabel();
        _toolTip = new ToolTip(components);

        _connectionGroup.SuspendLayout();
        _connectionPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_tcpPortBox).BeginInit();
        _mainLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_mainSplit).BeginInit();
        _mainSplit.Panel1.SuspendLayout();
        _mainSplit.Panel2.SuspendLayout();
        _mainSplit.SuspendLayout();
        _logPanel.SuspendLayout();
        _logToolbar.SuspendLayout();
        _sendGroup.SuspendLayout();
        _sendLayout.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();

        // _connectionPanel —— 连接参数区。
        // 用 TableLayoutPanel 而不是 FlowLayoutPanel：后者对 NumericUpDown 的高度
        // 计算有偏差，会把端口输入框顶到下一行；表格布局统一垂直居中，稳定可靠。
        _connectionPanel.AutoSize = true;
        _connectionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _connectionPanel.ColumnCount = 14;
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _connectionPanel.Controls.Add(_portLabel, 0, 0);
        _connectionPanel.Controls.Add(_portCombo, 1, 0);
        _connectionPanel.Controls.Add(_refreshButton, 2, 0);
        _connectionPanel.Controls.Add(_baudLabel, 3, 0);
        _connectionPanel.Controls.Add(_baudCombo, 4, 0);
        _connectionPanel.Controls.Add(_dataBitsLabel, 5, 0);
        _connectionPanel.Controls.Add(_dataBitsCombo, 6, 0);
        _connectionPanel.Controls.Add(_stopBitsLabel, 7, 0);
        _connectionPanel.Controls.Add(_stopBitsCombo, 8, 0);
        _connectionPanel.Controls.Add(_parityLabel, 9, 0);
        _connectionPanel.Controls.Add(_parityCombo, 10, 0);
        _connectionPanel.Controls.Add(_tcpPortLabel, 11, 0);
        _connectionPanel.Controls.Add(_tcpPortBox, 12, 0);
        _connectionPanel.Controls.Add(_startButton, 13, 0);
        _connectionPanel.Location = new Point(9, 22);
        _connectionPanel.Name = "_connectionPanel";
        _connectionPanel.RowCount = 1;
        _connectionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _connectionPanel.Size = new Size(1108, 33);
        _connectionPanel.TabIndex = 0;

        _portLabel.Anchor = AnchorStyles.Left;
        _portLabel.AutoSize = true;
        _portLabel.Margin = new Padding(3, 0, 1, 0);
        _portLabel.Name = "_portLabel";
        _portLabel.Size = new Size(35, 17);
        _portLabel.TabIndex = 0;
        _portLabel.Text = "串口";

        _portCombo.Anchor = AnchorStyles.Left;
        _portCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        // 收起时只需显示 COM 号，硬件名在展开的下拉列表里看。
        _portCombo.DropDownWidth = 400;
        _portCombo.Margin = new Padding(3, 4, 3, 4);
        _portCombo.Name = "_portCombo";
        _portCombo.Size = new Size(170, 25);
        _portCombo.TabIndex = 1;

        _refreshButton.Anchor = AnchorStyles.Left;
        _refreshButton.AutoSize = true;
        _refreshButton.Margin = new Padding(3, 4, 8, 4);
        _refreshButton.Name = "_refreshButton";
        _refreshButton.Size = new Size(56, 27);
        _refreshButton.TabIndex = 2;
        _refreshButton.Text = "刷新";
        _refreshButton.UseVisualStyleBackColor = true;

        _baudLabel.Anchor = AnchorStyles.Left;
        _baudLabel.AutoSize = true;
        _baudLabel.Margin = new Padding(3, 0, 1, 0);
        _baudLabel.Name = "_baudLabel";
        _baudLabel.Size = new Size(47, 17);
        _baudLabel.TabIndex = 3;
        _baudLabel.Text = "波特率";

        _baudCombo.Anchor = AnchorStyles.Left;
        _baudCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _baudCombo.Margin = new Padding(3, 4, 8, 4);
        _baudCombo.Name = "_baudCombo";
        _baudCombo.Size = new Size(92, 25);
        _baudCombo.TabIndex = 4;

        _dataBitsLabel.Anchor = AnchorStyles.Left;
        _dataBitsLabel.AutoSize = true;
        _dataBitsLabel.Margin = new Padding(3, 0, 1, 0);
        _dataBitsLabel.Name = "_dataBitsLabel";
        _dataBitsLabel.Size = new Size(47, 17);
        _dataBitsLabel.TabIndex = 5;
        _dataBitsLabel.Text = "数据位";

        _dataBitsCombo.Anchor = AnchorStyles.Left;
        _dataBitsCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _dataBitsCombo.Margin = new Padding(3, 4, 8, 4);
        _dataBitsCombo.Name = "_dataBitsCombo";
        _dataBitsCombo.Size = new Size(52, 25);
        _dataBitsCombo.TabIndex = 6;

        _stopBitsLabel.Anchor = AnchorStyles.Left;
        _stopBitsLabel.AutoSize = true;
        _stopBitsLabel.Margin = new Padding(3, 0, 1, 0);
        _stopBitsLabel.Name = "_stopBitsLabel";
        _stopBitsLabel.Size = new Size(47, 17);
        _stopBitsLabel.TabIndex = 7;
        _stopBitsLabel.Text = "停止位";

        _stopBitsCombo.Anchor = AnchorStyles.Left;
        _stopBitsCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _stopBitsCombo.Margin = new Padding(3, 4, 8, 4);
        _stopBitsCombo.Name = "_stopBitsCombo";
        _stopBitsCombo.Size = new Size(52, 25);
        _stopBitsCombo.TabIndex = 8;

        _parityLabel.Anchor = AnchorStyles.Left;
        _parityLabel.AutoSize = true;
        _parityLabel.Margin = new Padding(3, 0, 1, 0);
        _parityLabel.Name = "_parityLabel";
        _parityLabel.Size = new Size(35, 17);
        _parityLabel.TabIndex = 9;
        _parityLabel.Text = "校验";

        _parityCombo.Anchor = AnchorStyles.Left;
        _parityCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _parityCombo.Margin = new Padding(3, 4, 8, 4);
        _parityCombo.Name = "_parityCombo";
        _parityCombo.Size = new Size(96, 25);
        _parityCombo.TabIndex = 10;

        _tcpPortLabel.Anchor = AnchorStyles.Left;
        _tcpPortLabel.AutoSize = true;
        _tcpPortLabel.Margin = new Padding(3, 0, 1, 0);
        _tcpPortLabel.Name = "_tcpPortLabel";
        _tcpPortLabel.Size = new Size(59, 17);
        _tcpPortLabel.TabIndex = 11;
        _tcpPortLabel.Text = "监听端口";

        _tcpPortBox.Anchor = AnchorStyles.Left;
        // 不要强制设高：UpDownBase 在高度小于自然高度时会把内部编辑框向下挤，
        // 导致整个控件比同排其他控件低一截。用自然高度交给表格垂直居中。
        _tcpPortBox.Margin = new Padding(3, 4, 8, 4);
        _tcpPortBox.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _tcpPortBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _tcpPortBox.Name = "_tcpPortBox";
        _tcpPortBox.Size = new Size(76, 25);
        _tcpPortBox.TabIndex = 12;
        _tcpPortBox.TextAlign = HorizontalAlignment.Center;
        _tcpPortBox.Value = new decimal(new int[] { 8888, 0, 0, 0 });

        _startButton.Anchor = AnchorStyles.Left;
        _startButton.AutoSize = false;
        _startButton.Margin = new Padding(3, 4, 3, 4);
        _startButton.Name = "_startButton";
        _startButton.Size = new Size(96, 27);
        _startButton.TabIndex = 13;
        _startButton.Text = "启动";
        _startButton.UseVisualStyleBackColor = true;

        _connectionGroup.AutoSize = true;
        _connectionGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _connectionGroup.Controls.Add(_connectionPanel);
        _connectionGroup.Dock = DockStyle.Top;
        _connectionGroup.Location = new Point(8, 6);
        _connectionGroup.Name = "_connectionGroup";
        _connectionGroup.Padding = new Padding(6, 4, 6, 4);
        _connectionGroup.Size = new Size(1124, 47);
        _connectionGroup.TabIndex = 0;
        _connectionGroup.TabStop = false;
        _connectionGroup.Text = "连接配置";

        // _logBox —— 只读、等宽、不自动换行（一条转发记录固定占一行）。
        _logBox.BackColor = Color.White;
        _logBox.BorderStyle = BorderStyle.FixedSingle;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 9.75F);
        _logBox.ForeColor = Color.FromArgb(30, 30, 30);
        _logBox.HideSelection = false;
        _logBox.Location = new Point(0, 35);
        _logBox.Name = "_logBox";
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = RichTextBoxScrollBars.Both;
        _logBox.Size = new Size(1124, 300);
        _logBox.TabIndex = 1;
        _logBox.Text = "";
        _logBox.WordWrap = false;

        // _logToolbar —— 日志显示选项。
        _logToolbar.AutoSize = true;
        _logToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _logToolbar.Controls.Add(_displayLabel);
        _logToolbar.Controls.Add(_hexCheck);
        _logToolbar.Controls.Add(_timestampCheck);
        _logToolbar.Controls.Add(_autoScrollCheck);
        _logToolbar.Controls.Add(_encodingLabel);
        _logToolbar.Controls.Add(_encodingCombo);
        _logToolbar.Controls.Add(_clearButton);
        _logToolbar.Controls.Add(_saveButton);
        _logToolbar.Dock = DockStyle.Top;
        _logToolbar.Location = new Point(0, 0);
        _logToolbar.Name = "_logToolbar";
        _logToolbar.Padding = new Padding(2, 2, 2, 4);
        _logToolbar.Size = new Size(1004, 35);
        _logToolbar.TabIndex = 0;
        _logToolbar.WrapContents = false;

        _displayLabel.AutoSize = true;
        _displayLabel.Location = new Point(5, 7);
        _displayLabel.Margin = new Padding(3, 5, 1, 0);
        _displayLabel.Name = "_displayLabel";
        _displayLabel.Size = new Size(35, 17);
        _displayLabel.TabIndex = 0;
        _displayLabel.Text = "显示";

        _hexCheck.AutoSize = true;
        _hexCheck.Location = new Point(44, 5);
        _hexCheck.Margin = new Padding(3, 5, 10, 3);
        _hexCheck.Name = "_hexCheck";
        _hexCheck.Size = new Size(48, 21);
        _hexCheck.TabIndex = 1;
        _hexCheck.Text = "HEX";
        _hexCheck.UseVisualStyleBackColor = true;

        _timestampCheck.AutoSize = true;
        _timestampCheck.Checked = true;
        _timestampCheck.CheckState = CheckState.Checked;
        _timestampCheck.Location = new Point(105, 5);
        _timestampCheck.Margin = new Padding(3, 5, 10, 3);
        _timestampCheck.Name = "_timestampCheck";
        _timestampCheck.Size = new Size(74, 21);
        _timestampCheck.TabIndex = 2;
        _timestampCheck.Text = "时间戳";
        _timestampCheck.UseVisualStyleBackColor = true;

        _autoScrollCheck.AutoSize = true;
        _autoScrollCheck.Checked = true;
        _autoScrollCheck.CheckState = CheckState.Checked;
        _autoScrollCheck.Location = new Point(192, 5);
        _autoScrollCheck.Margin = new Padding(3, 5, 16, 3);
        _autoScrollCheck.Name = "_autoScrollCheck";
        _autoScrollCheck.Size = new Size(86, 21);
        _autoScrollCheck.TabIndex = 3;
        _autoScrollCheck.Text = "自动滚动";
        _autoScrollCheck.UseVisualStyleBackColor = true;

        _encodingLabel.AutoSize = true;
        _encodingLabel.Location = new Point(297, 7);
        _encodingLabel.Margin = new Padding(3, 5, 1, 0);
        _encodingLabel.Name = "_encodingLabel";
        _encodingLabel.Size = new Size(35, 17);
        _encodingLabel.TabIndex = 4;
        _encodingLabel.Text = "编码";

        _encodingCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _encodingCombo.Location = new Point(336, 4);
        _encodingCombo.Margin = new Padding(3, 4, 16, 4);
        _encodingCombo.Name = "_encodingCombo";
        _encodingCombo.Size = new Size(112, 25);
        _encodingCombo.TabIndex = 5;

        _clearButton.AutoSize = true;
        _clearButton.Location = new Point(467, 3);
        _clearButton.Margin = new Padding(3, 3, 6, 3);
        _clearButton.Name = "_clearButton";
        _clearButton.Size = new Size(56, 27);
        _clearButton.TabIndex = 6;
        _clearButton.Text = "清空";
        _clearButton.UseVisualStyleBackColor = true;

        _saveButton.AutoSize = true;
        _saveButton.Location = new Point(532, 3);
        _saveButton.Margin = new Padding(3, 3, 3, 3);
        _saveButton.Name = "_saveButton";
        _saveButton.Size = new Size(80, 27);
        _saveButton.TabIndex = 7;
        _saveButton.Text = "保存日志…";
        _saveButton.UseVisualStyleBackColor = true;

        _logPanel.Controls.Add(_logBox);
        _logPanel.Controls.Add(_logToolbar);
        _logPanel.Dock = DockStyle.Fill;
        _logPanel.Location = new Point(0, 0);
        _logPanel.Name = "_logPanel";
        _logPanel.Size = new Size(1004, 335);
        _logPanel.TabIndex = 0;

        // _sendBox —— 手动发送输入区。
        _sendBox.AcceptsReturn = true;
        _sendBox.Dock = DockStyle.Fill;
        _sendBox.Font = new Font("Consolas", 9.75F);
        _sendBox.Location = new Point(3, 36);
        _sendBox.Multiline = true;
        _sendBox.Name = "_sendBox";
        _sendBox.PlaceholderText = "输入要发送到串口的数据；回车发送，Ctrl/Shift + 回车换行";
        _sendBox.ScrollBars = ScrollBars.Vertical;
        _sendBox.Size = new Size(992, 87);
        _sendBox.TabIndex = 2;

        _sendModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sendModeCombo.Location = new Point(3, 3);
        _sendModeCombo.Margin = new Padding(3, 3, 8, 3);
        _sendModeCombo.Name = "_sendModeCombo";
        _sendModeCombo.Size = new Size(84, 25);
        _sendModeCombo.TabIndex = 0;

        _sendButton.Anchor = AnchorStyles.Right;
        _sendButton.AutoSize = true;
        _sendButton.Location = new Point(928, 4);
        _sendButton.Margin = new Padding(3, 4, 3, 4);
        _sendButton.Name = "_sendButton";
        _sendButton.Size = new Size(64, 27);
        _sendButton.TabIndex = 1;
        _sendButton.Text = "发送";
        _sendButton.UseVisualStyleBackColor = true;

        _sendLayout.ColumnCount = 2;
        _sendLayout.ColumnStyles.Add(new ColumnStyle());
        _sendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _sendLayout.Controls.Add(_sendModeCombo, 0, 0);
        _sendLayout.Controls.Add(_sendButton, 1, 0);
        _sendLayout.Controls.Add(_sendBox, 0, 1);
        _sendLayout.Dock = DockStyle.Fill;
        _sendLayout.Location = new Point(6, 20);
        _sendLayout.Name = "_sendLayout";
        _sendLayout.RowCount = 2;
        _sendLayout.RowStyles.Add(new RowStyle());
        _sendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _sendLayout.SetColumnSpan(_sendBox, 2);
        _sendLayout.Size = new Size(998, 126);
        _sendLayout.TabIndex = 0;

        _sendGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _sendGroup.Controls.Add(_sendLayout);
        _sendGroup.Dock = DockStyle.Fill;
        _sendGroup.Location = new Point(8, 493);
        _sendGroup.Name = "_sendGroup";
        _sendGroup.Padding = new Padding(6, 4, 6, 6);
        _sendGroup.Size = new Size(1010, 152);
        _sendGroup.TabIndex = 2;
        _sendGroup.TabStop = false;
        _sendGroup.Text = "手动发送到串口（调试）";

        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.FixedPanel = FixedPanel.Panel2;
        _mainSplit.IsSplitterFixed = true;
        _mainSplit.Location = new Point(8, 70);
        _mainSplit.Name = "_mainSplit";
        _mainSplit.Orientation = Orientation.Horizontal;
        _mainSplit.Panel1.Controls.Add(_logPanel);
        _mainSplit.Panel1MinSize = 160;
        _mainSplit.Panel2.Controls.Add(_sendGroup);
        _mainSplit.Panel2MinSize = 96;
        _mainSplit.Size = new Size(1124, 417);
        _mainSplit.SplitterDistance = 261;
        _mainSplit.SplitterWidth = 6;
        _mainSplit.TabIndex = 1;

        _mainLayout.ColumnCount = 1;
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mainLayout.Controls.Add(_connectionGroup, 0, 0);
        _mainLayout.Controls.Add(_mainSplit, 0, 1);
        _mainLayout.Dock = DockStyle.Fill;
        _mainLayout.Location = new Point(0, 0);
        _mainLayout.Name = "_mainLayout";
        _mainLayout.Padding = new Padding(8, 6, 8, 6);
        _mainLayout.RowCount = 3;
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        _mainLayout.Size = new Size(1140, 644);
        _mainLayout.TabIndex = 0;

        // _statusStrip —— 运行状态与计数器。
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _portStatusLabel, _clientStatusLabel, _rxStatusLabel, _txStatusLabel });
        _statusStrip.Location = new Point(0, 644);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new Size(1026, 24);
        _statusStrip.SizingGrip = false;
        _statusStrip.TabIndex = 1;

        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(724, 19);
        _statusLabel.Spring = true;
        _statusLabel.Text = "已停止";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _portStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _portStatusLabel.BorderStyle = Border3DStyle.Etched;
        _portStatusLabel.Name = "_portStatusLabel";
        _portStatusLabel.Size = new Size(86, 19);
        _portStatusLabel.Text = "串口：未打开";

        _clientStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _clientStatusLabel.BorderStyle = Border3DStyle.Etched;
        _clientStatusLabel.Name = "_clientStatusLabel";
        _clientStatusLabel.Size = new Size(74, 19);
        _clientStatusLabel.Text = "客户端：0";

        _rxStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _rxStatusLabel.BorderStyle = Border3DStyle.Etched;
        _rxStatusLabel.Name = "_rxStatusLabel";
        _rxStatusLabel.Size = new Size(56, 19);
        _rxStatusLabel.Text = "RX：0 B";

        _txStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _txStatusLabel.BorderStyle = Border3DStyle.Etched;
        _txStatusLabel.Name = "_txStatusLabel";
        _txStatusLabel.Size = new Size(56, 19);
        _txStatusLabel.Text = "TX：0 B";

        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1140, 668);
        Controls.Add(_mainLayout);
        Controls.Add(_statusStrip);
        Font = new Font("Microsoft YaHei UI", 9F);
        MinimumSize = new Size(1100, 560);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "串口服务器 —— 串口 ⇄ TCP 转发";

        _connectionGroup.ResumeLayout(false);
        _connectionGroup.PerformLayout();
        _connectionPanel.ResumeLayout(false);
        _connectionPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_tcpPortBox).EndInit();
        _mainLayout.ResumeLayout(false);
        _mainLayout.PerformLayout();
        _mainSplit.Panel1.ResumeLayout(false);
        _mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_mainSplit).EndInit();
        _mainSplit.ResumeLayout(false);
        _logPanel.ResumeLayout(false);
        _logToolbar.ResumeLayout(false);
        _logToolbar.PerformLayout();
        _sendGroup.ResumeLayout(false);
        _sendLayout.ResumeLayout(false);
        _statusStrip.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private GroupBox _connectionGroup;
    private TableLayoutPanel _connectionPanel;
    private Label _portLabel;
    private ComboBox _portCombo;
    private Button _refreshButton;
    private Label _baudLabel;
    private ComboBox _baudCombo;
    private Label _dataBitsLabel;
    private ComboBox _dataBitsCombo;
    private Label _stopBitsLabel;
    private ComboBox _stopBitsCombo;
    private Label _parityLabel;
    private ComboBox _parityCombo;
    private Label _tcpPortLabel;
    private NumericUpDown _tcpPortBox;
    private Button _startButton;
    private TableLayoutPanel _mainLayout;
    private SplitContainer _mainSplit;
    private Panel _logPanel;
    private RichTextBox _logBox;
    private FlowLayoutPanel _logToolbar;
    private Label _displayLabel;
    private CheckBox _hexCheck;
    private CheckBox _timestampCheck;
    private CheckBox _autoScrollCheck;
    private Label _encodingLabel;
    private ComboBox _encodingCombo;
    private Button _clearButton;
    private Button _saveButton;
    private GroupBox _sendGroup;
    private TableLayoutPanel _sendLayout;
    private ComboBox _sendModeCombo;
    private Button _sendButton;
    private TextBox _sendBox;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;
    private ToolStripStatusLabel _portStatusLabel;
    private ToolStripStatusLabel _clientStatusLabel;
    private ToolStripStatusLabel _rxStatusLabel;
    private ToolStripStatusLabel _txStatusLabel;
    private ToolTip _toolTip;
}
