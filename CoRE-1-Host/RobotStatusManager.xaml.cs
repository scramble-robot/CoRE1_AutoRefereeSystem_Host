using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;


namespace CoRE_1_Host
{
    /// <summary>
    /// RobotStatusManager.xaml の相互作用ロジック
    /// </summary>
    public partial class RobotStatusManager : UserControl {
        /* RobotStatusの定義 ******************************************************************************************************************************************/
        #region
        public class RobotStatus {
            private readonly RobotStatusManager _robotStatusManager;
            // public int team2idx;
            private Master.RobotConnectionEnum _connection;
            private Master.HPBarColorEnum _hpBarColor;
            private Master.DamagePanelColorEnum _damagePanelColor;
            private string _teamName;
            private int _teamID;
            private string _teamColor;
            private int _hp = 0;
            private int _maxHp = 0;
            private bool _defeatedFlag = false;
            private bool _powerOnFlag = true;
            private bool _invincivilityFlag = false;
            private TimeSpan _respawnTime;
            private string _respawnTimeString;
            private List<string> _log = new List<string>();
            private int _damageTaken = 0;
            private int _defeatedNum = 0;

            public RobotStatus(RobotStatusManager instance) {
                _robotStatusManager = instance;
            }

            public Master.RobotConnectionEnum Connection {
                get { return _connection; }
                set { _connection = value; }
            }

            public Master.HPBarColorEnum HPBarColor {
                get { return _hpBarColor;}
                set { _hpBarColor = value; }
            }

            public Master.DamagePanelColorEnum DamagePanelColor {
                get { return _damagePanelColor;}
                set { _damagePanelColor = value; }
            }

            public string TeamName {
                get { return _teamName; }
                set { _teamName = value; }
            }

            public int TeamID {
                get { return _teamID; }
                set { _teamID = value;}
            }

            public string TeamColor {
                get { return _teamColor; }
                set { _teamColor = value; }
            }

            public int HP {
                get { return _hp; }
                set {
                    _hp = value;
                    Application.Current.Dispatcher.Invoke(() => {
                        _robotStatusManager.HPBar.Value = _hp;
                        _robotStatusManager.HPTextBox.Text = $"{_hp}/{_maxHp}";
                    });
                }
            }

            public int MaxHP {
                get { return _maxHp; }
                set {
                    _maxHp = value;
                    if (!Application.Current.Dispatcher.CheckAccess()) {
                        Application.Current.Dispatcher.Invoke(() => {
                            _robotStatusManager.HPBar.Maximum = _maxHp;
                        });
                    } else {
                        _robotStatusManager.HPBar.Maximum = _maxHp;
                    }
                }
            }

            public bool DefeatedFlag {
                get { return _defeatedFlag; }
                set { 
                    _defeatedFlag = value;
                    if (_defeatedFlag) {
                        _hpBarColor = Master.HPBarColorEnum.YELLOW;
                        _damagePanelColor = Master.DamagePanelColorEnum.YELLOW;
                    } else {
                        if (_teamColor.Contains("Red")) {
                            _hpBarColor = Master.HPBarColorEnum.RED;
                            _damagePanelColor = Master.DamagePanelColorEnum.RED;
                        } else {
                            _hpBarColor = Master.HPBarColorEnum.BLUE;
                            _damagePanelColor = Master.DamagePanelColorEnum.BLUE;
                        }
                    }
                }
            }

            public bool PowerOnFlag {
                get { return _powerOnFlag; }
                set { _powerOnFlag = value; }
            }

            private bool _invincibilityFlagPrev = false;
            public bool InvincibilityFlag {
                get { return _invincivilityFlag; }
                set {
                    _invincivilityFlag = value;
                    if (!_invincibilityFlagPrev && _invincivilityFlag) {
                        AddRobotLog("Respawn & Invincible Time Start...");
                        _hpBarColor = Master.HPBarColorEnum.GREEN;
                        _damagePanelColor = Master.DamagePanelColorEnum.GREEN;
                    }
                    else if (_invincibilityFlagPrev && !InvincibilityFlag) {
                        AddRobotLog("Invincile Time End.");
                        if (_teamColor.Contains("Red")) {
                            _hpBarColor = Master.HPBarColorEnum.RED;
                            _damagePanelColor = Master.DamagePanelColorEnum.RED;
                        } else {
                            _hpBarColor = Master.HPBarColorEnum.BLUE;
                            _damagePanelColor = Master.DamagePanelColorEnum.BLUE;
                        }
                    }
                    _invincibilityFlagPrev = _invincivilityFlag;
                }
            }

            public TimeSpan RespawnTime {
                get { return _respawnTime; }
                set {
                    _respawnTime = value;

                    if (_respawnTime.TotalSeconds <= 0) {
                        Application.Current.Dispatcher.Invoke(() => {
                            _robotStatusManager.RespawnTimeTextBlock.Text = "";
                            _robotStatusManager.RespawnTimeTextBlock.IsEnabled = false;
                        });
                        // Master.Instance.Msgs.Robot[team2idx].RespawnTime = "00:00";
                        _respawnTimeString = "00:00";
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _robotStatusManager.RespawnTimeTextBlock.Text = $"{_respawnTime.Seconds:00} sec.";
                    });
                    _respawnTimeString = $"{_respawnTime.Minutes:00}:{_respawnTime.Seconds:00}";
                }
            }

            public string RespawnTimeString {
                get { return _respawnTimeString; }
                private set {; }
            }

            // ログ
            public List<string> Log {
                get { return _log; }
                set { _log = value; }
            }

            public void AddRobotLog(string text) {
                _log.Add($"[{Master.Instance.CurrentTime.Minutes:00}:{Master.Instance.CurrentTime.Seconds:00}:{Master.Instance.CurrentTime.Milliseconds:000}]" + text + "\r\n");
                Application.Current.Dispatcher.Invoke(() => {
                    _robotStatusManager.RobotLogTextBox.AppendText(_log[_log.Count - 1]);
                    _robotStatusManager.RobotLogTextBox.ScrollToEnd();
                });
            }

            public int DamageTaken {
                get { return _damageTaken; }
                set {
                    _damageTaken = value;
                    /*if (TeamColor.Contains("Red")) {
                        if (value == 0) Master.Instance.TotalRedDamageTaken = 0;
                        else Master.Instance.TotalRedDamageTaken += 10;
                    } else {
                        if (value == 0) Master.Instance.TotalBlueDamageTaken = 0;
                        else Master.Instance.TotalBlueDamageTaken += 10;
                    }*/

                    Application.Current.Dispatcher.Invoke(() => {
                        _robotStatusManager.DamageTakenTextBlock.Text = _damageTaken.ToString();
                    });
                }
            }

            public int DefeatedNum {
                get { return _defeatedNum; }
                set {
                    _defeatedNum = value;
                    if (TeamColor.Contains("Red")) {
                        if (value == 0) Master.Instance.TotalRedDefeated = 0;
                        else Master.Instance.TotalRedDefeated++;
                    } else {
                        if (value == 0) Master.Instance.TotalBlueDefeated = 0;
                        else Master.Instance.TotalBlueDefeated++;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _robotStatusManager.DefeatedTextBlock.Text = _defeatedNum.ToString();
                    });
                }
            }
        };
        #endregion


        /* 依存プロパティの設定 ****************************************************************************************************************************************/
        #region
        public static readonly DependencyProperty PanelColorProperty = DependencyProperty.Register("PanelColor", typeof(string), typeof(RobotStatusManager), new PropertyMetadata("#10FF0000"));
        public static readonly DependencyProperty PanelLabelProperty = DependencyProperty.Register("PanelLabel", typeof(string), typeof(RobotStatusManager), new PropertyMetadata("Blue/Red #"));
        public string PanelLabel {
            get { return (string)GetValue(PanelLabelProperty); }
            set { SetValue(PanelLabelProperty, value); }
        }

        public string PanelColor {
            get { return (string)GetValue(PanelColorProperty); }
            set { SetValue(PanelColorProperty, value); }
        }

        #endregion


        private RobotStatus _status;
        public RobotStatus Status {
            private set { _status = value; }
            get { return _status; }
        }
        
        // タイマー
        private DateTime _startTime;
        private TimeSpan _remainingTime;
        private System.Timers.Timer _respawnTimer;
        private System.Timers.Timer _invincibilityTimer;


        public RobotStatusManager() {
            InitializeComponent();

            TeamNameComboBox.Items.Clear();
            foreach (string tn in Master.Instance.TeamName)
                TeamNameComboBox.Items.Add(tn);

            Status = new RobotStatus(this);

            _respawnTimer = new System.Timers.Timer();
            _respawnTimer.Interval = 50;
            _respawnTimer.Elapsed += OnRespawnTimedEvent;

            _invincibilityTimer = new System.Timers.Timer();
            _invincibilityTimer.Interval = 50;
            _invincibilityTimer.Elapsed += OnInvincibilityTimedEvent;

            Master.Instance.ClearDataEvent += Reset;
        }

        /* ロード時のイベント ****************************************************************************************************************************************/
        #region
        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            Status.TeamColor = PanelLabel.Replace(" ", "");
            Debug.WriteLine(Status.TeamColor);
            if (Status.TeamColor.Contains("Red")) {
                Status.HPBarColor = Master.HPBarColorEnum.RED;
                Status.DamagePanelColor = Master.DamagePanelColorEnum.RED;
            } else {
                Status.HPBarColor = Master.HPBarColorEnum.BLUE;
                Status.DamagePanelColor = Master.DamagePanelColorEnum.BLUE;
            }

            TeamNameComboBox.SelectedIndex = 0;

            this.IsEnabled = false;
            //RespawnButton.IsEnabled = false;
            //DefeatButton.IsEnabled = false;
            //PunishButton.IsEnabled = false;
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            int maxHp = Master.Instance.MaxHP;
            if (this.IsEnabled) {
                if (Master.Instance.GameFormat == Master.GameFormatEnum.PRELIMINALY) {
                    if (Status.TeamColor.Contains("Red")) maxHp = Master.Instance.PreRedMaxHP;
                    else maxHp = Master.Instance.PreBlueMaxHP;
                }
                Status.MaxHP = maxHp;
                Status.HP = maxHp;
                HPBar.Opacity = 1;
            } else {
                Status.HP = 0;
                HPBar.Opacity = 0.5;
            }
            Status.MaxHP = maxHp;
        }

        private void Reset() {
            Status.HP = Status.MaxHP;
            Status.DefeatedFlag = false;
            Status.PowerOnFlag = true;
            Status.InvincibilityFlag = false;
            Status.RespawnTime = TimeSpan.Zero;
            Status.DamageTaken = 0;
            Status.DefeatedNum = 0;
            Status.Log.Clear();

            _respawnTimer.Stop();
            _invincibilityTimer.Stop();
        }
        #endregion

        /* ボタン等のイベント ****************************************************************************************************************************************/
        private void TeamNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Status.TeamName = TeamNameComboBox.SelectedItem.ToString();
            Status.TeamID = TeamNameComboBox.SelectedIndex;
        }

        private void RespawnButton_Click(object sender, RoutedEventArgs e) {
            Status.HP = Master.Instance.RespawnHP;
            Status.DefeatedFlag = false;
            Status.PowerOnFlag = true;
            Status.RespawnTime = TimeSpan.Zero;
            Status.InvincibilityFlag = true;
            _respawnTimer.Stop();

            StartInvincibilityTimer();
        }

        private void DefeatButton_Click(object sender, RoutedEventArgs e) {
            Status.HP = 0;
            Status.DefeatedFlag = true;
            Status.PowerOnFlag = false;
            // Status.DefeatedNum++;
            Status.AddRobotLog("Defeated by Host");

            // 基本的にこのボタンはロボットが倒れるなどして再起不能になった時に押すので必要ない
            // if (Master.Instance.GameFormat != Master.GameFormatEnum.PRELIMINALY)
            //    StartRespawnTimer();
        }

        public void PunishButton_Click(object sender, RoutedEventArgs e) {
            int attackBuff = 1;
            if (Status.TeamColor.Contains("Red")) attackBuff = Master.Instance.BlueAttackBuff;
            else attackBuff = Master.Instance.RedAttackBuff;

            Status.HP -= attackBuff * Master.Instance.PenaltyDamage;
            Status.DamageTaken += attackBuff * Master.Instance.PenaltyDamage;

            if (Status.HP <= 0) {
                Status.HP = 0;
                Status.DefeatedFlag = true;
                Status.PowerOnFlag = false;
                Status.DefeatedNum++;
                Status.AddRobotLog("Defeated");

                if (Master.Instance.GameFormat != Master.GameFormatEnum.PRELIMINALY)
                    StartRespawnTimer();
            }
        }

        /* タイマーの開始とイベント ****************************************************************************************************************************************/
        #region
        public void StartRespawnTimer() {
            _remainingTime = TimeSpan.FromSeconds(Master.Instance.RespawnTime);
            _startTime = DateTime.Now;
            _respawnTimer.Start();
            Dispatcher.Invoke(() => {
                RespawnTimeTextBlock.IsEnabled = true;
            });
        }

        private void OnRespawnTimedEvent(object source, ElapsedEventArgs e) {
            var timePassed = DateTime.Now - _startTime;
            Status.RespawnTime = _remainingTime - timePassed;

            if (Status.RespawnTime.TotalSeconds <= 0) {
                _respawnTimer.Stop();

                Status.HP = Master.Instance.RespawnHP;
                Status.DefeatedFlag = false;
                Status.PowerOnFlag = true;
                Status.InvincibilityFlag = true;

                StartInvincibilityTimer();
            }
        }

        public void StartInvincibilityTimer() {
            _remainingTime = TimeSpan.FromSeconds(Master.Instance.InvincibleTime);
            _startTime = DateTime.Now;
            _invincibilityTimer.Start();
        }

        private void OnInvincibilityTimedEvent(object? sender, ElapsedEventArgs e) {
            var timePassed = DateTime.Now - _startTime;
            var currentRemainingTime = _remainingTime - timePassed;

            if (currentRemainingTime.TotalSeconds <= 0) {
                _invincibilityTimer.Stop();
                Status.InvincibilityFlag = false;
            }
        }
        #endregion
    }
}
