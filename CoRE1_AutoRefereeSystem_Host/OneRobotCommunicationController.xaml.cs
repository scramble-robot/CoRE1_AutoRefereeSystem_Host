using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Media;
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
        private int _isBusy = 0; // interlock用
        private List<string> _sendData = new List<string>();

        private bool _isWatching = false;
        public Master.ARSSequenceEnum arsSequence = Master.ARSSequenceEnum.NONE;

        private int numHardwareReset = 0;
        private int numSoftwareReset = 0;
        private int numTimeout = 0;
        private bool statusChanged = false;
        private bool logClear = false;

        // ARSの更新タイマー
        private System.Timers.Timer _updateTimer;
        private System.Timers.Timer _logClearTimer;


        public OneRobotCommunicationController() {
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

            // それぞれ個別のタイマーを使用する
            // Master.Instance.UpdateEvent += UpdateRobotStatus;

            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 100;
            _updateTimer.Elapsed += UpdateRobotStatus;
            _updateTimer.Start();

            _logClearTimer = new System.Timers.Timer();
            _logClearTimer.Interval = 10 * 60 * 1000;
            _logClearTimer.Elapsed += ClearLog;
            _updateTimer.Start();

            // Master.Instance.GameResetEvent += Reset;
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

        // private void UpdateRobotStatus() {
        private void UpdateRobotStatus(object sender, EventArgs args) {
            // 1つ前のイベントがクライアント基板からの応答が遅くてまだ終了していない（別スレッドで実行中）場合はスキップ
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
                        if (!_serialPort.IsOpen) {
                            Dispatcher.Invoke(() => {
                                HostStatusTextBox.Text = "HostPCB closed";
                                ConnectButton.Content = "Open";
                                ConnectButton.IsEnabled = true;
                                PingButton1.IsEnabled = false;
                                BootButton.IsEnabled = false;
                                ShutdownButton.IsEnabled = false;
                                SendButton.IsEnabled = false;
                                arsSequence = Master.ARSSequenceEnum.NONE;
                            });
                        }
                    } else if (arsSequence == Master.ARSSequenceEnum.BOOTING) {
                        string data = _serialPort.ReadTo(">");
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText(data + ">");

                            if (data.Contains("[OK]")) {
                                HostStatusTextBox.Text = "Boot succeeded";
                                HostStatusTextBox.IsEnabled = true;

                                BootButton.IsEnabled = false;
                                ConnectButton.IsEnabled = false;
                                ShutdownButton.IsEnabled = true;
                                PingButton1.IsEnabled = false;

                                Robot1.RespawnButton.IsEnabled = true;
                                Robot1.DefeatButton.IsEnabled = true;
                                Robot1.PunishButton.IsEnabled = true;

                                _serialPort.ReadTimeout = Master.Instance.ARSTimeoutRandom.Next(
                                    Master.Instance.TimeoutMin, Master.Instance.TimeoutMax
                                );
                                statusChanged = true;
                                Robot1.Status.Connection = Master.RobotConnectionEnum.CONNECTED;
                                arsSequence = Master.ARSSequenceEnum.UPDATING;
                            } else {
                                HostStatusTextBox.Text = "Boot failed";
                                arsSequence = Master.ARSSequenceEnum.OPENED;
                                StartWatchingReceiveData();
                            }
                        });
                    } else if (arsSequence == Master.ARSSequenceEnum.HARDWARE_RESET ||
                               arsSequence == Master.ARSSequenceEnum.SOFTWARE_RESET) {
                        if (!_serialPort.IsOpen) _serialPort.Open();
                        if (_serialPort.IsOpen) {
                            string data = _serialPort.ReadTo(">");
                            if (data.Contains("*** autoRef HOST ***")) {
                                Dispatcher.Invoke(() => {
                                    HostStatusTextBox.Text = "Restarting: HostPCB opened";
                                });
                                arsSequence = Master.ARSSequenceEnum.SETTING_CH;
                            }
                        }
                    } else if (arsSequence == Master.ARSSequenceEnum.SETTING_CH) {
                        _serialPort.DiscardInBuffer();
                        string command = "";
                        Dispatcher.Invoke(() => {
                            command = "setCh " + Master.Instance.HostCH[this.Name].ToString("D2");
                        });
                        SendTextToHostPCB(command);
                        string data = _serialPort.ReadTo(">");
                        if (data.Contains("OK")) {
                            Dispatcher.Invoke(() => {
                                HostStatusTextBox.Text = "Restarting: setCh succeeded";
                                HostStatusTextBox.IsEnabled = true;
                            });
                            _serialPort.ReadTimeout = Master.Instance.ARSTimeoutRandom.Next(
                                Master.Instance.TimeoutMin, Master.Instance.TimeoutMax
                            );
                            statusChanged = true;
                            arsSequence = Master.ARSSequenceEnum.UPDATING;
                        }
                    } else if (arsSequence == Master.ARSSequenceEnum.SHUTING_DOWN) {
                        var converter = new System.Windows.Media.BrushConverter();
                        Dispatcher.Invoke(() => {
                            HostStatusTextBox.Text = "Shutting down";
                            //HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#00FFFFFF");
                        });

                        Thread.Sleep(3000);
                        _serialPort.DiscardInBuffer();
                        string command = "shutdown";
                        SendTextToHostPCB(command);
                        // string data = _serialPort.ReadTo(">");
                        // HACK
                        string data = "OK";
                        if (data.Contains("OK")) {
                            Dispatcher.Invoke(() => {
                                HostStatusTextBox.Text = "ARS shutdown";
                                HostStatusTextBox.IsEnabled = false;

                                LinkTextBox.AppendText(data + ">");
                                LinkTextBox.ScrollToEnd();

                                ConnectButton.IsEnabled = true;
                                BootButton.IsEnabled = true;
                                ShutdownButton.IsEnabled = false;
                                PingButton1.IsEnabled = true;
                            });

                            numHardwareReset = 0;
                            numSoftwareReset = 0;
                            numTimeout = 0;
                            _serialPort.ReadTimeout = 10000;
                            Robot1.Status.Connection = Master.RobotConnectionEnum.ENABLED;
                            arsSequence = Master.ARSSequenceEnum.OPENED;

                            // shutdownコマンドは時間がかかるので，cpuResetは無し
                            // Thread.Sleep(1000);
                            // command = "cpuReset";
                            // SendTextToHostPCB(command, false);
                            StartWatchingReceiveData();
                        }
                    }

                } catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is OperationCanceledException) {
                    var converter = new System.Windows.Media.BrushConverter();
                    Dispatcher.Invoke(() => {
                        LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] HARDWARE RESETED \r\n" +
                            $"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Retry to start ARS\r\n"
                        );
                        LinkTextBox.ScrollToEnd();

                        HostStatusTextBox.Text = "Restarting: Openning HostPCB";
                        //HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#00FFFFFF");
                    });

                    if (_serialPort.IsOpen) _serialPort.Close();
                    numHardwareReset++;
                    arsSequence = Master.ARSSequenceEnum.HARDWARE_RESET;
                    Thread.Sleep(1000);
                    return;
                } catch (TimeoutException ex) {
                    Debug.WriteLine(ex.Message);
                    if (arsSequence == Master.ARSSequenceEnum.SOFTWARE_RESET) {
                        SystemSounds.Hand.Play();
                        numTimeout++;
                        var converter = new System.Windows.Media.BrushConverter();
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText("HostPCB does not work\r\n");
                            LinkTextBox.AppendText("Please HARDWARE RESET\r\n");
                            LinkTextBox.ScrollToEnd();

                            HostStatusTextBox.Text = $"ARS: TIMEOUT x{numTimeout},  H/SR: {numHardwareReset}/{numSoftwareReset}";
                            HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#30FF0000");
                        });
                    }
                    return;
                } catch (FileNotFoundException) { // COMポートがまだないときに発生
                    Dispatcher.Invoke(() => {
                        LinkTextBox.Text = "Desired COM is not found\r\nSleep 500ms...";
                    });
                    Thread.Sleep(500);
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                    return;
                } finally {
                    Interlocked.Exchange(ref _isBusy, 0);
                }
                return;
            } else {
                // arsSequence == Master.ARSSequenceEnum.UPDATING
                if (_isWatching) StopWatchingReceivedData();
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

                    // クライアント基板からの応答待機
                    try {
                        // データを送信
                        _serialPort.DiscardInBuffer();
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Requesting... \r\n");
                            // LinkTextBox.ScrollToEnd();
                        });
                        string command = "send " + Master.Instance.TeamNodeNo[Robot1.Status.TeamName].ToString("D4") + " "
                                         + String.Join(",", _sendData);
                        SendTextToHostPCB(command);

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

                        // Debug.WriteLine(receivedDataString);

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
                                    robotStatus.AddRobotLog($"Hit DP{i}. HP decereased by {attackBuff * Master.Instance.HitDamage}, now at {robotStatus.HP}/{robotStatus.MaxHP}");
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

                        if (statusChanged) {
                            var converter = new System.Windows.Media.BrushConverter();
                            Dispatcher.Invoke(() => {
                                HostStatusTextBox.IsEnabled = true;
                                HostStatusTextBox.Text = $"ARS: OK,  H/SR: {numHardwareReset}/{numSoftwareReset}";
                                HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                            });
                            statusChanged = false;
                        }
                        numTimeout = 0;

                        // 通信待機時間．干渉対策用
                        // Thread.Sleep(200);
                    } catch (TimeoutException e) {
                        numTimeout++;
                        statusChanged = true;
                        Debug.WriteLine(e.ToString());

                        if (numTimeout == 1) SystemSounds.Exclamation.Play();
                        else SystemSounds.Hand.Play();

                        var converter = new System.Windows.Media.BrushConverter();
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Response timeout \r\n");
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] SOFTWARE RESETTING... \r\n" +
                                $"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Retry to start ARS \r\n"
                            );

                            if (numTimeout == 1) {
                                HostStatusTextBox.Text = $"ARS: TIMEOUT x{numTimeout},  H/SR: {numHardwareReset}/{numSoftwareReset}";
                                HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#66F5E98B");
                            } else {
                                HostStatusTextBox.Text = $"ARS: TIMEOUT x{numTimeout},  H/SR: {numHardwareReset}/{numSoftwareReset}";
                                HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#30FF0000");
                            }

                            LinkTextBox.ScrollToEnd();
                        });

                        string command = "cpuReset";
                        SendTextToHostPCB(command, false);
                        numSoftwareReset++;
                        arsSequence = Master.ARSSequenceEnum.SOFTWARE_RESET;
                        _serialPort.ReadTimeout = Master.Instance.ARSTimeoutRandom.Next(
                            Master.Instance.TimeoutMin, Master.Instance.TimeoutMax
                        );
                        Thread.Sleep(1000);
                    } catch (Exception ex) when (
                            ex is IOException || ex is InvalidOperationException ||
                            ex is OperationCanceledException) {
                        var converter = new System.Windows.Media.BrushConverter();
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] HARDWARE RESETED \r\n" +
                                $"[{DateTime.Now.ToString("HH:mm:ss.ff")}] Retry to start ARS\r\n"
                            );
                            LinkTextBox.ScrollToEnd();
                            _serialPort.Close();

                            HostStatusTextBox.Text = "Restarting: Openning HostPCB";
                            HostStatusTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#66F5E98B");
                        });
                        numHardwareReset++;
                        if (_serialPort.IsOpen) _serialPort.Close();
                        arsSequence = Master.ARSSequenceEnum.HARDWARE_RESET;
                        Thread.Sleep(1000);
                    } catch (Exception e) {
                        Debug.WriteLine(e.ToString());
                        Dispatcher.Invoke(() => {
                            LinkTextBox.AppendText("Error \r\n");
                            LinkTextBox.ScrollToEnd();
                        });
                        Thread.Sleep(100);
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
            arsSequence = Master.ARSSequenceEnum.NONE;
        }

        private void UpdateCOMPortsList() {
            Dispatcher.Invoke(() => {
                var selectedPort = ComPortSelectionComboBox.SelectedItem as string;

                ComPortSelectionComboBox.Items.Clear();
                ComPortSelectionComboBox.Items.Add("");
                foreach (string port in COMPortWatcher.Instance.GetAvailablePorts()) {
                    if (!ComPortSelectionComboBox.Items.Contains(port))
                        ComPortSelectionComboBox.Items.Add(port);
                }

                if (ComPortSelectionComboBox.Items.Contains(selectedPort)) {
                    ComPortSelectionComboBox.SelectedItem = selectedPort;
                }

            });
            //Master.Instance.SettingsChanged = true;
        }

        private string ReadSendCommandResponse(string command) {
            // 始めにこちらから送信したcommandがそのままホスト基板から返ってくる
            string data1 = _serialPort.ReadLine();
            if (!data1.Contains(command)) {
                _serialPort.ReadTo(">");
                return "send error";
            }

            // 次に所望のデータあるいは[NG]が返ってくる
            string data2 = _serialPort.ReadLine();
            Dispatcher.Invoke(() => {
                LinkTextBox.AppendText($"|--> {data2}\r\n");
            });
            if (data2.Contains("ERR") || data2.Contains("[NG]")) {
                _serialPort.ReadTo(">");
                return "comm error";
            }

            // 最後に[NG]ではない場合は[OK]が返ってくる
            string data3 = _serialPort.ReadLine();
            Dispatcher.Invoke(() => {
                LinkTextBox.AppendText($"|-->{data3}\r\n");
            });
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
                    arsSequence = Master.ARSSequenceEnum.OPENED;
                    HostStatusTextBox.Text = "HostPCB opend";
                    ConnectButton.Content = "Close";
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
                HostStatusTextBox.Text = "HostPCB closed";
                ConnectButton.Content = "Open";
                ConnectButton.IsEnabled = true;
                PingButton1.IsEnabled = false;
                BootButton.IsEnabled = false;
                ShutdownButton.IsEnabled = false;
                SendButton.IsEnabled = false;
                arsSequence = Master.ARSSequenceEnum.NONE;
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
            if (!_serialPort.IsOpen) return;
            if (arsSequence != Master.ARSSequenceEnum.OPENED) return;

            StopWatchingReceivedData();
            try {
                HostStatusTextBox.Text = "Booting ARS...";
                arsSequence = Master.ARSSequenceEnum.BOOTING;
                _serialPort.DiscardInBuffer();
                string command = $"boot {Master.Instance.HostCH[this.Name]} " + Master.Instance.TeamNodeNo[Robot1.Status.TeamName].ToString("D4");
                SendTextToHostPCB(command);
            } catch (Exception ex) {
                ;
            }
        }

        private void ShutdownButton_Click(object sender, RoutedEventArgs e) {
            arsSequence = Master.ARSSequenceEnum.SHUTING_DOWN;
        }

        private void PingButton_Click(Object sender, RoutedEventArgs e) {
            if (!_serialPort.IsOpen) return;
            if (Robot1.Status.TeamName is null) return;

            string command = "ping " + Master.Instance.TeamNodeNo[Robot1.Status.TeamName].ToString("D4");
            SendTextToHostPCB(command, false);
        }

        private void SendTextToHostPCB(string text, bool verbose = true) {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(text + "\r\n");
            foreach (byte b in data) {
                _serialPort.Write(new byte[] { b }, 0, 1);
                Thread.Sleep(2);
            }

            if (verbose) {
                Dispatcher.Invoke(() => {
                    // LinkTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ff")}] {text}\r\n");
                    LinkTextBox.AppendText($"|-${text}\r\n");
                    LinkTextBox.ScrollToEnd();
                });
            }
        }

        private void ComPortSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (arsSequence != Master.ARSSequenceEnum.UPDATING)
                Master.Instance.SettingsChanged = true;
        }
    }
}
