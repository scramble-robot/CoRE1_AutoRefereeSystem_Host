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
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows.Input;
using System.Net;
using System.Media;


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
        private IPEndPoint? serverIPEndPoint = null;

        private TcpClient? client = null;
        private NetworkStream? stream = null;

        private int _isBusy = 0; // interlock用
        private List<string> _sendData = new List<string>();

        private bool _isWatching = false;
        public Master.ARSSequenceEnum arsSequence = Master.ARSSequenceEnum.NONE;

        private int numSoftwareReset = 0;
        private int numTimeout = 0;
        private bool statusChanged = false;
        private bool logClear = false;

        // ARSの更新タイマー
        private System.Timers.Timer _updateTimer;
        private System.Timers.Timer _logClearTimer;


        public AutoTurretCommunicationController() {
            InitializeComponent();

            // それぞれ個別のタイマーを使用する
            // Master.Instance.UpdateEvent += UpdateRobotStatus;

            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 500;
            _updateTimer.Elapsed += UpdateRobotStatus;
            _updateTimer.Start();

            StartWatchingReceiveData();

            _logClearTimer = new System.Timers.Timer();
            _logClearTimer.Interval = 10 * 60 * 1000;
            _logClearTimer.Elapsed += ClearLog;
            _logClearTimer.Start();
        }

        /* ロード時のイベント ****************************************************************************************************************************************/
        #region
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            Robot1.RobotTypeComboBox.SelectedIndex = 3;

        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.IsEnabled) {
                Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;       
            } else {
                Robot1.Status.Connection = Master.RobotConnectionEnum.DISABLED;
                CommEnabledToggleButton1.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                PingButton1.IsEnabled = false;
                BootButton.IsEnabled = false;
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

            if (arsSequence != Master.ARSSequenceEnum.UPDATING) {
                try {
                    if (arsSequence == Master.ARSSequenceEnum.OPENED) {
                        ;
                    } else if (arsSequence == Master.ARSSequenceEnum.RECONNECTING) {
                        Debug.WriteLine(serverIPEndPoint);
                        client = new TcpClient();
                        client.Connect(serverIPEndPoint);
                        stream = client.GetStream();

                        // タイムアウトの設定
                        stream.ReadTimeout = 2000;
                        stream.WriteTimeout = 2000;

                        string command = "boot autoturret";
                        SendTextToArduino(command);
                        Dispatcher.Invoke(() => {
                            HostStatusTextBox.Text = "Reconnecting succeeded";
                        });

                        arsSequence = Master.ARSSequenceEnum.BOOTING;
                    } else if (arsSequence == Master.ARSSequenceEnum.BOOTING) {
                        string data = ReadTo(">");
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText(data + ">");

                            if (data.Contains("[OK]")) {
                                HostStatusTextBox.Text = "Boot succeeded";
                                HostStatusTextBox.IsEnabled = true;

                                ConnectButton.IsEnabled = false;
                                PingButton1.IsEnabled = false;

                                Robot1.RespawnButton.IsEnabled = true;
                                Robot1.DefeatButton.IsEnabled = true;
                                Robot1.PunishButton.IsEnabled = true;

                                //stream.ReadTimeout = Master.Instance.ARSTimeoutRandom.Next(
                                //    Master.Instance.TimeoutMin, Master.Instance.TimeoutMax
                                //);
                                statusChanged = true;
                                Robot1.Status.Connection = Master.RobotConnectionEnum.CONNECTED;
                                arsSequence = Master.ARSSequenceEnum.UPDATING;
                            } else {
                                HostStatusTextBox.Text = "Boot failed";
                                arsSequence = Master.ARSSequenceEnum.OPENED;
                                StartWatchingReceiveData();
                            }
                        });
                    } else if (arsSequence == Master.ARSSequenceEnum.SHUTING_DOWN) {
                        var converter = new System.Windows.Media.BrushConverter();
                        Dispatcher.Invoke(() => {
                            HostStatusTextBox.Text = "Shutting down";
                            //HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#00FFFFFF");
                        });

                        Thread.Sleep(3000);
                        string command = "shutdown";
                        SendTextToArduino(command);
                        // string data = _serialPort.ReadTo(">");
                        // HACK
                        string data = "OK";
                        if (data.Contains("OK")) {
                            Dispatcher.Invoke(() => {
                                HostStatusTextBox.Text = "ARS shutdown";
                                HostStatusTextBox.IsEnabled = false;

                                LinkTextBox.AppendText(data + ">");
                                LinkTextBox.ScrollToEnd();

                                BootButton.Content = "Boot";
                                ConnectButton.IsEnabled = true;
                                BootButton.IsEnabled = true;
                                PingButton1.IsEnabled = true;
                            });

                            numSoftwareReset = 0;
                            numTimeout = 0;
                            Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;
                            arsSequence = Master.ARSSequenceEnum.OPENED;

                            // shutdownコマンドは時間がかかるので，cpuResetは無し
                            // Thread.Sleep(1000);
                            // command = "cpuReset";
                            // SendTextToHostPCB(command, false);
                            StartWatchingReceiveData();
                        }
                    }
                    //else if (arsSequence == Master.ARSSequenceEnum.SOFTWARE_RESET) {
                    //    ;
                    //}
                } catch (Exception ex) when (ex is IOException || ex is TimeoutException) {
                    statusChanged = true;
                    SystemSounds.Exclamation.Play();

                    var converter = new System.Windows.Media.BrushConverter();
                    Dispatcher.Invoke(() => {
                        LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Disconnected \r\n" +
                            $"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Retry to start ARS\r\n"
                        );
                        LinkTextBox.ScrollToEnd();

                        HostStatusTextBox.Text = "Restarting: Openning HostPCB";
                        HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#66F5E98B");
                    });

                    numSoftwareReset++;
                    if (!client.Connected) client.Close();
                    stream.Close();
                    Thread.Sleep(2000);
                    arsSequence = Master.ARSSequenceEnum.RECONNECTING;
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                    return;
                } finally {
                    Interlocked.Exchange(ref _isBusy, 0);
                }
                return;
            } else { // arsSequence == Master.ARSSequenceEnum.UPDATING
                if (_isWatching) StopWatchingReceivedData();

                try {
                    // Arduinoに送信する情報
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

                    // Arduinoからの応答待機
                    try {
                        // データを送信
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Requesting... \r\n");
                            // LinkTextBox.ScrollToEnd();
                        });
                        string command = "send " + Master.Instance.TeamNodeNo[robotStatus.TeamName].ToString("D4") + " "
                                         + String.Join(",", _sendData);
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

                        if (Master.Instance.DuringGame && !robotStatus.DefeatedFlag && !robotStatus.InvincibilityFlag) {
                            // ダメージパネルのヒット情報からHPを計算
                            double attackBuff = 1;
                            if (robotStatus.TeamColor.Contains("Red"))
                                attackBuff = Master.Instance.BlueAttackBuff;
                            else
                                attackBuff = Master.Instance.RedAttackBuff;

                            for (int i = 0; i < 4; i++) {
                                if (BitHigh(info[5], i)) {
                                    robotStatus.HP -= (int)(attackBuff * Master.Instance.HitDamage);
                                    robotStatus.DamageTaken += (int)(attackBuff * Master.Instance.HitDamage);
                                    robotStatus.AddRobotLog($"Hit DP{i}. -{attackBuff * Master.Instance.HitDamage}, now: {robotStatus.HP}/{robotStatus.MaxHP}");
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
                            }
                        }

                        if (statusChanged) {
                            var converter = new System.Windows.Media.BrushConverter();
                            Dispatcher.Invoke(() => {
                                HostStatusTextBox.IsEnabled = true;
                                HostStatusTextBox.Text = $"ARS: OK, SR: {numSoftwareReset}";
                                HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                            });
                            statusChanged = false;
                        }
                        //numTimeout = 0;
                    } catch (Exception ex) when (ex is IOException || ex is TimeoutException) {
                        statusChanged = true;
                        SystemSounds.Exclamation.Play();

                        var converter = new System.Windows.Media.BrushConverter();
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Disconnected \r\n" +
                                $"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Retry to start ARS\r\n"
                            );
                            LinkTextBox.ScrollToEnd();

                            HostStatusTextBox.Text = "Restarting: Openning HostPCB";
                            HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#66F5E98B");
                        });

                        numSoftwareReset++;
                        if (!client.Connected) client.Close();
                        stream.Close();
                        Thread.Sleep(2000);
                        arsSequence = Master.ARSSequenceEnum.RECONNECTING;
                    } 
                    catch (Exception ex) {
                        Debug.WriteLine(ex);
                    }

                } finally {
                    Interlocked.Exchange(ref _isBusy, 0);
                }
            }
        }

        private void ClearLog(object sender, EventArgs args) {
            logClear = true;
        }

        private void StartWatchingReceiveData() {
            _isWatching = true;
            _updateTimer.Elapsed += WatchReceivedData;
        }

        private void StopWatchingReceivedData() {
            _isWatching = false;
            _updateTimer.Elapsed -= WatchReceivedData;
        }

        // 試合中以外ではこの関数で常時受信データを監視する
        private async void WatchReceivedData(object sender, EventArgs args) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }

            if (stream.DataAvailable) {
                byte[] buffer = new byte[256];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Dispatcher.Invoke(() => {
                    LinkTextBox.AppendText(data);
                    LinkTextBox.ScrollToEnd();
                });
            }
        }

        private string ReadSendCommandResponse(string command) {
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

        private string ReadTo(string value, int timeoutMilliseconds=2000) {
            if (stream == null) return "error";

            StringBuilder sb = new StringBuilder();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
                if (stream.DataAvailable) {
                    int b = stream.ReadByte();
                    string receivedChar = Convert.ToChar(b).ToString();
                    //string receivedChar = System.Text.Encoding.ASCII.GetString(new byte[] { b })
                    //string receivedChar = b.ToString()
                    sb.Append(receivedChar);
                    Debug.Write(receivedChar);
                    if (receivedChar == value) return sb.ToString();
                }
            }
            throw new TimeoutException("read timeout");
        }

        private string ReadLine() {
            string data1 = ReadTo("\r");
            string data2 = ReadTo("\n");

            return data1.Substring(0, data1.Length - 1); // \r\nは除外
        }

        private void CommEnabledToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (CommEnabledToggleButton1.IsChecked == true) {
                Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;
                ConnectButton.IsEnabled = true;
            } else {
                Robot1.Status.Connection = Master.RobotConnectionEnum.DISABLED;
                ConnectButton.IsEnabled = false;
            }
        }


        private async void ConnectButton_Click(object sender, RoutedEventArgs e) {
            if (client is null || !client.Connected) {
                if (serverIPEndPoint is null) {
                    // Debug.WriteLine("stream is null");
                    return;
                }
                try {
                    ConnectButton.Content = "...";

                    client = new TcpClient();
                    await client.ConnectAsync(serverIPEndPoint);
                    stream = client.GetStream();

                    // タイムアウトの設定
                    stream.ReadTimeout = 2000;
                    stream.WriteTimeout = 2000;

                    arsSequence = Master.ARSSequenceEnum.OPENED;
                    HostStatusTextBox.Text = "Arduino server connected";
                    ConnectButton.Content = "Close";
                    PingButton1.IsEnabled = true;
                    BootButton.IsEnabled = true;
                    SendButton.IsEnabled = true;
                } catch (Exception ex) {
                    ConnectButton.Content = "Open";
                    MessageBox.Show($"{this.Name}: Failed to connect to Arduino server\n" +
                         $"\nProbably, selected IP has already been connnected by another.",
                         "Connection failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                StopWatchingReceivedData();
                Thread.Sleep(1000);

                if (stream is not null) {
                    stream.Close();
                }
                client.Close();
                arsSequence = Master.ARSSequenceEnum.NONE;
                HostStatusTextBox.Text = "Arduino server closed";
                ConnectButton.Content = "Open";
                ConnectButton.IsEnabled = true;
                PingButton1.IsEnabled = false;
                BootButton.IsEnabled = false;
                SendButton.IsEnabled = false;
            }

        }

        private void SendButton_Click(object obj, RoutedEventArgs e) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }

            SendTextToArduino(SendDataTextBox.Text);
            LinkTextBox.AppendText($"|--> ");
            SendDataTextBox.Clear();
        }


        private void SendDataTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }

        private void BootButton_Click(object sender, RoutedEventArgs e) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }
            if (arsSequence == Master.ARSSequenceEnum.OPENED) {
                StopWatchingReceivedData();
                try {
                    HostStatusTextBox.Text = "Booting ARS...";
                    string command = "boot autoturret";
                    SendTextToArduino(command);

                    BootButton.Content = "Shtdwn";
                    arsSequence = Master.ARSSequenceEnum.BOOTING;

                } catch (Exception ex) {
                    ;
                }
            } else if (arsSequence == Master.ARSSequenceEnum.UPDATING) {
                arsSequence = Master.ARSSequenceEnum.SHUTING_DOWN;
            }
        }

        private void PingButton_Click(Object sender, RoutedEventArgs e) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }
            if (arsSequence != Master.ARSSequenceEnum.OPENED) return;

            string command = "ping autoturret";
            SendTextToArduino(command);
        }


        private void SendTextToArduino(string text, bool verbose = true) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }

            byte[] data = System.Text.Encoding.ASCII.GetBytes(text + "\r\n");
            //foreach (byte b in data) {
            //    stream.Write(new byte[] { b }, 0, 1);
            //    Thread.Sleep(2);
            //}

            stream.Write(data, 0, data.Length);

            if (verbose) {
                Dispatcher.Invoke(() => {
                    // LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] {text}\r\n");
                    LinkTextBox.AppendText($"|-${text}\r\n");
                    LinkTextBox.ScrollToEnd();
                });
            }
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

        private void EndPointTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var converter = new System.Windows.Media.BrushConverter();
            EndPointTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#30FF0000");
        }

        private void EndPointTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                string input = EndPointTextBox.Text;
                if (TryParseIpPort(input, out IPEndPoint? tmpIPEndPoint)) {
                    Keyboard.ClearFocus();
                    e.Handled = true;

                    serverIPEndPoint = tmpIPEndPoint;
                    Debug.WriteLine(serverIPEndPoint);
                    var converter = new System.Windows.Media.BrushConverter();
                    EndPointTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                } else {
                    MessageBox.Show($"Invalid endpoint: {EndPointTextBox.Text}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void EnterEndPoint() {
            string input = EndPointTextBox.Text;
            if (TryParseIpPort(input, out IPEndPoint? tmpIPEndPoint)) {
                serverIPEndPoint = tmpIPEndPoint;
                Debug.WriteLine(serverIPEndPoint);
                var converter = new System.Windows.Media.BrushConverter();
                EndPointTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
            } else {
                MessageBox.Show($"Invalid endpoint: {EndPointTextBox.Text}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        static bool TryParseIpPort(string input, out IPEndPoint? endPoint) {
            endPoint = null;

            // ":"が含まれていない場合は無効
            if (!input.Contains(":"))
                return false;

            // ":"で分割
            string[] parts = input.Split(':');
            if (parts.Length != 2)
                return false;

            // IPアドレスのチェック
            if (!IPAddress.TryParse(parts[0], out IPAddress? ipAddress))
                return false;

            // ポート番号のチェック（1～65535）
            if (!int.TryParse(parts[1], out int port) || port < 1 || port > 65535)
                return false;

            // IPEndPointを作成
            endPoint = new IPEndPoint(ipAddress, port);
            return true;
        }
    }
}