using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using static CoRE1_AutoRefereeSystem_Host.CommonBaseStatusManager;
using static CoRE1_AutoRefereeSystem_Host.RobotStatusManager;
using System.Windows.Input;
using System.Text;
using System.IO;
using System.Media;


namespace CoRE1_AutoRefereeSystem_Host
{
    /// <summary>
    /// CommonBaseStatusManager.xaml の相互作用ロジック
    /// </summary>
    public partial class CommonBaseStatusManager : UserControl
    {
        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty CommonBaseLabelProperty = DependencyProperty.Register("CommonBaseLabel", typeof(string), typeof(TeamBaseStatusManager), new PropertyMetadata("Base-#"));
        public static readonly DependencyProperty CommonBaseColorProperty = DependencyProperty.Register("CommonBaseColor", typeof(string), typeof(TeamBaseStatusManager), new PropertyMetadata("#10FF0000"));

        public string CommonBaseLabel {
            get { return (string)GetValue(CommonBaseLabelProperty); }
            set { SetValue(CommonBaseLabelProperty, value); }
        }
        public string CommonBaseColor {
            get { return (string)GetValue(CommonBaseColorProperty); }
            set { SetValue(CommonBaseColorProperty, value); }
        }

        #endregion

        /* BaseStatusの定義 ******************************************************************************************************************************************/
        #region
        public class BaseStatus {
            private readonly CommonBaseStatusManager _baseStatusManager;
            private Master.BaseConnectionEnum _connection;
            private bool _isActive = false;
            private Master.HPBarColorEnum _occupationLevelBarColor;
            private Master.DamagePanelColorEnum _damagePanelColor;
            private Master.OccupiedEnum _occupied = Master.OccupiedEnum.NO;

            // 赤は -5 ~ -1
            // 中点が 0
            // 青は 1 ~ 5
            private int _occupationLevel = 0;

            public enum DamagePanelPosition {
                LeftRDP,
                CenterRDP,
                RightRDP,
                LeftBDP,
                CenterBDP,
                RightBDP
            }

            private DateTime _lastAttackStartTime;
            private TimeSpan _lastAttackRemaingTime;

            private bool[] _dpIsInvulnerable = new bool[6];
            private DateTime[] _dpInvulnerableStartTime = new DateTime[6];
            private TimeSpan[] _dpInvulnerableRemainingTime = new TimeSpan[6];

            private List<string> _log = new List<string>();

            public BaseStatus(CommonBaseStatusManager instance) {
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

                    if (_occupationLevel <= -5) {
                        _occupationLevel = -5;
                        _occupied = Master.OccupiedEnum.RED;
                    } else if (_occupationLevel >= 5) {
                        _occupationLevel = 5;
                        _occupied = Master.OccupiedEnum.BLUE;
                    } else {
                        _occupied = Master.OccupiedEnum.NO;
                        _lastAttackStartTime = DateTime.Now;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.ResetTimeTextBox.IsEnabled = true;
                        });
                    }

                    string pointText = "N/A";
                    if (_occupationLevel == 0) {
                        _occupationLevelBarColor = Master.HPBarColorEnum.WHITE;
                    } else if (_occupationLevel < 0) {
                        _occupationLevelBarColor = Master.HPBarColorEnum.RED;
                        pointText = "R" + Math.Abs(_occupationLevel).ToString();
                    } else {
                        _occupationLevelBarColor = Master.HPBarColorEnum.BLUE;
                        pointText = "B" + Math.Abs(_occupationLevel).ToString();
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.OccupationLevelBar.Value = 5 + _occupationLevel;
                        _baseStatusManager.PointTextBox.Text = pointText;
                    });
                }
            }

            public Master.OccupiedEnum Occupied {
                get { return _occupied; }
                set { _occupied = value; }
            }

            public DateTime LastAttackStartTime {
                get { return _lastAttackStartTime; }
                set { _lastAttackStartTime = value; }
            }

            public TimeSpan LastAttackRemainingTime {
                get { return _lastAttackRemaingTime; }
                set {
                    _lastAttackRemaingTime = value;

                    if (_lastAttackRemaingTime.TotalSeconds <= 0) {
                        OccupationLevel = 0;
                        Occupied = Master.OccupiedEnum.NO;
                        
                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.ResetTimeTextBox.Text = "";
                            _baseStatusManager.ResetTimeTextBox.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.ResetTimeTextBox.Text = $"{_lastAttackRemaingTime.Seconds:00} sec.";
                    });
                }
            }

            /* 各ダメージパネルの設定 ******************************************************************************************************************************************/
            #region
            public bool LeftRDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.LeftRDP]; }
                set { 
                    _dpIsInvulnerable[(int)DamagePanelPosition.LeftRDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool CenterRDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.CenterRDP]; }
                set { 
                    _dpIsInvulnerable[(int)DamagePanelPosition.CenterRDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool RightRDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.RightRDP]; }
                set { 
                    _dpIsInvulnerable[(int)DamagePanelPosition.RightRDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool LeftBDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.LeftBDP]; }
                set { 
                    _dpIsInvulnerable[(int)DamagePanelPosition.LeftBDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool CenterBDPInvulnerable { 
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.CenterBDP]; }
                set { 
                    _dpIsInvulnerable[(int)DamagePanelPosition.CenterBDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }

            public bool RightBDPInvulnerable {
                get { return _dpIsInvulnerable[(int)DamagePanelPosition.RightBDP]; }
                set { 
                    _dpIsInvulnerable[(int)DamagePanelPosition.RightBDP] = value;

                    Application.Current.Dispatcher.Invoke(() => {
                        if (value) {
                            _baseStatusManager.DamagePanelRL.Opacity = 0.5;
                        } else {
                            _baseStatusManager.DamagePanelRL.Opacity = 1.0;
                        }
                    });
                }
            }
            public DateTime LeftRDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftRDP]; }
                set { 
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftRDP] = value;
                }
            }

            public TimeSpan LeftRDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftRDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftRDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftRDP].TotalSeconds <= 0) {
                        LeftRDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftRDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime CenterRDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.CenterRDP]; }
                set { 
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.CenterRDP] = value;
                }
            }

            public TimeSpan CenterRDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterRDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterRDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterRDP].TotalSeconds <= 0) {
                        CenterRDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterRDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime RightRDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.RightRDP]; }
                set { 
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.RightRDP] = value;
                }
            }

            public TimeSpan RightRDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightRDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightRDP] = value;
                    
                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightRDP].TotalSeconds <= 0) {
                        RightRDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightRDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime LeftBDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftBDP]; }
                set { 
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.LeftBDP] = value;
                }
            }

            public TimeSpan LeftBDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftBDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftBDP] = value;
                    
                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftBDP].TotalSeconds <= 0) {
                        LeftBDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.LeftBDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime CenterBDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.CenterBDP]; }
                set { 
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.CenterBDP] = value;
                }
            }

            public TimeSpan CenterBDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterBDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterBDP] = value;
                    
                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterBDP].TotalSeconds <= 0) {
                        CenterBDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.CenterBDP].Seconds:00} sec.";
                    });
                }
            }

            public DateTime RightBDPInvulnerableStartTime {
                get { return _dpInvulnerableStartTime[(int)DamagePanelPosition.RightBDP]; }
                set { 
                    _dpInvulnerableStartTime[(int)DamagePanelPosition.RightBDP] = value;
                }
            }

            public TimeSpan RightBDPInvulnerableRemainigTime {
                get { return _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightBDP]; }
                set {
                    _dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightBDP] = value;

                    if (_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightBDP].TotalSeconds <= 0) {
                        RightBDPInvulnerable = false;

                        Application.Current.Dispatcher.Invoke(() => {
                            _baseStatusManager.InvincibleTimeTextBoxRL.Text = "";
                            _baseStatusManager.InvincibleTimeTextBoxRL.IsEnabled = false;
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _baseStatusManager.InvincibleTimeTextBoxRL.Text = $"{_dpInvulnerableRemainingTime[(int)DamagePanelPosition.RightBDP].Seconds:00} sec.";
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


        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty BasePanelLabelProperty = DependencyProperty.Register("BasePanelLabel", typeof(string), typeof(RobotStatusManager), new PropertyMetadata("Base-C"));
        public string BasePanelLabel {
            get { return (string)GetValue(BasePanelLabelProperty); }
            set { SetValue(BasePanelLabelProperty, value); }
        }
        #endregion

        /* インスタンス ****************************************************************************************************************************************/
        private BaseStatus _baseStatus;

        public BaseStatus Status {
            private set { _baseStatus = value; }
            get { return _baseStatus; }
        }

        // タイマー
        private System.Timers.Timer _invulnerableTimer;
        private System.Timers.Timer _lastAttackTimer;

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

        public CommonBaseStatusManager() {
            InitializeComponent();
            _baseStatus = new BaseStatus(this);

            _invulnerableTimer = new System.Timers.Timer();
            _invulnerableTimer.Interval = 50;
            _invulnerableTimer.Elapsed += OnInvulnerableTimedEvent;
            _invulnerableTimer.Start();

            _lastAttackTimer = new System.Timers.Timer();
            _lastAttackTimer.Interval = 50;
            _lastAttackTimer.Elapsed += OnLastAttackTimedEvent;
            _lastAttackTimer.Start();


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
                RedBaseDPPanel.Opacity = 1;
                BlueBaseDPPanel.Opacity = 1;
            } else {
                OccupationLevelBar.Opacity = 0.5;
                RedBaseDPPanel.Opacity = 0.5;
                BlueBaseDPPanel.Opacity = 0.5;
            }
        }
        #endregion

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
                    int occupationLevelPercent = Math.Abs(status.OccupationLevel) * 10;

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
                            BitShift(status.LeftRDPInvulnerable, 0) | BitShift(status.CenterRDPInvulnerable, 1) | BitShift(status.RightRDPInvulnerable, 2) 
                            | BitShift(status.LeftBDPInvulnerable, 3) | BitShift(status.CenterBDPInvulnerable, 4) | BitShift(status.RightBDPInvulnerable, 5)
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

                        if (Master.Instance.DuringGame && !status.IsActive) {
                            // ダメージパネルのヒット情報から占拠レベルを計算

                            // 青
                            if (status.Occupied != Master.OccupiedEnum.BLUE) {
                                int attackBuff = Master.Instance.BlueAttackBuff;
                                if (!status.LeftRDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.LeftRDP)) {
                                    int diff = 1 * attackBuff;
                                    status.OccupationLevel += diff;

                                    status.LeftRDPInvulnerable = true;
                                    status.LeftRDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Left RDP. -{diff}, now: {status.OccupationLevel}");
                                }
                                if (!status.CenterRDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.CenterRDP)) {
                                    int diff = 2 * attackBuff; // 2倍ダメージ
                                    status.OccupationLevel += diff;

                                    status.CenterRDPInvulnerable = true;
                                    status.CenterRDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Center RDP. -{diff}, now: {status.OccupationLevel}");
                                }
                                if (!status.RightRDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.RightRDP)) {
                                    int diff = 1 * attackBuff;
                                    status.OccupationLevel += diff;

                                    status.RightRDPInvulnerable = true;
                                    status.RightRDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Right RDP. -{diff}, now: {status.OccupationLevel}");
                                }
                            }

                            // 赤
                            if (status.Occupied != Master.OccupiedEnum.RED) {
                                var attackBuff = Master.Instance.RedAttackBuff;
                                if (!status.LeftBDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.LeftBDP)) {
                                    int diff = 1 * attackBuff;
                                    status.OccupationLevel -= diff;

                                    status.LeftBDPInvulnerable = true;
                                    status.LeftBDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Left BDP. -{diff}, now: {status.OccupationLevel}");
                                }
                                if (!status.CenterBDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.CenterBDP)) {
                                    int diff = 2 * attackBuff; // 2倍ダメージ
                                    status.OccupationLevel -= diff;

                                    status.CenterBDPInvulnerable = true;
                                    status.CenterBDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Center BDP. -{diff}, now: {status.OccupationLevel}");
                                }
                                if (!status.RightBDPInvulnerable && BitHigh(info[5], (int)BaseStatus.DamagePanelPosition.RightBDP)) {
                                    int diff = 1 * attackBuff;
                                    status.OccupationLevel -= diff;

                                    status.RightBDPInvulnerable = true;
                                    status.RightBDPInvulnerableStartTime = DateTime.Now;
                                    status.AddRobotLog($"Hit Right BDP. -{diff}, now: {status.OccupationLevel}");
                                }
                            }
                        }

                        if (status.Occupied != Master.OccupiedEnum.NO) {
                            string teamColor = status.Occupied == Master.OccupiedEnum.RED ? "Red" : "Blue";
                            status.AddRobotLog($"Occupied by {teamColor} team");
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

        private void OnLastAttackTimedEvent(object source, ElapsedEventArgs e) {
            if (!Master.Instance.DuringGame) return;
            if (_baseStatus.Occupied != Master.OccupiedEnum.NO) return;
            if (_baseStatus.OccupationLevel == 0) return;

            var timePassed = DateTime.Now - _baseStatus.LastAttackStartTime;
            _baseStatus.LastAttackRemainingTime = TimeSpan.FromSeconds(Master.Instance.BaseNeutralPointTime) - timePassed;
        }

        private void OnInvulnerableTimedEvent(object source, ElapsedEventArgs e) {
            if (!Master.Instance.DuringGame) return;

            if (_baseStatus.LeftRDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.LeftRDPInvulnerableStartTime;
                _baseStatus.LeftBDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }

            if (_baseStatus.CenterRDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.CenterRDPInvulnerableStartTime;
                _baseStatus.CenterBDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }

            if (_baseStatus.RightRDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.RightRDPInvulnerableStartTime;
                _baseStatus.RightBDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }
            
            if (_baseStatus.LeftBDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.LeftBDPInvulnerableStartTime;
                _baseStatus.LeftBDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }
            
            if (_baseStatus.CenterBDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.CenterBDPInvulnerableStartTime;
                _baseStatus.CenterBDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
            }
            
            if (_baseStatus.RightBDPInvulnerable) {
                var timePassed = DateTime.Now - _baseStatus.RightBDPInvulnerableStartTime;
                _baseStatus.RightBDPInvulnerableRemainigTime = TimeSpan.FromSeconds(Master.Instance.BaseInvulnerableTime) - timePassed;
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
                Debug.WriteLine("stream is null");
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

        public void RedLevelButton_Click(object sender, RoutedEventArgs e) {
            _baseStatus.OccupationLevel -= 1;
        }

        public void NeurtralButton_Click(object sender, RoutedEventArgs e) {
            _baseStatus.OccupationLevel = 0;
        }

        public void BlueLevelButton_Click(object sender, RoutedEventArgs e) {
            _baseStatus.OccupationLevel += 1;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) {
            if (client is null || !client.Connected) {
                if (serverIPEndPoint is null) {
                    Debug.WriteLine("stream is null");
                    return;
                }
                try {
                    client = new TcpClient();
                    client.Connect(serverIPEndPoint);
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
                Debug.WriteLine("stream is null");
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
                Debug.WriteLine("stream is null");
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

        private void PingButton_Click(Object sender, RoutedEventArgs e) {
            if (stream is null) {
                Debug.WriteLine("stream is null");
                return;
            }
            if (arsSequence != Master.ARSSequenceEnum.OPENED) return;

            string command = "ping autoturret";
            SendTextToArduino(command);
        }

        private void SendTextToArduino(string text, bool verbose = true) {
            if (stream is null) {
                Debug.WriteLine("stream is null");
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
