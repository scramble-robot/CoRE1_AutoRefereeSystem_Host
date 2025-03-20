using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Diagnostics;
using static CoRE1_AutoRefereeSystem_Host.RobotStatusManager;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;
using System.Media;

namespace CoRE1_AutoRefereeSystem_Host
{
    /// <summary>
    /// TeamBaseStatusManager.xaml の相互作用ロジック
    /// </summary>
    public partial class TeamBaseStatusManager : UserControl
    {
        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty TeamBaseLabelProperty = DependencyProperty.Register("TeamBaseLabel", typeof(string), typeof(TeamBaseStatusManager), new PropertyMetadata("Base-#"));
        public static readonly DependencyProperty TeamBaseColorProperty = DependencyProperty.Register("TeamBaseColor", typeof(string), typeof(TeamBaseStatusManager), new PropertyMetadata("#10FF0000"));
        public static readonly DependencyProperty OccupationLevelColorProperty = DependencyProperty.Register("OccupationLevelColor", typeof(string), typeof(TeamBaseStatusManager), new PropertyMetadata(null));

        public string TeamBaseLabel {
            get { return (string)GetValue(TeamBaseLabelProperty); }
            set { SetValue(TeamBaseLabelProperty, value); }
        }
        public string TeamBaseColor {
            get { return (string)GetValue(TeamBaseColorProperty); }
            set { SetValue(TeamBaseColorProperty, value); }
        }

        public string OccupationLevelColor {
            get { return (string)GetValue(OccupationLevelColorProperty); }
            set { SetValue(OccupationLevelColorProperty, value); }
        }
        #endregion

        /* BaseStatusの定義 ******************************************************************************************************************************************/
        #region
        public class BaseStatus
        {
            private readonly TeamBaseStatusManager _baseStatusManager;
            private Master.BaseConnectionEnum _connection;
            private bool _isActive = false;
            private Master.HPBarColorEnum _occupationLevelBarColor;
            private Master.DamagePanelColorEnum _damagePanelColor;
            private Master.OccupiedEnum _occupied = Master.OccupiedEnum.NO;

            // チーム陣地は占拠レベルが10段階
            private int _occupationLevel = 0;

            public enum DamagePanelPosition
            {
                LeftHighDP,
                RightHighDP,
                LeftLowDP,
                RightLowDP
            }

            private bool[] _dpIsInvulnerable = new bool[4];
            private DateTime[] _dpInvulnerableStartTime = new DateTime[4];
            private TimeSpan[] _dpInvulnerableRemainingTime = new TimeSpan[4];

            private List<string> _log = new List<string>();

            public BaseStatus(TeamBaseStatusManager instance) {
                _baseStatusManager = instance;
            }

            public string BaseColor { set; get; } = "";

            public Master.BaseConnectionEnum Connection {
                get { return _connection; }
                set { _connection = value; }
            }

            public bool IsActive {
                get { return _isActive; }
                set { _isActive = value; }
            }

            public string NodeNo { get; set; } = "0109";

            public Master.HPBarColorEnum OccupationLevelBarColor {
                get { return _occupationLevelBarColor; }
                set { _occupationLevelBarColor = value; }
            }

            public Master.DamagePanelColorEnum DamagePanelColor {
                get { return _damagePanelColor; }
                set { _damagePanelColor = value; }
            }

            public int OccupationLevel {
                get { return _occupationLevel; }
                set {
                    _occupationLevel = value;

                    if (_occupationLevel >= 10) {
                        _occupationLevel = 10;
                        if (BaseColor.Contains("R")) _occupied = Master.OccupiedEnum.RED;
                        else _occupied = Master.OccupiedEnum.BLUE;
                    } else if (_occupationLevel < 0) {
                        _occupationLevel = 0;
                        _occupied = Master.OccupiedEnum.NO;
                    } else {
                        _occupied = Master.OccupiedEnum.NO;
                    }

                    string pointText = "N/A";
                    if (_occupationLevel == 0) {
                        _occupationLevelBarColor = Master.HPBarColorEnum.WHITE;
                    } else if (BaseColor.Contains("R")) {
                        _occupationLevelBarColor = Master.HPBarColorEnum.RED;
                        pointText = "B" + Math.Abs(_occupationLevel).ToString();
                    } else {
                        _occupationLevelBarColor = Master.HPBarColorEnum.BLUE;
                        pointText = "R" + Math.Abs(_occupationLevel).ToString();
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.OccupationLevelBar.Value = _occupationLevel;
                        _baseStatusManager.PointTextBox.Text = pointText;
                    });
                }
            }

            public Master.OccupiedEnum Occupied {
                get { return _occupied; }
                set { _occupied = value; }
            }

            /* 各ダメージパネルの設定 ******************************************************************************************************************************************/
            #region
            public bool LeftHighDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.LeftHighDP]; }
                set {
                    _dpIsInvulnerable[(int)DamagePanelPosition.LeftHighDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelLH.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelLH.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool RightHighDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.RightHighDP]; }
                set {
                    _dpIsInvulnerable[(int)DamagePanelPosition.RightHighDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRH.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRH.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool LeftLowDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.LeftLowDP]; }
                set {
                    _dpIsInvulnerable[(int)DamagePanelPosition.LeftLowDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelLL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelLL.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool RightLowDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.RightLowDP]; }
                set {
                    _dpIsInvulnerable[(int)DamagePanelPosition.RightLowDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }
            public DateTime LeftHighDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftHighDP]; }
                set {
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftHighDP] = value;
                }
            }

            public TimeSpan LeftHighDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftHighDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftHighDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftHighDP].TotalSeconds <= 0) {
                        LeftHighDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxLH.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftHighDP].Seconds:00} sec.";
                    });
                }   
            }

            public DateTime RightHighDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.RightHighDP]; }
                set {
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.RightHighDP] = value;
                }
            }

            public TimeSpan RightHighDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightHighDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightHighDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightHighDP].TotalSeconds <= 0) {
                        RightHighDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRH.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRH.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRH.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightHighDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime LeftLowDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftLowDP]; }
                set {
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftLowDP] = value;
                }
            }

            public TimeSpan LeftLowDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftLowDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftLowDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftLowDP].TotalSeconds <= 0) {
                        LeftLowDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxLL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxLL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxLL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftLowDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime RightLowDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.RightLowDP]; }
                set {
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.RightLowDP] = value;
                }
            }

            public TimeSpan RightLowDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightLowDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightLowDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightLowDP].TotalSeconds <= 0) {
                        RightLowDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightLowDP].Seconds:00} sec.";
                    });
                }
            }
            #endregion

            // ログ
            public List<string> Log {
                get { return _log; }
                set { _log = value; }
            }

            public void AddRobotLog(string text) {
                _log.Add($"[{Master.Instance.CurrentTime.Minutes:00}:{Master.Instance.CurrentTime.Seconds:00}:{Master.Instance.CurrentTime.Milliseconds:000}]" + text + "\r\n");
                Application.Current.Dispatcher.Invoke(() => {
                    _baseStatusManager.RobotLogTextBox.AppendText(_log[_log.Count - 1]);
                    _baseStatusManager.RobotLogTextBox.ScrollToEnd();
                });
            }
        };
        #endregion

        /* インスタンス ****************************************************************************************************************************************/
        private BaseStatus _baseStatus;
        public BaseStatus Status {
            private set { _baseStatus = value; }
            get { return _baseStatus; }
        }

        // タイマー
        private System.Timers.Timer _invulnerableTimer;

        /* 各種通信で使用する変数 ****************************************************************************************************************************************/
        // Arduinoサーバー
        private IPEndPoint? serverIPEndPoint = null;

        private TcpClient? client = null;
        private NetworkStream? stream = null;

        private int _isBusy = 0; // interlock用
        private List<string> _sendData = new List<string>();

        private bool _isWatching = false;
        public Master.ARSSequenceEnum arsSequence = Master.ARSSequenceEnum.NONE;

        private bool isActivePrev = false;
        private bool isOccupiedPrev = false;

        private int numSoftwareReset = 0;
        private int numTimeout = 0;
        private bool statusChanged = false;
        private bool logClear = false;

        // ARSの更新タイマー
        private System.Timers.Timer _updateTimer;
        private System.Timers.Timer _logClearTimer;

        public TeamBaseStatusManager() {
            InitializeComponent();
            _baseStatus = new BaseStatus(this);

            _invulnerableTimer = new System.Timers.Timer();
            _invulnerableTimer.Interval = 50;
            _invulnerableTimer.Elapsed += OnInvulnerableTimedEvent;
            _invulnerableTimer.Start();

            // それぞれ個別のタイマーを使用する
            // Master.Instance.UpdateEvent += UpdateRobotStatus;

            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 500;
            _updateTimer.Elapsed += UpdateBaseStatus;
            _updateTimer.Start();

            StartWatchingReceiveData();

            _logClearTimer = new System.Timers.Timer();
            _logClearTimer.Interval = 10 * 60 * 1000;
            _logClearTimer.Elapsed += ClearLog;
            _logClearTimer.Start();
        }

        /* ロード時のイベント ****************************************************************************************************************************************/
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            _baseStatus.BaseColor = TeamBaseLabel;
            if (_baseStatus.BaseColor.Contains("R")) {
                _baseStatus.OccupationLevelBarColor = Master.HPBarColorEnum.RED;
                _baseStatus.DamagePanelColor = Master.DamagePanelColorEnum.BLUE;
            } else {
                _baseStatus.OccupationLevelBarColor = Master.HPBarColorEnum.BLUE;
                _baseStatus.DamagePanelColor = Master.DamagePanelColorEnum.RED;
            }
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.IsEnabled) {
                OccupationLevelBar.Opacity = 1;
                //BaseDPPanel.Opacity = 1;

                Application.Current.Dispatcher.Invoke(() => {
                    InvincibleTimeTextBoxLH.Text = "nonactive";
                    InvincibleTimeTextBoxRH.Text = "nonactive";
                    InvincibleTimeTextBoxLL.Text = "nonactive";
                    InvincibleTimeTextBoxRL.Text = "nonactive";
                });

            } else {
                OccupationLevelBar.Opacity = 0.5;
                BaseDPPanel.Opacity = 0.5;
            }
        }

        private void UpdateBaseStatus(object sender, EventArgs args) {
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

                        string command = "boot teambase";
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

                                //stream.ReadTimeout = Master.Instance.ARSTimeoutRandom.Next(
                                //    Master.Instance.TimeoutMin, Master.Instance.TimeoutMax
                                //);
                                statusChanged = true;
                                _baseStatus.Connection = Master.BaseConnectionEnum.CONNECTED;
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
                            _baseStatus.Connection = Master.BaseConnectionEnum.ENABLED;
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
                    var status = _baseStatus;
                    var baseRecivedTextBox = ReceivedDataTextBox1;


                    bool activeFlag = status.IsActive;
                    bool defeatedFlag = false; // dummy

                    int hpBarColor = (int)status.OccupationLevelBarColor;
                    int dpColor = hpBarColor; // dummy
                    int occupationLevelPercent = status.OccupationLevel * 10;

                    // 送信データを規定のプロトコルに基づいて作成
                    _sendData.Clear();

                    // 宛先の機能No (09は共通陣地)
                    _sendData.Add("09");

                    // [b0: アクティブフラグ, b1: 撃破フラグ]
                    _sendData.Add(
                        (BitShift(activeFlag, 0) | BitShift(defeatedFlag, 1)).ToString("X2")
                    );

                    // [b0..3:HPバーのカラー,b4..7:ダメージプレートのカラー]
                    _sendData.Add(
                        (BitShift(hpBarColor, 0) | BitShift(dpColor, 4)).ToString("X2")
                    );

                    // [b0~b5: DP無敵フラグ] 陣地のみで使用
                    _sendData.Add((
                            BitShift(status.LeftHighDPInvulnerable, 0) | BitShift(status.RightHighDPInvulnerable, 1) 
                            | BitShift(status.LeftLowDPInvulnerable, 2) | BitShift(status.LeftLowDPInvulnerable, 3)
                        ).ToString("X2")
                    );

                    // HP% 0x00 ~ 0x64 (100)
                    _sendData.Add(occupationLevelPercent.ToString("X2"));

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
                        string command = "send " + status.NodeNo + " "
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
                                baseRecivedTextBox.AppendText(
                                $"[{Master.Instance.CurrentTime.Minutes:00}:{Master.Instance.CurrentTime.Seconds:00}:{Master.Instance.CurrentTime.Milliseconds:000}]\""
                                + receivedDataString + "\r\n");
                                baseRecivedTextBox.ScrollToEnd();
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

                        if (Master.Instance.DuringGame && status.IsActive) {
                            if (!isActivePrev) {
                                Application.Current.Dispatcher.Invoke(() => {
                                    BaseDPPanel.Opacity = 1;
                                    InvincibleTimeTextBoxLH.Text = "";
                                    InvincibleTimeTextBoxRH.Text = "";
                                    InvincibleTimeTextBoxLL.Text = "";
                                    InvincibleTimeTextBoxRL.Text = "";
                                });
                                isActivePrev = true;
                            }

                            // ダメージパネルのヒット情報から占拠レベルを計算
                            if (status.Occupied == Master.OccupiedEnum.NO) {
                                int attackBuff = 1;
                                if (status.BaseColor.Contains("R")) attackBuff = (int)Master.Instance.RedAttackBuff;
                                else attackBuff = (int)Master.Instance.BlueAttackBuff;

                                if (!status.LeftHighDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.LeftHighDP)) {
                                    int diff = 2 * attackBuff; // 2倍ダメージ
                                    status.OccupationLevel += diff;

                                    status.LeftHighDPInvulnerable = true;
                                    status.LeftHighDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Left-high DP. +{diff}, now: {status.OccupationLevel}/10");
                                }

                                if (!status.RightHighDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.RightHighDP)) {
                                    int diff = 2 * attackBuff; // 2倍ダメージ
                                    status.OccupationLevel += diff;

                                    status.RightHighDPInvulnerable = true;
                                    status.RightHighDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Right-high DP. +{diff}, now: {status.OccupationLevel}/10");
                                }

                                if (!status.LeftLowDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.LeftLowDP)) {
                                    int diff = 1 * attackBuff;
                                    status.OccupationLevel += diff;

                                    status.LeftLowDPInvulnerable = true;
                                    status.LeftLowDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Left-low DP. +{diff}, now: {status.OccupationLevel}/10");
                                }

                                if (!status.RightLowDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.RightLowDP)) {
                                    int diff = 1 * attackBuff;
                                    status.OccupationLevel += diff;

                                    status.RightLowDPInvulnerable = true;
                                    status.RightLowDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Right-low DP. +{diff}, now: {status.OccupationLevel}/10");
                                }
                            }
                        }

                        if (!isOccupiedPrev && status.Occupied != Master.OccupiedEnum.NO) {
                            string teamColor = status.Occupied == Master.OccupiedEnum.RED ? "Blue" : "Red";
                            status.AddRobotLog($"Occupied by {teamColor} team");
                            isOccupiedPrev = true;
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
                    } catch (Exception ex) {
                        Debug.WriteLine(ex);
                    }

                } finally {
                    Interlocked.Exchange(ref _isBusy, 0);
                }
            }
        }

        private void OnInvulnerableTimedEvent(object source, ElapsedEventArgs e) {
            if (!Master.Instance.DuringGame) return;

            if (_baseStatus.LeftHighDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.LeftHighDPInvulnerableStartTime;
                _baseStatus.LeftHighDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }

            if (_baseStatus.RightHighDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.RightHighDPInvulnerableStartTime;
                _baseStatus.RightHighDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }

            if (_baseStatus.RightLowDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.RightLowDPInvulnerableStartTime;
                _baseStatus.RightLowDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }

            if (_baseStatus.LeftLowDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.LeftLowDPInvulnerableStartTime;
                _baseStatus.LeftLowDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
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

        private string ReadTo(string value, int timeoutMilliseconds = 2000) {
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

        private void MinusButton_Click(object sender, RoutedEventArgs e) {
            _baseStatus.OccupationLevel -= 1;
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e) {
            _baseStatus.OccupationLevel += 1;
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

        private void SendButton_Click(object sender, RoutedEventArgs e) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }

            SendTextToArduino(SendDataTextBox.Text);
            LinkTextBox.AppendText($"|--> ");
            SendDataTextBox.Clear();
        }

        private void PingButton_Click(object sender, RoutedEventArgs e) {
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

        private void BootButton_Click(object sender, RoutedEventArgs e) {
            if (stream is null) {
                // Debug.WriteLine("stream is null");
                return;
            }
            if (arsSequence == Master.ARSSequenceEnum.OPENED) {
                StopWatchingReceivedData();
                try {
                    HostStatusTextBox.Text = "Booting ARS...";
                    string command = "boot commonbase";
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

        private void EndPointTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var converter = new System.Windows.Media.BrushConverter();
            EndPointTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#30FF0000");
        }

        private void EndPointTextBox_KeyDown(object sender, KeyEventArgs e) {
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


        private void SendDataTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                SendButton_Click(this, new RoutedEventArgs());
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
    }
}
