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

            private DateTime _lastAttackStartTime;
            private TimeSpan _lastAttackRemaingTime;

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

                    if (_occupationLevel <= -5) {
                        _occupationLevel = -5;
                        _occupied = Master.OccupiedEnum.RED;
                    } else if (_occupationLevel >= 5) {
                        _occupationLevel = 5;
                        _occupied = Master.OccupiedEnum.BLUE;
                    } else {
                        _occupied = Master.OccupiedEnum.NO;
                        _lastAttackStartTime = DateTime.Now;
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

        public TeamBaseStatusManager() {
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
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            _baseStatus.BaseColor = TeamBaseLabel;
            if (_baseStatus.BaseColor.Contains("R")) {
                _baseStatus.OccupationLevelBarColor = Master.HPBarColorEnum.RED;
                _baseStatus.DamagePanelColor = Master.DamagePanelColorEnum.RED;
            } else {
                _baseStatus.OccupationLevelBarColor = Master.HPBarColorEnum.BLUE;
                _baseStatus.DamagePanelColor = Master.DamagePanelColorEnum.BLUE;
            }
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.IsEnabled) {
                OccupationLevelBar.Opacity = 1;
                BaseDPPanel.Opacity = 1;
            } else {
                OccupationLevelBar.Opacity = 0.5;
                BaseDPPanel.Opacity = 0.5;
            }
        }

        private void UpdateBaseStatus(object sender, EventArgs args) {

        }

        private void OnLastAttackTimedEvent(object source, ElapsedEventArgs e) {
        }

        private void OnInvulnerableTimedEvent(object source, ElapsedEventArgs e) {
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

        private void MinusButton_Click(object sender, RoutedEventArgs e) {

        }

        private void PlusButton_Click(object sender, RoutedEventArgs e) {

        }

        private void PingButton_Click(object sender, RoutedEventArgs e) {

        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) {

        }

        private void BootButton_Click(object sender, RoutedEventArgs e) {

        }

        private void SendButton_Click(object sender, RoutedEventArgs e) {

        }

        private void EndPointTextBox_TextChanged(object sender, TextChangedEventArgs e) {

        }

        private void EndPointTextBox_KeyDown(object sender, KeyEventArgs e) {

        }

        private void SendDataTextBox_KeyDown(object sender, KeyEventArgs e) {

        }
    }
}
