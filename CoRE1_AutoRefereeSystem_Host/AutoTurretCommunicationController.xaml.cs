using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Linq;
using System.Diagnostics;

namespace CoRE1_AutoRefereeSystem_Host
{
    /// <summary>
    /// AutoTurretCommunicationController.xaml の相互作用ロジック
    /// </summary>
    public partial class AutoTurretCommunicationController : UserControl
    {
        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty RobotColorProperty = DependencyProperty.Register("AutoTurretColor", typeof(string), typeof(AutoTurretCommunicationController), new PropertyMetadata("#10FF0000"));
        public static readonly DependencyProperty RobotLabelProperty = DependencyProperty.Register("AutoTurretLabel", typeof(string), typeof(RobotStatusManager), new PropertyMetadata("Blue/Red #"));

        public string AutoTurretLabel {
            get { return (string)GetValue(RobotLabelProperty); }
            set { SetValue(RobotLabelProperty, value); }
        }
        public string AutoTurretColor {
            get { return (string)GetValue(RobotColorProperty); }
            set { SetValue(RobotColorProperty, value); }
        }
        #endregion

        /* 各種通信で使用する変数 ****************************************************************************************************************************************/
        // Arduinoサーバー
        private const string ArduinoIP = "192.168.11.200";
        private const int ArduinoPort = 8888;

        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;

        private int _isBusy = 0; // interlock用
        private List<string> _sendData = new List<string>();

        private bool _isWatching = false;

        private int numHardwareReset = 0;
        private int numSoftwareReset = 0;
        private int numTimeout = 0;
        private bool statusChanged = false;
        private bool logClear = false;

        // ARSの更新タイマー
        private System.Timers.Timer _updateTimer;
        private System.Timers.Timer _logClearTimer;


        public AutoTurretCommunicationController() {
            InitializeComponent();

            _tcpClient = new TcpClient();


            // それぞれ個別のタイマーを使用する
            // Master.Instance.UpdateEvent += UpdateRobotStatus;

            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 500;
            _updateTimer.Elapsed += UpdateRobotStatus;
            //_updateTimer.Start();

            _logClearTimer = new System.Timers.Timer();
            _logClearTimer.Interval = 10 * 60 * 1000;
            _logClearTimer.Elapsed += ClearLog;
            _logClearTimer.Start();
        }

        /* ロード時のイベント ****************************************************************************************************************************************/
        #region
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            // this.IsEnabled = false;

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


        private void UpdateRobotStatus(object sender, EventArgs args) {
            // 1つ前のイベントがまだ終了していない（別スレッドで実行中）場合はスキップ
            if (Interlocked.CompareExchange(ref _isBusy, 1, 0) != 0) return;

            if (logClear) {
                Dispatcher.Invoke(() => {
                    HostStatusTextBox.Clear();
                    LinkTextBox.Clear();
                    logClear = false;
                });
            }

            try {
                var robot = Robot1;
                var robotStatus = Robot1.Status;
                var robotRecivedTextBox = ReceivedDataTextBox1;

                bool defeatedFlag = robotStatus.DefeatedFlag;
                bool powerRelayOnFlag = robotStatus.PowerOnFlag;
                int hpBarColor = (int)robotStatus.HPBarColor;
                int dpColor = (int)robotStatus.DamagePanelColor;
                //int hpPercent = 100 * robotStatus.HP / robotStatus.MaxHP;
                int hpPercent = 100;

                // 送信データを規定のプロトコルに基づいて作成
                _sendData.Clear();

                // 宛先の機能No (05はauto turret)
                _sendData.Add("07");

                // [b0: アクティブフラグ, b1: 撃破フラグ]
                _sendData.Add(
                    (BitShift(powerRelayOnFlag, 0) | BitShift(defeatedFlag, 1)).ToString("X2")
                );

                // [b0..3:HPバーのカラー,b4..7:ダメージプレートのカラー]
                _sendData.Add(
                    (BitShift(hpBarColor, 0) | BitShift(dpColor, 4)).ToString("X2")
                 );

                // [b0~b5: DP無敵フラグ] 共通陣地のみで使用
                _sendData.Add(
                    "00"
                );

                // HP% 0x00 ~ 0x64 (100)
                _sendData.Add(hpPercent.ToString("X2"));

                // 未使用
                _sendData.Add("00");

                // コンフィグコマンド
                _sendData.Add("00");

                // コンフィグパラメータ
                _sendData.Add("00");

                try {
                    // データを送信
                    Dispatcher.Invoke(() => {
                        LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Requesting... \r\n");
                        // LinkTextBox.ScrollToEnd();
                    });
                    string command = "send " + Master.Instance.TeamNodeNo[Robot1.Status.TeamName].ToString("D4") + " "
                                     + String.Join(",", _sendData);
                    Debug.WriteLine(command);
                    SendTextToArduino(command);

                    string receivedDataString = ReadSendCommandResponse(command);

                    if (receivedDataString.Contains("error")) {
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss:ff")}] ERR, Sleep 100ms... \r\n");
                            LinkTextBox.AppendText("--------- \r\n");
                            LinkTextBox.ScrollToEnd();
                        });
                        Thread.Sleep(100);
                        return;
                    } else {
                        Dispatcher.Invoke(() => {
                            robotRecivedTextBox.AppendText(
                            $"[{Master.Instance.CurrentTime.Minutes:00}:{Master.Instance.CurrentTime.Seconds:00}:{Master.Instance.CurrentTime.Milliseconds:000}]\""
                            + receivedDataString + "\r\n");
                            robotRecivedTextBox.ScrollToEnd();
                        });

                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Response succeeded \r\n");
                            LinkTextBox.AppendText("--------- \r\n");
                            LinkTextBox.ScrollToEnd();
                        });
                    }

                    // 受信データの複号
                    // 文字列を,で分割し，それぞれの16進数の文字をint型に変換
                    int[] info = receivedDataString.Split(',').Select(part => Convert.ToInt32(part, 16)).ToArray();

                    //if (Master.Instance.DuringGame && !robotStatus.DefeatedFlag && !robotStatus.InvincibilityFlag) {
                    //    // ダメージパネルのヒット情報からHPを計算
                    //    int attackBuff = 1;
                    //    if (robotStatus.TeamColor.Contains("Red"))
                    //        attackBuff = Master.Instance.BlueAttackBuff;
                    //    else
                    //        attackBuff = Master.Instance.RedAttackBuff;

                    //    for (int i = 0; i < 4; i++) {
                    //        if (BitHigh(info[4], i)) {
                    //            robotStatus.HP -= attackBuff * Master.Instance.HitDamage;
                    //            robotStatus.DamageTaken += attackBuff * Master.Instance.HitDamage;
                    //            robotStatus.AddRobotLog($"Hit DP{i}. -{attackBuff * Master.Instance.HitDamage}, now: {robotStatus.HP}/{robotStatus.MaxHP}");
                    //        }
                    //    }
                    //}

                    //if (robotStatus.HP <= 0) {
                    //    robotStatus.DamageTaken -= Math.Abs(robotStatus.HP);
                    //    robotStatus.HP = 0;
                    //    if (!robotStatus.DefeatedFlag) {
                    //        robotStatus.AddRobotLog("Defeated");
                    //        robotStatus.DefeatedFlag = true;
                    //        robotStatus.PowerOnFlag = false;
                    //        robotStatus.DefeatedNum++;
                    //        if (Master.Instance.GameFormat != Master.GameFormatEnum.PRELIMINALY
                    //            && !Master.Instance.GameEndFlag) {
                    //            robot.StartRespawnTimer();
                    //        }
                    //    }
                    //}

                    //if (statusChanged) {
                    //    var converter = new System.Windows.Media.BrushConverter();
                    //    Dispatcher.Invoke(() => {
                    //        HostStatusTextBox.IsEnabled = true;
                    //        HostStatusTextBox.Text = $"ARS: OK,  H/SR: {numHardwareReset}/{numSoftwareReset}";
                    //        HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                    //    });
                    //    statusChanged = false;
                    //}
                    //numTimeout = 0;
                } catch (Exception ex) {
                    ;
                }

            } finally {
                Interlocked.Exchange(ref _isBusy, 0);
            }
        }

        private void ClearLog(object sender, EventArgs args) {
            logClear = true;
        }

        //private void StartWatchingReceiveData() {
        //    _isWatching = true;
        //    _serialPort.DataReceived += WatchReceivedData;
        //}

        //private void StopWatchingReceivedData() {
        //    _isWatching = false;
        //    _serialPort.DataReceived -= WatchReceivedData;
        //}

        //// 試合中以外ではこの関数で常時受信データを監視する
        //private void WatchReceivedData(object sender, SerialDataReceivedEventArgs e) {
        //    string data = _serialPort.ReadExisting();
        //    Dispatcher.Invoke(() => {
        //        LinkTextBox.AppendText(data);
        //        LinkTextBox.ScrollToEnd();
        //    });
        //}


        private string ReadSendCommandResponse(string command) {
            /*string receivedData = _serialPort.ReadTo(">");
            if (receivedData.Length < 5) return "comm error";

            int colonIdx = receivedData.IndexOf(':');
            if (colonIdx == -1) return $"data error: {receivedData}";

            string data = receivedData.Substring(colonIdx - 10, 23 + 10 + 1);
            Debug.WriteLine(data);
            return data;*/

            // 始めにこちらから送信したcommandがそのままホスト基板から返ってくる
            string data1 = ReadLine();
            if (!data1.Contains(command)) {
                ReadTo(">");
                return "send error";
            }

            // 次に所望のデータあるいは[NG]が返ってくる
            string data2 = ReadLine();
            Dispatcher.Invoke(() => {
                LinkTextBox.AppendText($"|--> {data2}\r\n");
            });
            if (data2.Contains("ERR") || data2.Contains("[NG]")) {
                ReadTo(">");
                return "comm error";
            }

            // 最後に[NG]ではない場合は[OK]が返ってくる
            string data3 = ReadLine();
            Dispatcher.Invoke(() => {
                LinkTextBox.AppendText($"|-->{data3}\r\n");
            });
            if (data3.Contains("[OK]")) {
                ReadTo(">");
                return data2;
            }
            return "receive error";
        }

        private string ReadTo(string value) {
            if (_tcpStream == null) return "error";

            StringBuilder sb = new StringBuilder();

            while (true) {
                if (_tcpStream.DataAvailable) {
                    Debug.WriteLine("data available");
                    int b = _tcpStream.ReadByte();
                    string receivedChar = Convert.ToChar(b).ToString();
                    //string receivedChar = System.Text.Encoding.ASCII.GetString(new byte[] { b })
                    //string receivedChar = b.ToString()
                    Debug.WriteLine(receivedChar);
                    sb.Append(receivedChar);
                    if (receivedChar == value) return sb.ToString();
                }
            }
        }

        private string ReadLine() {
            string data1 = ReadTo("\r");
            string data2 = ReadTo("\n");

            return data1.Substring(0, data1.Length - 1); // \r\nは除外
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
            if (!_tcpClient.Connected) {
                try {
                    _tcpClient.Connect(ArduinoIP, ArduinoPort);
                    _tcpStream = _tcpClient.GetStream();

                    // タイムアウトの設定
                    _tcpStream.ReadTimeout = 5000;
                    _tcpStream.WriteTimeout = 5000;

                    HostStatusTextBox.Text = "Arduino server connected";
                    ConnectButton.Content = "Close";
                    PingButton1.IsEnabled = true;
                    BootButton.IsEnabled = true;
                    ShutdownButton.IsEnabled = false;
                    SendButton.IsEnabled = true;

                    _updateTimer.Start();

                } catch (Exception ex) {
                    MessageBox.Show($"{this.Name}: Failed to connect to Arduino\n"// +
                         //$"\nProbably, selected COM port has already been connnected by another.",
                         ,
                         "Connection failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                _updateTimer.Stop();

                _tcpStream.Close();
                _tcpClient.Close();
                ConnectButton.Content = "Connect";
                ConnectButton.IsEnabled = true;
                PingButton1.IsEnabled = false;
                BootButton.IsEnabled = false;
                ShutdownButton.IsEnabled = false;
                SendButton.IsEnabled = false;
            }

        }

        private void SendButton_Click(object obj, RoutedEventArgs e) {
            if (_tcpStream == null) return;
            SendTextToArduino(SendDataTextBox.Text);
            SendDataTextBox.Clear();
        }


        private void SendDataTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }

        private void BootButton_Click(object sender, RoutedEventArgs e) {
            if (_tcpStream == null) return;
        }

        private void ShutdownButton_Click(object sender, RoutedEventArgs e) {
            ;
        }

        private void PingButton_Click(Object sender, RoutedEventArgs e) {
            ;
        }


        private void SendTextToArduino(string text, bool verbose = true) {
            if (_tcpStream == null) return;

            byte[] data = System.Text.Encoding.ASCII.GetBytes(text + "\r\n");
            //foreach (byte b in data) {
            //    _tcpStream.Write(new byte[] { b }, 0, 1);
            //    Thread.Sleep(2);
            //}

            _tcpStream.Write(data, 0, data.Length);

            Debug.WriteLine("sended");

            if (verbose) {
                Dispatcher.Invoke(() => {
                    // LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] {text}\r\n");
                    LinkTextBox.AppendText($"|-${text}\r\n");
                    LinkTextBox.ScrollToEnd();
                });
            }
        }

        private void ComPortSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ;
        }
    }
}
