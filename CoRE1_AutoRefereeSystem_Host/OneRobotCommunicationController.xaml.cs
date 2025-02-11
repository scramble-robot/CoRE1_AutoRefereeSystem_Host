using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;


namespace CoRE1_AutoRefereeSystem_Host
{
    /// <summary>
    /// OneRobotCommunicationController.xaml の相互作用ロジック
    /// </summary>
    public partial class OneRobotCommunicationController : UserControl
    {
        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty RobotColorProperty = DependencyProperty.Register("RobotColor", typeof(string), typeof(OneRobotCommunicationController), new PropertyMetadata("#10FF0000"));
        public static readonly DependencyProperty RobotLabelProperty = DependencyProperty.Register("RobotLabel", typeof(string), typeof(RobotStatusManager), new PropertyMetadata("Blue/Red #"));

        public string RobotLabel {
            get { return (string)GetValue(RobotLabelProperty); }
            set { SetValue(RobotLabelProperty, value); }
        }
        public string RobotColor {
            get { return (string)GetValue(RobotColorProperty); }
            set { SetValue(RobotColorProperty, value); }
        }

        #endregion

        /* 各種通信で使用する変数 ****************************************************************************************************************************************/
        private readonly SerialPort _serialPort;
        private int _isCommunicating = 0; // interlock用
        private int _commSkipCount = 0;  // 1台のみ通信するときに2台の方と合わせるための変数
        private List<string> _sendData = new List<string>();
        private string _receivedDataPrev = "";

        private bool _isWatching = false;
        public Master.CommunicationSeqEnum commSeq = Master.CommunicationSeqEnum.NONE;


        public OneRobotCommunicationController()
        {
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
                ReadTimeout = 10000,
            };

            StartWatchingReceiveData();
            COMPortWatcher.Instance.PortsUpdated += UpdateCOMPortsList;
            UpdateCOMPortsList();
            #endregion

            Master.Instance.UpdateEvent += UpdateRobotStatus;
            // Master.Instance.GameStartEvent += StopWatchingReceivedData;
            Master.Instance.GameResetEvent += Reset;
            Master.Instance.ClearDataEvent += Reset;
        }

        /* ロード時のイベント ****************************************************************************************************************************************/
        #region
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            this.IsEnabled = false;

            /*CommEnabledToggleButton1.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            PingButton1.IsEnabled = false;
            BootButton.IsEnabled = false;
            ShutdownButton.IsEnabled = false;
            SendButton.IsEnabled = false;*/
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.IsEnabled) {
                Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;
            } else {
                Robot1.Status.Connection = Master.RobotConnectionEnum.DISABLED;
                CommEnabledToggleButton1.IsEnabled = false;
                ComPortSelectionComboBox.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                PingButton1.IsEnabled = false;
                BootButton.IsEnabled = false;
                ShutdownButton.IsEnabled = false;
                SendButton.IsEnabled = false;
            }
        }
        #endregion

        private void UpdateRobotStatus() {
            // ホスト基板と接続していなければスキップ
            if (!_serialPort.IsOpen) return;

            // Robot1との通信がenabledだが，接続されていなければスキップ
            if (Robot1.Status.Connection != Master.RobotConnectionEnum.DISABLED &&
                Robot1.Status.Connection != Master.RobotConnectionEnum.CONNECTED) return;

            if (!Master.Instance.IsUpdatingStatus) return;

            if (_isWatching) StopWatchingReceivedData();

            // 1つ前のイベントがクライアント基板からの応答が遅くてまだ終了していない（別スレッドで実行中）場合はスキップ
            if (Interlocked.CompareExchange(ref _isCommunicating, 1, 0) != 0) return;

            try {
                // クライアントに送信する情報
                var robot = Robot1;
                var robotStatus = Robot1.Status;
                var robotRecivedTextBox = ReceivedDataTextBox1;

                bool defeatedFlag = robotStatus.DefeatedFlag;
                bool powerRelayOnFlag = robotStatus.PowerOnFlag;
                int hpBarColor = (int)robotStatus.HPBarColor;
                int dpColor = (int)robotStatus.DamagePanelColor;
                int hpPercent = 100 * robotStatus.HP / robotStatus.MaxHP;


                // 送信データを規定のプロトコルに基づいて作成
                _sendData.Clear();

                // 宛先の機能No (03はClient)
                _sendData.Add("03");

                // [b0:パワーリレー出力,b1:撃破フラグ]
                _sendData.Add(
                    (BitShift(powerRelayOnFlag, 0) | BitShift(defeatedFlag, 1)).ToString("X2")
                );

                // [b0..3:HPバーのカラー,b4..7:ダメージプレートのカラー]
                _sendData.Add(
                    (BitShift(hpBarColor, 0) | BitShift(dpColor, 4)).ToString("X2")
                 );

                // HP% 0x00 ~ 0x64 (100)
                _sendData.Add(hpPercent.ToString("X2"));

                // 未使用
                _sendData.Add("00");
                _sendData.Add("00");

                // コンフィグコマンド
                _sendData.Add("00");

                // コンフィグパラメータ
                _sendData.Add("00");

                // データを送信
                _serialPort.DiscardInBuffer();
                Dispatcher.Invoke(() => {
                    LinkTextBox.AppendText($"Requesting {robotStatus.TeamName}\r\n");
                    LinkTextBox.ScrollToEnd();
                });
                string command = $"send {Master.Instance.TeamNodeNo[Robot1.Status.TeamName]} "
                                 + String.Join(",", _sendData);
                SendTextToHostPCB(command);

                // クライアント基板からの応答待機
                try {
                    string receivedDataString = ReadSendCommandResponse(command);
                    Dispatcher.Invoke(() => {
                        robotRecivedTextBox.AppendText(
                            $"[{Master.Instance.CurrentTime.Minutes:00}:{Master.Instance.CurrentTime.Seconds:00}:{Master.Instance.CurrentTime.Milliseconds:000}]\""
                            + receivedDataString + "\r\n");
                        robotRecivedTextBox.ScrollToEnd();
                    });

                    if (receivedDataString.Contains("error")) {
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText("Sleep 100ms... \r\n");
                            LinkTextBox.AppendText("--------- \r\n");
                            LinkTextBox.ScrollToEnd();
                        });
                        Thread.Sleep(100);
                        return;
                    } else {
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText("Response succeeded \r\n");
                            LinkTextBox.AppendText("--------- \r\n");
                            LinkTextBox.ScrollToEnd();
                        });
                    }

                    Debug.WriteLine(receivedDataString);

                    // 受信データの複号
                    // IM920が自動で付与するヘッダを除去
                    receivedDataString = receivedDataString.Split(":")[1];

                    // 文字列を,で分割し，それぞれの16進数の文字をint型に変換
                    int[] info = receivedDataString.Split(',').Select(part => Convert.ToInt32(part, 16)).ToArray();

                    if (Master.Instance.DuringGame && !robotStatus.DefeatedFlag && !robotStatus.InvincibilityFlag) {
                        // ダメージパネルのヒット情報からHPを計算
                        int attackBuff = 1;
                        if (robotStatus.TeamColor.Contains("Red"))
                            attackBuff = Master.Instance.BlueAttackBuff;
                        else
                            attackBuff = Master.Instance.RedAttackBuff;

                        for (int i = 0; i < 4; i++) {
                            if (BitHigh(info[4], i)) {
                                robotStatus.HP -= attackBuff * Master.Instance.HitDamage;
                                robotStatus.DamageTaken += attackBuff * Master.Instance.HitDamage;
                                robotStatus.AddRobotLog($"Hit DP{i}. HP decereased by {attackBuff*Master.Instance.HitDamage}, now at {robotStatus.HP}/{robotStatus.MaxHP}");
                            }
                        }
                    }

                    if (robotStatus.HP <= 0) {
                        robotStatus.DamageTaken -= Math.Abs(robotStatus.HP);
                        robotStatus.HP = 0;
                        if (!robotStatus.DefeatedFlag) {
                            robotStatus.AddRobotLog("Defeated");
                            robotStatus.DefeatedFlag = true;
                            robotStatus.PowerOnFlag = false;
                            robotStatus.DefeatedNum++;
                            if (Master.Instance.GameFormat != Master.GameFormatEnum.PRELIMINALY
                                && !Master.Instance.GameEndFlag) {
                                robot.StartRespawnTimer();
                            }
                        }
                    }

                } catch (TimeoutException e) {
                    Debug.WriteLine(e.ToString());
                    Dispatcher.Invoke(() => {
                        LinkTextBox.AppendText("Response timeout \r\n");
                        LinkTextBox.ScrollToEnd();
                        Thread.Sleep(1000);
                    });
                } catch (Exception e) {
                    _serialPort.DiscardInBuffer();
                    Debug.WriteLine(e.ToString());
                    Dispatcher.Invoke(() => {
                        LinkTextBox.AppendText("Error \r\n");
                        LinkTextBox.ScrollToEnd();
                    });
                    Thread.Sleep(100);
                }
            } finally {
                Interlocked.Exchange(ref _isCommunicating, 0);
            }
        }

        private void StartWatchingReceiveData() {
            _isWatching = true;
            _serialPort.DataReceived += WatchReceivedData;
        }

        private void StopWatchingReceivedData() {
            _isWatching = false;
            _serialPort.DataReceived -= WatchReceivedData;
        }

        // 試合中以外ではこの関数で常時受信データを監視する
        private void WatchReceivedData(object sender, SerialDataReceivedEventArgs e) {
            string data = _serialPort.ReadExisting();
            Dispatcher.Invoke(() => {
                LinkTextBox.AppendText(data);
                LinkTextBox.ScrollToEnd();
            });
        }

        private void Reset() {
            LinkTextBox.Clear();
            ReceivedDataTextBox1.Clear();

            //_serialPort.Close();
            //ConnectButton.Content = "Connect";
            StartWatchingReceiveData();
            commSeq = Master.CommunicationSeqEnum.NONE;
        }

        private void UpdateCOMPortsList() {
            Dispatcher.Invoke(() => {
                ComPortSelectionComboBox.Items.Clear();
                foreach (string port in COMPortWatcher.Instance.GetAvailablePorts())
                    ComPortSelectionComboBox.Items.Add(port);
            });
        }

        private string ReadSendCommandResponse(string command) {
            /*string receivedData = _serialPort.ReadTo(">");
            if (receivedData.Length < 5) return "comm error";

            int colonIdx = receivedData.IndexOf(':');
            if (colonIdx == -1) return $"data error: {receivedData}";

            string data = receivedData.Substring(colonIdx - 10, 23 + 10 + 1);
            Debug.WriteLine(data);
            return data;*/

            // 始めにこちらから送信したcommandがそのままホスト基板から返ってくる
            string data1 = _serialPort.ReadLine();
            if (!data1.Contains(command)) {
                _serialPort.ReadTo(">");
                return "send error";
            }

            // 次に所望のデータあるいは[NG]が返ってくる
            string data2 = _serialPort.ReadLine();
            if (data2.Contains("[NG]")) {
                _serialPort.ReadTo(">");
                return "comm error";
            }

            // 最後に[NG]ではない場合は[OK]が返ってくる
            string data3 = _serialPort.ReadLine();
            if (data3.Contains("[OK]")) {
                _serialPort.ReadTo(">");
                return data2;
            }

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


        private void CommEnabledToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (CommEnabledToggleButton1.IsChecked == true) {
                Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;
                ConnectButton.IsEnabled = true;
                ComPortSelectionComboBox.IsEnabled = true;
            } else {
                Robot1.Status.Connection = Master.RobotConnectionEnum.DISABLED;
                ConnectButton.IsEnabled = false;
                ComPortSelectionComboBox.IsEnabled = false;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) {
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
                    PingButton1.IsEnabled = true;
                    BootButton.IsEnabled = true;
                    ShutdownButton.IsEnabled = false;
                    SendButton.IsEnabled = true;

                } catch (Exception ex) {
                    MessageBox.Show($"{this.Name}: Failed to connect to HostPCB\n" +
                         $"\nProbably, selected COM port has already been connnected by another.",
                         "Connection failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                _serialPort.Close();
                ConnectButton.Content = "Connect";
                ConnectButton.IsEnabled = true;
                PingButton1.IsEnabled = false;
                BootButton.IsEnabled = false;
                ShutdownButton.IsEnabled = false;
                SendButton.IsEnabled = false;
                commSeq = Master.CommunicationSeqEnum.NONE;
            }
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
        }

        private void BootButton_Click(object sender, RoutedEventArgs e) {
            //StopWatchingReceivedData();

            _serialPort.DiscardInBuffer();
            string command = $"boot {Master.Instance.HostCH[this.Name]} {Master.Instance.TeamNodeNo[Robot1.Status.TeamName]}";
            SendTextToHostPCB(command);

            try {
                //string data = _serialPort.ReadTo(">");
                //LinkTextBox.AppendText(data + ">");

                BootButton.IsEnabled = false;
                ShutdownButton.IsEnabled = true;
                Robot1.Status.Connection = Master.RobotConnectionEnum.CONNECTED;
                PingButton1.IsEnabled = true;
                Robot1.RespawnButton.IsEnabled = true;
                Robot1.DefeatButton.IsEnabled = true;
                Robot1.PunishButton.IsEnabled = true;
            } catch (Exception ex) {
                ;
            }
            

        }

        private void ShutdownButton_Click(object sender, RoutedEventArgs e) {
            if (Master.Instance.IsUpdatingStatus) return;

            _serialPort.DiscardInBuffer();
            string command = "shutdown";
            SendTextToHostPCB(command);

            try {
                //string data = _serialPort.ReadTo(">");
                //LinkTextBox.AppendText(data + ">");

                ConnectButton.IsEnabled = true;
                BootButton.IsEnabled = true;
                ShutdownButton.IsEnabled = false;

                Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;
                PingButton1.IsEnabled = false;
                //Robot1.RespawnButton.IsEnabled = false;
                //Robot1.DefeatButton.IsEnabled = false;
                //Robot1.PunishButton.IsEnabled = false;

                StartWatchingReceiveData();
            } catch (Exception ex) {
                ;
            }
        }

        private void PingButton_Click(Object sender, RoutedEventArgs e) {
            if (!_serialPort.IsOpen) return;
            string command = $"ping {Master.Instance.TeamNodeNo[Robot1.Status.TeamName]}";
            SendTextToHostPCB(command);
        }

        private void SendTextToHostPCB(string text) {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(text + "\r\n");
            foreach (byte b in data) {
                _serialPort.Write(new byte[] { b }, 0, 1);

                // 1文字毎に少しだけスリープしないと，上手く基板側が処理できない
                // これは基板側がソフトウェアシリアルだから？
                Thread.Sleep(3);
            }
            Thread.Sleep(10);
        }
    }
}
