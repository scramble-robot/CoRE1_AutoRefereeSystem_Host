using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Linq;
using System.Security;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using static CoRE_1_Host.BaseStatusManager;
using static CoRE_1_Host.RobotStatusManager;


namespace CoRE_1_Host
{
    /// <summary>
    /// BaseStatusManager.xaml の相互作用ロジック
    /// </summary>
    public partial class BaseStatusManager : UserControl
    {
        /* BaseStatusの定義 ******************************************************************************************************************************************/
        #region
        public class BaseStatus {
            private readonly BaseStatusManager _baseStatusManager;
            private Master.BaseConnectionEnum _connection;
            private bool _isActive = false;
            private static Master.HPBarColorEnum _occupationLevelBarColor;
            private Master.DamagePanelColorEnum _damagePanelColor;

            private string _teamColor;

            private static Master.OccupiedEnum _occupied = Master.OccupiedEnum.NO;
            private int _occupationLevel = 0;
            private bool _dpLIsInvulnerable = false;
            private bool _dpCIsInvulnerable = false;
            private bool _dpRIsInvulnerable = false;

            private DateTime _dpLInvulnerableStartTime;
            private DateTime _dpCInvulnerableStartTime;
            private DateTime _dpRInvulnerableStartTime;

            private List<string> _log = new List<string>();

            public BaseStatus(BaseStatusManager instance) {
                _baseStatusManager = instance;
            }

            public Master.BaseConnectionEnum Connection {
                get { return _connection; }
                set { _connection = value; }
            }

            public bool IsActive {
                get { return _isActive; }
                set { _isActive = value; }
            }

            public string NodeNo { get; set; }

            public static Master.HPBarColorEnum OccupationLevelBarColor {
                get { return _occupationLevelBarColor; }
                set { _occupationLevelBarColor = value; }
            }

            public Master.DamagePanelColorEnum DamagePanelColor {
                get { return _damagePanelColor; }
                set { _damagePanelColor = value; }
            }

            public string TeamColor {
                get { return _teamColor; }
                set { _teamColor = value; }
            }

            public int OccupationLevel {
                get { return _occupationLevel; }
                set { 
                    _occupationLevel = value;
                    Application.Current.Dispatcher.Invoke(() => {
                        if (_teamColor.Contains("Red"))
                            _baseStatusManager.OccupationLevelBar.Value = 5 + _occupationLevel;
                        else
                            _baseStatusManager.OccupationLevelBar.Value = 5 - _occupationLevel;
                    });
                }
            }

            public static Master.OccupiedEnum Occupied { set; get; } = Master.OccupiedEnum.NO;

            public bool LeftDPInvulnerable {
                get { return _dpLIsInvulnerable; }
                set {  _dpLIsInvulnerable = value; }
            }

            public bool CenterDPInvulnerable {
                get { return _dpCIsInvulnerable; }
                set { _dpCIsInvulnerable = value; }
            }

            public bool RightDPInvulnerable {
                get { return _dpRIsInvulnerable;}
                set { _dpRIsInvulnerable = value; }
            }

            public DateTime LeftDPInvulnerableStartTime {
                get { return _dpLInvulnerableStartTime; }
                set { _dpLInvulnerableStartTime = value; }
            }


            public DateTime CenterDPInvulnerableStartTime {
                get { return _dpCInvulnerableStartTime; }
                set { _dpCInvulnerableStartTime = value;}
            }

            public DateTime RightDPInvulnerableStartTime {
                get { return _dpRInvulnerableStartTime; }
                set { _dpRInvulnerableStartTime = value; }
            }

            // ログ
            public List<string> Log {
                get { return _log; }
                set { _log = value; }
            }
        };
        #endregion


        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty BasePanelLabelProperty = DependencyProperty.Register("BasePanelLabel", typeof(string), typeof(RobotStatusManager), new PropertyMetadata("Base-C"));
        public string BasePanelLabel {
            get { return (string)GetValue(BasePanelLabelProperty); }
            set { SetValue(BasePanelLabelProperty, value); }
        }
        #endregion

        /* インスタンス ****************************************************************************************************************************************/
        private BaseStatus _redBaseStatus;
        private BaseStatus _blueBaseStatus;
        public BaseStatus RedBaseStatus {
            private set { _redBaseStatus = value; }
            get { return _redBaseStatus; }
        }
        public BaseStatus BlueBaseStatus {
            private set { _blueBaseStatus = value; }
            get { return _blueBaseStatus; }
        }


        // タイマー
        private System.Timers.Timer _invulnerableTimer;

        /* 各種通信で使用する変数 ****************************************************************************************************************************************/
        private readonly SerialPort _serialPort;
        private int _isCommunicating = 0; // interlock用
        private string _lastCommTeam = "Blue";
        private List<string> _sendData = new List<string>();

        public Master.CommunicationSeqEnum commSeq = Master.CommunicationSeqEnum.NONE;

        public BaseStatusManager() {
            InitializeComponent();

            #region シリアルポートの設定
            _serialPort = new SerialPort {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                Encoding = System.Text.Encoding.ASCII,
                NewLine = "\r\n",
                ReadTimeout = 3000,
            };

            //StartWatchingReceiveData();
            //COMPortWatcher.Instance.PortsUpdated += UpdateCOMPortsList;
            //UpdateCOMPortsList();
            #endregion

            RedBaseStatus = new BaseStatus(this);
            RedBaseStatus.TeamColor = "Red";
            BlueBaseStatus = new BaseStatus(this);
            BlueBaseStatus.TeamColor = "Blue";

            _invulnerableTimer = new System.Timers.Timer();
            _invulnerableTimer.Interval = 50;
            _invulnerableTimer.Elapsed += OnInvulnerableTimedEvent;

            //Master.Instance.UpdateEvent += UpdateBaseStatus;
            //Master.Instance.GameStartEvent += StopWatchingReceivedData;
            //Master.Instance.GameStartEvent += StartInvulnerableTimer;
            //Master.Instance.ClearDataEvent += Reset;
        }

        /* ロード時のイベント ****************************************************************************************************************************************/
        #region
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            this.IsEnabled = false;
            //BootButton.IsEnabled = false;
            //RedPingButton.IsEnabled = false;
            //BluePingButton.IsEnabled = false;
            //SendButton.IsEnabled = false;
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.IsEnabled) {
                OccupationLevelBar.Opacity = 1;
                RedBaseStatus.NodeNo = Master.Instance.BaseNodeNo[this.Name].Split(" ")[0];
                BlueBaseStatus.NodeNo = Master.Instance.BaseNodeNo[this.Name].Split(" ")[1];

            } else {
                OccupationLevelBar.Opacity = 0.5;
                RedBaseDPPanel.Opacity = 0.5;
                BlueBaseDPPanel.Opacity = 0.5;
            }
        }
        #endregion

        /* イベント ****************************************************************************************************************************************/

        /*private void UpdateBaseStatus() {
            // ホスト基盤と接続していなければスキップ
            if (!_serialPort.IsOpen) return;

            // RedBaseとの通信が接続されていなければスキップ
            if (RedBaseStatus.Connection != Master.BaseConnectionEnum.CONNECTED) return;

            // BlueBaseとの通信が接続されていなければスキップ
            if (BlueBaseStatus.Connection != Master.BaseConnectionEnum.CONNECTED) return;

            if (!Master.Instance.IsUpdatingStatus) return;

            // 1つ前のイベントがクライアント基板からの応答が遅くてまだ終了していない（別スレッドで実行中）場合はスキップ
            if (Interlocked.CompareExchange(ref _isCommunicating, 1, 0) != 0) return;

            try {
                // クライアントに送信する情報
                var baseStatus = RedBaseStatus;
                var baseStatusInv = BlueBaseStatus;
                var nodeNum = RedBaseStatus.NodeNo;
                if (_lastCommTeam == "Red") {
                    baseStatus = BlueBaseStatus;
                    baseStatusInv = RedBaseStatus;
                    nodeNum = BlueBaseStatus.NodeNo;
                }

                int dpColor = (int)baseStatus.DamagePanelColor;
                // デフォルトは占拠レベル0
                int occupationLevelBarColor = (int)Master.HPBarColorEnum.WHITE;
                int occupationLevelPercent = 100;

                if (RedBaseStatus.OccupationLevel > 0) {
                    occupationLevelBarColor = (int)Master.HPBarColorEnum.RED;
                    occupationLevelPercent = RedBaseStatus.OccupationLevel * 20;
                } else if (BlueBaseStatus.OccupationLevel > 0) {
                    occupationLevelBarColor = (int)Master.HPBarColorEnum.BLUE;
                    occupationLevelPercent = BlueBaseStatus.OccupationLevel * 20;
                }

                // 送信データを規定のプロトコルに基づいて作成
                _sendData.Clear();

                // 宛先の機能No (03はClient)
                _sendData.Add("03");

                // [b0:パワーリレー出力,b1:撃破フラグ]
                _sendData.Add(
                    (BitShift(1, 0) | BitShift(0, 1)).ToString("X2")
                );

                // [b0..3:HPバーのカラー,b4..7:ダメージプレートのカラー]
                _sendData.Add(
                    (BitShift(occupationLevelBarColor, 0) | BitShift(dpColor, 4)).ToString("X2")
                 );

                // HP% 0x00 ~ 0x64 (100)
                _sendData.Add(occupationLevelPercent.ToString("X2"));

                // 未使用
                _sendData.Add("00");
                _sendData.Add("00");

                // コンフィグコマンド
                _sendData.Add("00");

                // コンフィグパラメータ
                _sendData.Add("00");

                // データを送信
                _serialPort.DiscardInBuffer();
                string command = $"send {nodeNum} " + String.Join(",", _sendData);
                SendTextToHostPCB(command);

                // クライアント基板からの応答待機
                try {
                    string receivedDataString = ReadSendCommandResponse(command);

                    Dispatcher.Invoke(() => {
                        ReceivedDataTextBox1.AppendText(
                            $"[{baseStatus.TeamColor}][{Master.Instance.CurrentTime.Minutes:00}:{Master.Instance.CurrentTime.Seconds:00}:{Master.Instance.CurrentTime.Milliseconds:000}]\""
                            + receivedDataString + "\r\n");
                        ReceivedDataTextBox1.ScrollToEnd();
                    });

                    if (receivedDataString.Contains("error")) {
                        Thread.Sleep(500);
                    }

                    Debug.WriteLine(receivedDataString);

                    // 受信データの複号
                    // IM920が自動で付与するヘッダを除去
                    receivedDataString = receivedDataString.Split(":")[1];

                    // 文字列を,で分割し，それぞれの16進数の文字をint型に変換
                    int[] info = receivedDataString.Split(',').Select(part => Convert.ToInt32(part, 16)).ToArray();

                    bool[] invulnerable = { baseStatus.LeftDPInvulnerable, 
                                            baseStatus.CenterDPInvulnerable, 
                                            baseStatus.RightDPInvulnerable 
                    };

                    // ダメージパネルのヒット情報から計算
                    if (baseStatus.IsActive) {
                        for (int i = 0; i < 3; i++) {
                            if (BitHigh(info[4], i) && !invulnerable[i]) {
                                baseStatus.OccupationLevel++;
                                baseStatusInv.OccupationLevel--;
                                invulnerable[i] = true;
                                // baseStatus.AddLog()
                            }
                        }
                    }

                    if (baseStatus.OccupationLevel >= 5) {
                        baseStatus.OccupationLevel = 5;
                        baseStatusInv.OccupationLevel = -5;
                        
                        if (baseStatus.TeamColor.Contains("Red")) {
                            BaseStatus.Occupied = Master.OccupiedEnum.RED;
                        } else {
                            BaseStatus.Occupied = Master.OccupiedEnum.BLUE;
                        }

                        //baseStatus.AddLog()
                    }

                    if (_lastCommTeam == "Red") _lastCommTeam = "Blue";
                    else _lastCommTeam = "Red";

                } catch (TimeoutException e) {
                    Debug.WriteLine(e.ToString());
                    Dispatcher.Invoke(() => {
                        ReceivedDataTextBox1.AppendText("Response timeout \r\n");
                    });
                    Thread.Sleep(1000);
                } catch (Exception e) {
                    _serialPort.DiscardInBuffer();
                    Debug.WriteLine(e.ToString());
                    Dispatcher.Invoke(() => {
                        ReceivedDataTextBox1.AppendText("Error \r\n");
                        ReceivedDataTextBox1.ScrollToEnd();
                    });
                    Thread.Sleep(100);
                }

            } finally {
                Interlocked.Exchange(ref _isCommunicating, 0);
            }
        }

        private void StartWatchingReceiveData() {
            _serialPort.DataReceived += WatchReceivedData;
        }

        // 試合中以外ではこの関数で常時受信データを監視する
        private void WatchReceivedData(object sender, SerialDataReceivedEventArgs e) {
            string data = _serialPort.ReadExisting();
            Dispatcher.Invoke(() => {
                ReceivedDataTextBox1.AppendText(data);
                ReceivedDataTextBox1.ScrollToEnd();
            });
        }


        private void StopWatchingReceivedData() {
            _serialPort.DataReceived -= WatchReceivedData;
        }

        private void Reset() {
            ReceivedDataTextBox1.Clear();

            _serialPort.Close();
            StartWatchingReceiveData();
            commSeq = Master.CommunicationSeqEnum.NONE;
        }

        private void UpdateCOMPortsList() {
            Dispatcher.Invoke(() => {
                ComPortSelectionComboBox.Items.Clear();
                foreach (string port in COMPortWatcher.Instance.GetAvailablePorts())
                    ComPortSelectionComboBox.Items.Add(port);
            });
        }*/

        /* ボタン等のイベント ****************************************************************************************************************************************/
        public void RedLevelButton_Click(object sender, RoutedEventArgs e) {
            if (this.Name == "BaseL") {
                if (Master.Instance.BaseLOccupationLevel > -5) {
                    Master.Instance.BaseLOccupationLevel--;
                }
                OccupationLevelBar.Value = 5 - Master.Instance.BaseLOccupationLevel;

            } else if (this.Name == "BaseC") {
                if (Master.Instance.BaseCOccupationLevel > -5) {
                    Master.Instance.BaseCOccupationLevel--;
                }
                OccupationLevelBar.Value = 5 - Master.Instance.BaseCOccupationLevel;
            } else {
                if (Master.Instance.BaseROccupationLevel > -5) {
                    Master.Instance.BaseROccupationLevel--;
                }
                OccupationLevelBar.Value = 5 - Master.Instance.BaseROccupationLevel;
            }
        }

        public void NeurtralButton_Click(object sender, RoutedEventArgs e) {
            if (this.Name == "BaseL") {
                Master.Instance.BaseLOccupationLevel = 0;
                OccupationLevelBar.Value = 5;
            } else if (this.Name == "BaseC") {
                Master.Instance.BaseCOccupationLevel = 0;
                OccupationLevelBar.Value = 5;
            } else {
                Master.Instance.BaseROccupationLevel = 0;
                OccupationLevelBar.Value = 5;
            }
        }

        public void BlueLevelButton_Click(object sender, RoutedEventArgs e) {
            if (this.Name == "BaseL") {
                if (Master.Instance.BaseLOccupationLevel < 5) {
                    Master.Instance.BaseLOccupationLevel++;
                }
                OccupationLevelBar.Value = 5 - Master.Instance.BaseLOccupationLevel;
            } else if (this.Name == "BaseC") {
                if (Master.Instance.BaseCOccupationLevel < 5) {
                    Master.Instance.BaseCOccupationLevel++;
                }
                OccupationLevelBar.Value = 5 - Master.Instance.BaseCOccupationLevel;
            } else {
                if (Master.Instance.BaseROccupationLevel < 5) {
                    Master.Instance.BaseROccupationLevel++;
                }
                OccupationLevelBar.Value = 5 - Master.Instance.BaseROccupationLevel;
            }
        }


        /*private void ConnectButton_Click(object sender, RoutedEventArgs e) {
            if (!_serialPort.IsOpen) {
                if (ComPortSelectionComboBox.SelectedItem == null) {
                    MessageBox.Show("You must select COM port", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _serialPort.PortName = ComPortSelectionComboBox.SelectedItem.ToString();
                try {
                    // ホスト基板と接続
                    _serialPort.Open();
                    HostStatusTextBox.Text = "Connected HostPCB";
                    ConnectButton.Content = "Disconn.";
                    RedPingButton.IsEnabled = true;
                    BluePingButton.IsEnabled = true;
                    BootButton.IsEnabled = true;
                    SendButton.IsEnabled = true;
                } catch (Exception ex) {
                    MessageBox.Show($"{this.Name}: Failed to connect to HostPCB\n" +
                        $"\nProbably, selected COM port has already been connnected by another.",
                        "Connection failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                _serialPort.Close();
                ConnectButton.Content = "Connect";
                RedPingButton.IsEnabled = false;
                BluePingButton.IsEnabled = false;
                SendButton.IsEnabled = false;
            }
        }

        private void BootButton_Click(object sender, RoutedEventArgs e) {
            if (BootButton.Content.ToString() == "Boot") {
                StopWatchingReceivedData();
                _serialPort.DiscardInBuffer();
                string command = $"boot {Master.Instance.HostCH[this.Name]} {Master.Instance.BaseNodeNo[this.Name]}";
                SendTextToHostPCB(command);

                try {
                    string data = _serialPort.ReadTo(">");
                    ReceivedDataTextBox1.AppendText(data + ">");

                    RedBaseStatus.Connection = Master.BaseConnectionEnum.CONNECTED;
                    BlueBaseStatus.Connection = Master.BaseConnectionEnum.CONNECTED;

                    BootButton.Content = "Stdn";
                    var converter = new System.Windows.Media.BrushConverter();
                    BootButton.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#FFB7403A");
                    BootButton.BorderBrush = BootButton.Background;

                    RedPingButton.IsEnabled = true;
                    BluePingButton.IsEnabled = true;
                } catch (Exception ex) {
                    ;
                }
                
            } else {
                if (Master.Instance.IsUpdatingStatus) return;

                _serialPort.DiscardInBuffer();
                string command = "shutdown";
                SendTextToHostPCB(command);

                try {
                    string data = _serialPort.ReadTo(">");
                    ReceivedDataTextBox1.AppendText(data + ">");

                    RedBaseStatus.Connection = Master.BaseConnectionEnum.DISCONNECTED;
                    BlueBaseStatus.Connection = Master.BaseConnectionEnum.DISCONNECTED;

                    BootButton.Content = "Boot";
                    var converter = new System.Windows.Media.BrushConverter();
                    BootButton.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#FF44B73A");
                    BootButton.BorderBrush = BootButton.Background;
                } catch (Exception ex) {
                    ;
                }
                
            }
        }

        private void RedPingButton_Click(object sender, RoutedEventArgs e) {
            string command = $"ping {Master.Instance.BaseNodeNo[this.Name].Split(" ")[0]}";
            SendTextToHostPCB(command);
        }

        private void BluePingButton_Click(Object sender, RoutedEventArgs e) {
            string command = $"ping {Master.Instance.BaseNodeNo[this.Name].Split(" ")[1]}";
            SendTextToHostPCB(command);
        }

        private void SendButton_Click(object obj, RoutedEventArgs e) {
            if (!_serialPort.IsOpen) return;
            SendTextToHostPCB(SendDataTextBox.Text);
            SendDataTextBox.Clear();
        }

        private void SendDataTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }*/



        /* 各種使用する関数 ****************************************************************************************************************************************/
        private void SendTextToHostPCB(string text) {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(text + "\r\n");
            foreach (byte b in data) {
                _serialPort.Write(new byte[] { b }, 0, 1);

                // 1文字毎に少しだけスリープしないと，上手く基板側が処理できない
                // これは，基板側に受信バッファがないためである
                Thread.Sleep(3);
            }
        }

        private string ReadSendCommandResponse(string command) {
            // 始めにこちらから送信したcommandがそのままホスト基板から返ってくる
            string data1 = _serialPort.ReadLine();
            if (!data1.Contains(command)) return "send error";

            // 次に所望のデータあるいは[NG]が返ってくる
            string data2 = _serialPort.ReadLine();
            if (data2.Contains("[NG]")) return "comm error";

            // 最後に[NG]ではない場合は[OK]が返ってくる
            string data3 = _serialPort.ReadLine();
            if (data3.Contains("[OK]")) return data2;

            return "receive error";
        }

        private bool BitHigh(int data, int i) {
            return ((data & (0b1 << i)) >> i != 0) ? true : false;
        }

        private int BitShift(bool data, int shift) {
            int b = data ? 1 : 0;
            return b << shift;
        }

        private int BitShift(int data, int shift) {
            return data << shift;
        }

        private void OnInvulnerableTimedEvent(object? sender, ElapsedEventArgs e) {
            Dispatcher.Invoke(() => {
                if (RedBaseStatus.LeftDPInvulnerable) {
                    var timePassed = DateTime.Now - RedBaseStatus.LeftDPInvulnerableStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        RedBaseStatus.LeftDPInvulnerable = false;
                        DamagePanelRL.Opacity = 1.0;
                        InvincibleTimeTextBoxRL.Text = "Active";
                    } else {
                        DamagePanelRL.Opacity = 0.5;
                        InvincibleTimeTextBoxRL.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (RedBaseStatus.CenterDPInvulnerable) {
                    var timePassed = DateTime.Now - RedBaseStatus.CenterDPInvulnerableStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        RedBaseStatus.CenterDPInvulnerable = false;
                        DamagePanelRC.Opacity = 1.0;
                        InvincibleTimeTextBoxRC.Text = "Active";
                    } else {
                        DamagePanelRC.Opacity = 0.5;
                        InvincibleTimeTextBoxRC.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (RedBaseStatus.RightDPInvulnerable) {
                    var timePassed = DateTime.Now - RedBaseStatus.RightDPInvulnerableStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        RedBaseStatus.RightDPInvulnerable = false;
                        DamagePanelRR.Opacity = 1.0;
                        InvincibleTimeTextBoxRR.Text = "Active";
                    } else {
                        DamagePanelRR.Opacity = 0.5;
                        InvincibleTimeTextBoxRR.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (BlueBaseStatus.LeftDPInvulnerable) {
                    var timePassed = DateTime.Now - BlueBaseStatus.LeftDPInvulnerableStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        BlueBaseStatus.LeftDPInvulnerable = false;
                        DamagePanelBL.Opacity = 1.0;
                        InvincibleTimeTextBoxBL.Text = "Active";
                    } else {
                        DamagePanelBL.Opacity = 0.5;
                        InvincibleTimeTextBoxBL.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (BlueBaseStatus.CenterDPInvulnerable) {
                    var timePassed = DateTime.Now - BlueBaseStatus.CenterDPInvulnerableStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        BlueBaseStatus.CenterDPInvulnerable = false;
                        DamagePanelBC.Opacity = 1.0;
                        InvincibleTimeTextBoxBC.Text = "Active";
                    } else {
                        DamagePanelBC.Opacity = 0.5;
                        InvincibleTimeTextBoxBC.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (BlueBaseStatus.RightDPInvulnerable) {
                    var timePassed = DateTime.Now - BlueBaseStatus.RightDPInvulnerableStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        BlueBaseStatus.RightDPInvulnerable = false;
                        DamagePanelBR.Opacity = 1.0;
                        InvincibleTimeTextBoxBR.Text = "Active";
                    } else {
                        DamagePanelBR.Opacity = 0.5;
                        InvincibleTimeTextBoxBR.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }
            });
        }

        private void StartInvulnerableTimer() {
            _invulnerableTimer.Start();
        }
    }
}
