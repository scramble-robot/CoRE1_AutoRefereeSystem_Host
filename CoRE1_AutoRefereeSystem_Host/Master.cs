using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Timers;
using System.Windows;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Net;


namespace CoRE1_AutoRefereeSystem_Host
{
    public class Master {
        // シングルトン
        private static readonly Lazy<Master> _instance = new Lazy<Master>(() => new Master());
        public static Master Instance => _instance.Value;

        // 出場チーム
        public string[] TeamName = {
            "",
            "[AGA]AGA'star.",
            "[AGSR]AGSR",
            "[AVNT]AVANT",
            "[KNIT]KINKI KNIGHTS",
            "[MKNG]MA-KING",
            "[RTUS]Ro.T.U.S",
            "[SEC]SETAGAYA Eclipse",
            "[TMC]Tactical Majestic Creators",
            "[TKG]TKG",
            "[TRU]Tohoku Roboconist Union",
            "[CNTN]こんてにゅ～",
            "[DTB]でんとつーとびーばー",
            "[TSTM]チーム薩摩",
            "[KSHH]機襲藩",
            "[KSKN]香創研",
            "[DRBS]大ロボーズ",
            "[RISN]雷閃"
        };


        public Dictionary<string, int> TeamNodeNo = new Dictionary<string, int> {};

        public Dictionary<string, int> HostCH = new Dictionary<string, int> {};

        /***** 試合のルール *******************************************************************************************************/

        public int GameTimeMin { private set; get; } = 5;
        public int MaxHP { private set; get; } = 40;
        public int MaxHPBuilder { private set; get; } = 80;
        public int MaxHPAutoTurret { private set; get; } = 200;
        public int PreGameTimeMin { private set; get; } = 2;

        public int PreRedMaxHP { private set; get; } = 100;  // 予選の赤（攻撃サイド）のMaxHP

        public int PreBlueMaxHP { private set; get; } = 20;  // 予選の青（迎撃サイド）のMaxHP

        public int HitDamage { private set; get; } = 10;

        public int AttackBuff12Time { private set; get; } = 40;

        public int AttackBuff45Time { private set; get; } = 60;

        public int Zone1ShieldBuffTime { private set; get; } = 30;

        public int Zone2ShieldBuffTime { private set; get; } = 45;

        public int PenaltyDamage { private set; get; } = 10;
        public int RespawnTime { private set; get; } = 60;
        public int RespawnHP { private set; get; } = 30;
        public int RespawnHPBuilder { private set; get; } = 60;
        public int InvincibleTime { private set; get; } = 5;

        public int BaseNeutralPointTime { private set; get; } = 30;
        public int BaseInvulnerableTime { private set; get; } = 5;


        /***** 各種フラグ *******************************************************************************************************/
        public bool GameEndFlag { set; get; } = false;
        public bool DuringGame { private set; get; } = false;

        /***** enum定義 *******************************************************************************************************/
        #region
        public enum RobotTypeEnum {
            NONE,
            ATTACKER,
            BUILDER,
            AUTOTURRET,
            STRIDER,
        }

        public enum RobotConnectionEnum {
            DISABLED,
            ENABLED,
            CONNECTED,
            // DISCONNECTED,
        }

        public enum BaseConnectionEnum {
            DISABLED,
            ENABLED,
            CONNECTED,
            // DISCONNECTED,
        }

        public enum TeamColorEnum {
            NONE,
            RED,
            GREEN,
            BLUE,
            CYAN,
            MAGENTA,
            YELLOW,
            WHITE,
        };

        public enum GameFormatEnum {
            NONE,
            PRELIMINALY,
            SEMIFINALS,
            FINALS
        };

        public enum GameStatusEnum {
            NONE,
            SETTING,
            PREGAME,
            GAME,
            POSTGAME,
        };

        public enum SettingStatusEnum {
            NONE,
            RUNNING,
            TECH_TIMEOUT,
            RESUME,
            SKIP,
        };

        public enum WinnerEnum {
            NONE,
            RED,
            BLUE,
            DRAW,
        };

        public enum ARSSequenceEnum {
            NONE,
            HARDWARE_RESET,
            SOFTWARE_RESET,
            OPENED,
            CLOSING,
            SETTING_CH,
            PING,
            BOOTING,
            UPDATING,
            SHUTING_DOWN,
            RECONNECTING,
        };

        public enum HPBarColorEnum {
            NONE,
            RED,
            GREEN,
            BLUE,
            YELLOW,
            WHITE,
        };

        public enum DamagePanelColorEnum {
            NONE,
            RED,
            GREEN,
            BLUE,
            CYAN,
            MAGENTA,
            YELLOW,
            WHITE,
            FULL_WHITE,
        };

        public enum OccupiedEnum {
            NO,
            RED,
            BLUE,
        };

        public enum StriderBuffStatusEnum {
            NONE,
            ZONE1_ACTIVE,
            ZONE1_EXPIRED,
            ZONE2_ACTIVE,
            ZONE2_EXPIRED
        }

        #endregion

        /***** 各種設定 *******************************************************************************************************/
        public GameFormatEnum GameFormat { set; get; } = GameFormatEnum.NONE;
        public GameStatusEnum GameStatus { set; get; } = GameStatusEnum.PREGAME;
        public SettingStatusEnum SettingStatus { set; get; } = SettingStatusEnum.NONE;

        public bool IsUpdatingStatus { set; get; } = false;

        public Settings SettingsJson { set; get; }

        public bool SettingsChanged { set; get; } = false;

        // ARS通信のタイムアウト設定の乱数
        public int TimeoutMin { set; get; } = 1500;
        public int TimeoutMax { set; get; } = 2500;
        public Random ARSTimeoutRandom { set; get; } = new Random();

        /***** 各種試合状況 *******************************************************************************************************/
        public string GameTime { set; get; } = "00:00";
        public string SettingTime { set; get; } = "00:00";
        public TimeSpan CurrentTime { private set; get; } = TimeSpan.Zero;
        public int TotalRedDefeated { set; get; } = 0;
        public int TotalRedDamageTaken { set; get; } = 0;
        public int TotalBlueDefeated { set; get; } = 0;
        public int TotalBlueDamageTaken { set; get; } = 0;

        public int TotalRedOccupatedBase {
            private set {; }
            get {
                int num = 0;
                Application.Current.Dispatcher.Invoke(() => {
                    var window = GetMainWindow();
                    if (window.CommonBase.Status.Occupied == OccupiedEnum.RED) num++;
                    if (window.RedBase.Status.Occupied == OccupiedEnum.RED) num++;
                });
                return num;
            }
        }

        public int TotalBlueOccupatedBase {
            private set {;}
            get {
                int num = 0;
                Application.Current.Dispatcher.Invoke(() => {
                    var window = GetMainWindow();
                    if (window.CommonBase.Status.Occupied == OccupiedEnum.BLUE) num++;
                    if (window.BlueBase.Status.Occupied == OccupiedEnum.BLUE) num++;
                });
                return num;
            }
        }

        public int TotalRedEMs {
            private set {; }
            get {
                int num = 0;
                Application.Current.Dispatcher.Invoke(() => {
                    var window = GetMainWindow();
                    if (!window.RedEMSpot1Button.IsEnabled) num++;
                    if (!window.RedEMSpot2Button.IsEnabled) num++;
                    if (!window.RedEMSpot3Button.IsEnabled) num++;
                    if (!window.RedEMSpot4Button.IsEnabled) num++;
                    if (!window.RedEMSpot5Button.IsEnabled) num++;
                });
                return num;
            }
        }

        public int TotalBlueEMs {
            private set {; }
            get {
                int num = 0;
                Application.Current.Dispatcher.Invoke(() => {
                    var window = GetMainWindow();
                    if (!window.BlueEMSpot1Button.IsEnabled) num++;
                    if (!window.BlueEMSpot2Button.IsEnabled) num++;
                    if (!window.BlueEMSpot3Button.IsEnabled) num++;
                    if (!window.BlueEMSpot4Button.IsEnabled) num++;
                    if (!window.BlueEMSpot5Button.IsEnabled) num++;
                });
                return num;
            }
        }

        public double RedAttackBuff { set; get; } = 1.0;
        public bool IsRedAttackBuff1Active { set; get; } = false;
        public bool IsRedAttackBuff2Active { set; get; } = false;

        public bool IsRedAttackBuff4Active { set; get; } = false;
        public bool IsRedAttackBuff5Active { set; get; } = false;

        public double BlueAttackBuff { set; get; } = 1.0;
        public bool IsBlueAttackBuff1Active { set; get; } = false;
        public bool IsBlueAttackBuff2Active { set; get; } = false;
        public bool IsBlueAttackBuff4Active { set; get; } = false;
        public bool IsBlueAttackBuff5Active { set; get; } = false;

        public bool RedHealing {  set; get; } = false;
        public bool BlueHealing { set; get; } = false;

        public bool RedInvinsible { set; get; } = false;
        public bool IsRedZone1ShieldBuffActive { set; get; } = false;
        public bool IsRedZone2ShieldBuffActiveWaiting { set; get; } = false;
        public bool IsRedZone2ShieldBuffActive { set; get; } = false;

        public bool BlueInvinsible { set; get; } = false;
        public bool IsBlueZone1ShieldBuffActive { set; get; } = false;
        public bool IsBlueZone2ShieldBuffActiveWaiting { set; get; } = false;
        public bool IsBlueZone2ShieldBuffActive { set; get; } = false;

        public WinnerEnum Winner { set; get; } = WinnerEnum.NONE;

        // 操縦画面用プログラムに送信するためのクラス
        public CoreClass Msgs = new CoreClass();
        private const int _sendPort = 12345;
        private UdpClient _operatorUdpSender = new UdpClient();

        //
        private const int _recievePort = 8888;
        private UdpClient _buffUdpReciever = new UdpClient(_recievePort);
        private Dictionary<IPAddress, string> _latestDataByIp = new();
        private IPEndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public static MainWindow GetMainWindow() {
            return Application.Current.MainWindow as MainWindow;
        }

        // サーバー全体の更新
        // 100msの間隔で通信を試みる
        // ただし実際には500ms程度はかかる
        private static System.Timers.Timer _updateTimer;
        public event Action UpdateEvent;

        // 強化素材による攻撃バフの時間の測定用
        public DateTime _redAttackBuff1StartTime;
        public DateTime _redAttackBuff2StartTime;
        public DateTime _redAttackBuff4StartTime;
        public DateTime _redAttackBuff5StartTime;
        public DateTime _blueAttackBuff1StartTime;
        public DateTime _blueAttackBuff2StartTime;
        public DateTime _blueAttackBuff4StartTime;
        public DateTime _blueAttackBuff5StartTime;

        // ストライダーのゾーン踏破によるバフの時間測定用
        public DateTime _redZone1ShieldBuffStartTime;
        public DateTime _redZone2ShieldBuffStartTime;
        public DateTime _blueZone1ShieldBuffStartTime;
        public DateTime _blueZone2ShieldBuffStartTime;


        // 試合時間のタイマー関係
        private static System.Timers.Timer _countDownTimer;
        private static DateTime _startTime;
        private static TimeSpan _remainingTime;

        // Settingsのタイマー
        public static System.Timers.Timer _settingsTimer;

        // UDPのタイマー
        public readonly DispatcherTimer _udpSenderTimer;
        public readonly DispatcherTimer _udpReceiverTimer;

        private Master() {
            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 100;
            _updateTimer.Elapsed += UpdateAttackBuff;
            _updateTimer.Elapsed += UpdateStriderBuff;
            _updateTimer.Elapsed += UpdateBaseActive;
            _updateTimer.Elapsed += OnEventArrived;
            _updateTimer.Elapsed += AggregateDamage;
            _updateTimer.Elapsed += CheckGameEnd;
            _updateTimer.Start();

            _countDownTimer = new System.Timers.Timer();
            _countDownTimer.Interval = 50;
            _countDownTimer.Elapsed += OnCountDownTimedEvent;

            _settingsTimer = new System.Timers.Timer();
            _settingsTimer.Interval = 1000;
            _settingsTimer.Elapsed += SaveSettings;
            _settingsTimer.Start();

            _udpSenderTimer = new DispatcherTimer();
            _udpSenderTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _udpSenderTimer.Tick += new EventHandler(SendMsgsToOperatorScreen);
            _udpSenderTimer.Start();

            _udpReceiverTimer = new DispatcherTimer();
            _udpReceiverTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            _udpReceiverTimer.Tick += new EventHandler(ReceiveAndTriggerBuff);
            _udpReceiverTimer.Start();
        }

        private void OnEventArrived(object sender, EventArgs e) {
            UpdateEvent?.Invoke();
        }

        private void AggregateDamage(object sender, EventArgs e) {
            Application.Current.Dispatcher.Invoke((() => {
                var window = GetMainWindow();

                Instance.TotalRedDamageTaken = (
                    window.Red12.Robot1.Status.DamageTaken
                    + window.Red12.Robot2.Status.DamageTaken
                    + window.Red34.Robot1.Status.DamageTaken
                    + window.Red34.Robot2.Status.DamageTaken
                    + window.Red5.Robot1.Status.DamageTaken
                    + window.Red6.Robot1.Status.DamageTaken
                );

                Instance.TotalBlueDamageTaken = (
                    window.Blue12.Robot1.Status.DamageTaken
                    + window.Blue12.Robot2.Status.DamageTaken
                    + window.Blue34.Robot1.Status.DamageTaken
                    + window.Blue34.Robot2.Status.DamageTaken
                    + window.Blue5.Robot1.Status.DamageTaken
                    + window.Blue6.Robot1.Status.DamageTaken
                );

                Instance.TotalRedDefeated = (
                    window.Red12.Robot1.Status.DefeatedNum
                    + window.Red12.Robot2.Status.DefeatedNum
                    + window.Red34.Robot1.Status.DefeatedNum
                    + window.Red34.Robot2.Status.DefeatedNum
                    + window.Red5.Robot1.Status.DefeatedNum
                    //+ window.Red6.Robot1.Status.DefeatedNum オートタレットは対象外
                );

                Instance.TotalBlueDefeated = (
                    window.Blue12.Robot1.Status.DefeatedNum
                    + window.Blue12.Robot2.Status.DefeatedNum
                    + window.Blue34.Robot1.Status.DefeatedNum
                    + window.Blue34.Robot2.Status.DefeatedNum
                    + window.Blue5.Robot1.Status.DefeatedNum
                    //+ window.Blue6.Robot1.Status.DefeatedNum オートタレットは対象外
                );

                window.RedDamageTakenTextBlock.Text = $"{Instance.TotalRedDamageTaken:0000}";
                window.BlueDamageTakenTextBlock.Text = $"{Instance.TotalBlueDamageTaken:0000}";
                window.RedDefeatedTextBlock.Text = $"{Instance.TotalRedDefeated:0000}";
                window.BlueDefeatedTextBlock.Text = $"{Instance.TotalBlueDefeated:0000}";

                window.RedAttackBuffTextBlock.Text = $"x{Instance.RedAttackBuff:0}";
                window.BlueAttackBuffTextBlock.Text = $"x{Instance.BlueAttackBuff:0}";
            }));
        }

        private void UpdateAttackBuff(object sender, EventArgs e) {
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY) return;

            Application.Current.Dispatcher.Invoke(() => {
                var window = GetMainWindow();
                int buff = 1;
                if (Instance.IsRedAttackBuff1Active) {
                    var timePassed = DateTime.Now - Instance._redAttackBuff1StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff12Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedAttackBuff1Active = false;
                        window.RedAttackbuff1TimeTextBlock.Text = "";
                        window.RedAttackbuff1TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.RedAttackbuff1TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsRedAttackBuff2Active) {
                    var timePassed = DateTime.Now - Instance._redAttackBuff2StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff12Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedAttackBuff2Active = false;
                        window.RedAttackbuff2TimeTextBlock.Text = "";
                        window.RedAttackbuff2TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.RedAttackbuff2TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsRedAttackBuff4Active) {
                    var timePassed = DateTime.Now - Instance._redAttackBuff4StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff45Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedAttackBuff4Active = false;
                        window.RedAttackbuff4TimeTextBlock.Text = "";
                        window.RedAttackbuff4TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.RedAttackbuff4TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsRedAttackBuff5Active) {
                    var timePassed = DateTime.Now - Instance._redAttackBuff5StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff45Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedAttackBuff5Active = false;
                        window.RedAttackbuff5TimeTextBlock.Text = "";
                        window.RedAttackbuff5TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.RedAttackbuff5TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }
                Instance.RedAttackBuff = buff;

                buff = 1;
                if (Instance.IsBlueAttackBuff1Active) {
                    var timePassed = DateTime.Now - Instance._blueAttackBuff1StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff12Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueAttackBuff1Active = false;
                        window.BlueAttackbuff1TimeTextBlock.Text = "";
                        window.BlueAttackbuff1TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.BlueAttackbuff1TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsBlueAttackBuff2Active) {
                    var timePassed = DateTime.Now - Instance._blueAttackBuff2StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff12Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueAttackBuff2Active = false;
                        window.BlueAttackbuff2TimeTextBlock.Text = "";
                        window.BlueAttackbuff2TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.BlueAttackbuff2TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsBlueAttackBuff4Active) {
                    var timePassed = DateTime.Now - Instance._blueAttackBuff4StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff45Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueAttackBuff4Active = false;
                        window.BlueAttackbuff4TimeTextBlock.Text = "";
                        window.BlueAttackbuff4TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.BlueAttackbuff4TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsBlueAttackBuff5Active) {
                    var timePassed = DateTime.Now - Instance._blueAttackBuff5StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuff45Time) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueAttackBuff5Active = false;
                        window.BlueAttackbuff5TimeTextBlock.Text = "";
                        window.BlueAttackbuff5TimeTextBlock.IsEnabled = false;
                    } else {
                        buff *= 2;
                        window.BlueAttackbuff5TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }
                Instance.BlueAttackBuff = buff;
                                
                window.RedAttackBuffTextBlock.Text = $"x{Instance.RedAttackBuff:0}";
                window.BlueAttackBuffTextBlock.Text = $"x{Instance.BlueAttackBuff:0}";
            });
        }

        private void UpdateStriderBuff(object sender, EventArgs e) {
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY) return;

            Application.Current.Dispatcher.Invoke(() => {
                var window = GetMainWindow();
                if (Instance.IsRedZone1ShieldBuffActive) {
                    var timePassed = DateTime.Now - Instance._redZone1ShieldBuffStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.Zone1ShieldBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedZone1ShieldBuffActive = false;
                        window.RedZone1TimeTextBlock.Text = "";
                        window.RedZone1TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.RedInvinsible = true;
                        window.RedZone1TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsRedZone2ShieldBuffActiveWaiting) {
                    if (!Instance.IsRedZone1ShieldBuffActive) {
                        Instance.IsRedZone2ShieldBuffActiveWaiting = false;

                        Instance.IsRedZone2ShieldBuffActive = true;
                        Instance._redZone2ShieldBuffStartTime = DateTime.Now;
                    } else {
                        window.RedZone1TimeTextBlock.Text = $"waiting...";
                    }
                }

                if (Instance.IsRedZone2ShieldBuffActive) {
                    var timePassed = DateTime.Now - Instance._redZone2ShieldBuffStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.Zone2ShieldBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedZone2ShieldBuffActive = false;
                        window.RedZone2TimeTextBlock.Text = "";
                        window.RedZone2TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.RedInvinsible = true;
                        window.RedZone2TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsBlueZone1ShieldBuffActive) {
                    var timePassed = DateTime.Now - Instance._blueZone1ShieldBuffStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.Zone1ShieldBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueZone1ShieldBuffActive = false;
                        window.BlueZone1TimeTextBlock.Text = "";
                        window.BlueZone1TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.BlueInvinsible = true;
                        window.BlueZone1TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsBlueZone2ShieldBuffActiveWaiting) {
                    if (!Instance.IsBlueZone1ShieldBuffActive) {
                        Instance.IsBlueZone2ShieldBuffActiveWaiting = false;

                        Instance.IsBlueZone2ShieldBuffActive = true;
                        Instance._blueZone2ShieldBuffStartTime = DateTime.Now;
                    } else {
                        window.BlueZone1TimeTextBlock.Text = $"waiting...";
                    }
                }

                if (Instance.IsBlueZone2ShieldBuffActive) {
                    var timePassed = DateTime.Now - Instance._blueZone2ShieldBuffStartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.Zone2ShieldBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueZone2ShieldBuffActive = false;
                        window.BlueZone2TimeTextBlock.Text = "";
                        window.BlueZone2TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.BlueInvinsible = true;
                        window.BlueZone2TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }
            });
        }

        private void UpdateBaseActive(object sender, EventArgs e) {
            if (!Instance.DuringGame) return;
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY) return;

            Application.Current.Dispatcher.Invoke(() => {
                var window = GetMainWindow();
                window.CommonBase.Status.IsRedActive = window.Red6.Robot1.Status.DefeatedFlag;
                window.CommonBase.Status.IsBlueActive = window.Blue6.Robot1.Status.DefeatedFlag;
                window.RedBase.Status.IsActive = window.Red6.Robot1.Status.DefeatedFlag;
                window.BlueBase.Status.IsActive = window.Blue6.Robot1.Status.DefeatedFlag;
            });
        }

        // 試合時間中に勝敗が決定しているか確認
        private void CheckGameEnd(object sender, EventArgs e) {
        //private void CheckGameEnd() {
            if (!Instance.DuringGame) return;

            // 予選の終了条件
            // 攻撃サイドのロボットが撃破される
            // 迎撃サイドのすべてのロボットが撃破される
            // 上記2条件に当てはまらず、2分間が経過する
            Application.Current.Dispatcher.Invoke(() => {
                var window = GetMainWindow();

                if (Instance.GameFormat == GameFormatEnum.PRELIMINALY) {
                    if (window.Red12.Robot1.Status.DefeatedFlag) { // 攻撃サイドが撃破
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.BLUE;
                        window.TimerLabel.Text = "BLUE WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    } else if (window.Blue12.Robot1.Status.DefeatedFlag &&
                               window.Blue12.Robot2.Status.DefeatedFlag &&
                               window.Blue34.Robot1.Status.DefeatedFlag) { // 迎撃サイドがすべて撃破
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }
                }

                // 準決勝・決勝の勝敗条件（ここでは条件1のみ確認）
                // 1. 共通陣地および相手陣地の両方を同時刻に占拠状態とした同盟の勝利
                // 2. 相手アタッカーおよびビルダーを撃破した回数が多い同盟の勝利
                // 3. 5分経過時に陣地を多く占拠している同盟の勝利
                // 4. 5分経過時にスポットをより多く獲得した同盟の勝利
                // 5. 相手同盟への与ダメージが多い同盟の勝利
                // 6. 以上の条件で決定できない場合、引き分けとなり当該ラウンドは再試合となる

                else {
                    // 条件１
                    if (window.CommonBase.Status.Occupied == OccupiedEnum.RED
                        && window.RedBase.Status.Occupied == OccupiedEnum.RED) { // 赤が占拠
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }
                    else if (window.CommonBase.Status.Occupied == OccupiedEnum.BLUE
                             && window.BlueBase.Status.Occupied == OccupiedEnum.BLUE) { // 青が占拠
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.BLUE;
                        window.TimerLabel.Text = "BLUE WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }
                }
            });
        }

        public event Action GameStartEvent;
        /// <summary>
        /// GAME STARTボタンが押されると更新タイマーが開始
        /// また，ゲームスタートイベントが発生
        /// </summary>
        public void GameStart() {
            GetMainWindow().TimerLabel.Text = "GAME TIME";
            Instance.GameStatus = GameStatusEnum.GAME;
            Instance.DuringGame = true;
            //_updateTimer.Start();
            int gameTime = Instance.GameTimeMin;
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY) gameTime = Instance.PreGameTimeMin;
            StartTimer(gameTime * 60 + 5 + 1);
            GameStartEvent?.Invoke();
            AllocateButton();
        }


        public event Action GameResetEvent;
        /// <summary>
        /// GAME RESETボタンが押されると更新タイマーが停止
        /// また，ゲームリセットイベントが発生
        /// </summary>
        public void GameReset() {
            GetMainWindow().TimerLabel.Text = "GAME READY?";
            Instance.GameStatus = GameStatusEnum.PREGAME;
            Instance.DuringGame = false;
            //_updateTimer.Stop();

            int gameTime = Instance.GameTimeMin;
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY)
                gameTime = Instance.PreGameTimeMin;
            ResetTimer(gameTime * 60);
            GameResetEvent?.Invoke();
            AllocateButton();
        }

        private static void StartTimer(double timeSec) {
            _remainingTime = TimeSpan.FromSeconds(timeSec);
            _startTime = DateTime.Now;
            _countDownTimer.Start();
        }

        private static void ResetTimer(int timeSec = 0) {
            _countDownTimer.Stop();

            string timeText = $"{timeSec / 60:D2}:{timeSec % 60:D2}";
            GetMainWindow().GameCountDown.Text = timeText;
            Instance.GameTime = timeText;
            Instance.SettingTime = timeText;
        }

        private static void OnCountDownTimedEvent(object source, ElapsedEventArgs e) {
            Instance.CurrentTime = DateTime.Now - _startTime;
            var currentRemainingTime = _remainingTime - Instance.CurrentTime;
            if (Instance.GameEndFlag || currentRemainingTime.TotalSeconds <= 0) {
                _countDownTimer.Stop();
                if (Instance.GameEndFlag) return;

                Instance.GameStatus += 1;
                if (Instance.GameStatus == GameStatusEnum.POSTGAME
                    && Instance.GameFormat != GameFormatEnum.PRELIMINALY) {

                    // 決勝トーナメントにおけるラウンドの勝敗条件2以降
                    // 条件2 相手アタッカーおよびビルダーを撃破した回数が多い同盟の勝利
                    if (Instance.TotalRedDefeated != Instance.TotalBlueDefeated) {
                        if (Instance.TotalRedDefeated > Instance.TotalBlueDefeated) {
                            Instance.Winner = WinnerEnum.BLUE;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "BLUE WINS!!!";
                            });
                            Instance.DuringGame = false;
                        } else {
                            Instance.Winner = WinnerEnum.RED;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "RED WINS!!!";
                            });
                            Instance.DuringGame = false;
                        }
                    }

                    // 条件3 5分経過時に陣地を多く占拠している同盟の勝利
                    else if (Instance.TotalRedOccupatedBase != Instance.TotalBlueOccupatedBase) {
                        if (Instance.TotalRedOccupatedBase > Instance.TotalBlueOccupatedBase) {
                            Instance.Winner = WinnerEnum.RED;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "RED WINS!!!";
                            });
                            Instance.DuringGame = false;
                        } else {
                            Instance.Winner = WinnerEnum.BLUE;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "BLUE WINS!!!";
                            });
                            Instance.DuringGame = false;
                        }
                    }

                    // 条件4 5分経過時にスポットをより多く獲得した同盟の勝利
                    else if (Instance.TotalRedEMs != Instance.TotalBlueEMs) {
                        if (Instance.TotalRedEMs > Instance.TotalBlueEMs) {
                            Instance.Winner = WinnerEnum.RED;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "RED WINS!!!";
                            });
                            Instance.DuringGame = false;
                        } else {
                            Instance.Winner = WinnerEnum.BLUE;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "BLUE WINS!!!";
                            });
                            Instance.DuringGame = false;
                        }
                    }

                    // 条件5 相手同盟への与ダメージが多い同盟の勝利
                    else if (Instance.TotalRedDamageTaken != Instance.TotalBlueDamageTaken) {
                        if (Instance.TotalRedDamageTaken > Instance.TotalBlueDamageTaken) {
                            Instance.Winner = WinnerEnum.BLUE;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "BLUE WINS!!!";
                            });
                            Instance.DuringGame = false;
                        } else {
                            Instance.Winner = WinnerEnum.RED;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "RED WINS!!!";
                            });
                            Instance.DuringGame = false;
                        }
                    }

                    // 勝敗条件を満たさない
                    else {
                        Instance.Winner = WinnerEnum.DRAW;
                        Application.Current.Dispatcher.Invoke(() => {
                            GetMainWindow().TimerLabel.Text = "DRAW!!!";
                        });
                        Instance.DuringGame = false;
                    }
                }

                AllocateButton();
                Instance.DuringGame = false;
                return;
            }

            string timeText = $"{currentRemainingTime.Minutes:00}:{currentRemainingTime.Seconds:00}";
            Application.Current.Dispatcher.Invoke(() => {
                GetMainWindow().GameCountDown.Text = timeText;
            });
            Instance.GameTime = timeText;
            Instance.SettingTime = timeText;
        }

        private static void AllocateButton() {
            Application.Current.Dispatcher.Invoke((Delegate)(() => {
                var window = GetMainWindow();
                if (Instance.GameStatus == GameStatusEnum.NONE
                    || Instance.GameStatus == GameStatusEnum.POSTGAME) {
                    window.PreliminaryRadioButton.IsEnabled = true;
                    window.GameStartButton.IsEnabled = true;
                    window.GameResetButton.IsEnabled = false;
                } else if (Instance.GameStatus == GameStatusEnum.SETTING) {
                    if (Instance.SettingStatus == SettingStatusEnum.RUNNING) {
                        window.PreliminaryRadioButton.IsEnabled = false;
                        window.GameStartButton.IsEnabled = false;
                        window.GameResetButton.IsEnabled = false;
                    } else if (Instance.SettingStatus == SettingStatusEnum.TECH_TIMEOUT) {
                        window.PreliminaryRadioButton.IsEnabled = false;
                        window.GameStartButton.IsEnabled = false;
                        window.GameResetButton.IsEnabled = false;
                    }
                } else if (Instance.GameStatus == GameStatusEnum.PREGAME) {
                    window.PreliminaryRadioButton.IsEnabled = false;
                    window.GameStartButton.IsEnabled = true;
                    window.GameResetButton.IsEnabled = false;
                } else if (Instance.GameStatus == GameStatusEnum.GAME) {
                    window.PreliminaryRadioButton.IsEnabled = false;
                    window.GameStartButton.IsEnabled = false;
                    window.GameResetButton.IsEnabled = true;
                }
            }));
        }


        private static void SaveSettings(object sender, EventArgs e) {
            if (!Instance.SettingsChanged) return;

            Settings settings = new Settings();
            settings.GameFormat = Instance.GameFormat;

            // HACK
            if (Instance.SettingsJson is null) {
                settings.NumRedWins = 0;
                settings.NumBlueWins = 0;
            } else {
                settings.NumRedWins = Instance.SettingsJson.NumRedWins;
                settings.NumBlueWins = Instance.SettingsJson.NumBlueWins;
            }

            //Application.Current.Dispatcher.Invoke((Delegate)(() => {
            Application.Current.Dispatcher.InvokeAsync(() => {
                var window = GetMainWindow();
                settings.Red1TeamName = window.Red12.Robot1.Status.TeamName;
                settings.Red2TeamName = window.Red12.Robot2.Status.TeamName;
                settings.Red3TeamName = window.Red34.Robot1.Status.TeamName;
                settings.Red4TeamName = window.Red34.Robot2.Status.TeamName;
                settings.Red5TeamName = window.Red5.Robot1.Status.TeamName;
                settings.Red6TeamName = window.Red6.Robot1.Status.TeamName;

                settings.Blue1TeamName = window.Blue12.Robot1.Status.TeamName;
                settings.Blue2TeamName = window.Blue12.Robot2.Status.TeamName;
                settings.Blue3TeamName = window.Blue34.Robot1.Status.TeamName;
                settings.Blue4TeamName = window.Blue34.Robot2.Status.TeamName;
                settings.Blue5TeamName = window.Blue5.Robot1.Status.TeamName;
                settings.Blue6TeamName = window.Blue6.Robot1.Status.TeamName;

                settings.Red1RobotType = window.Red12.Robot1.Status.RobotType;
                settings.Red2RobotType = window.Red12.Robot2.Status.RobotType;
                settings.Red3RobotType = window.Red34.Robot1.Status.RobotType;
                settings.Red4RobotType = window.Red34.Robot2.Status.RobotType;
                settings.Red5RobotType = window.Red5.Robot1.Status.RobotType;
                settings.Red6RobotType = window.Red6.Robot1.Status.RobotType;

                settings.Blue1RobotType = window.Blue12.Robot1.Status.RobotType;
                settings.Blue2RobotType = window.Blue12.Robot2.Status.RobotType;
                settings.Blue3RobotType = window.Blue34.Robot1.Status.RobotType;
                settings.Blue4RobotType = window.Blue34.Robot2.Status.RobotType;
                settings.Blue5RobotType = window.Blue5.Robot1.Status.RobotType;
                settings.Blue6RobotType = window.Blue6.Robot1.Status.RobotType;

                settings.Red12ComPort = window.Red12.ComPortSelectionComboBox.SelectedItem;
                settings.Red34ComPort = window.Red34.ComPortSelectionComboBox.SelectedItem;
                settings.Red5ComPort = window.Red5.ComPortSelectionComboBox.SelectedItem;
                settings.Blue12ComPort = window.Blue12.ComPortSelectionComboBox.SelectedItem;
                settings.Blue34ComPort = window.Blue34.ComPortSelectionComboBox.SelectedItem;
                settings.Blue5ComPort = window.Blue5.ComPortSelectionComboBox.SelectedItem;

                settings.Red6EndPoint = window.Red6.EndPointTextBox.Text;
                settings.Blue6EndPoint = window.Red6.EndPointTextBox.Text;
                settings.RedBaseEndPoint = window.RedBase.EndPointTextBox.Text;
                settings.BlueBaseEndPoint = window.Red6.EndPointTextBox.Text;
                settings.CommonBaseEndPoint = window.CommonBase.EndPointTextBox.Text;

                settings.BuffHost = window.BuffHostTextBox.Text;
            });

            try {
                SettingsManager.Instance.SaveSettings(settings);
            } catch (BusyException ex) {
                ;
            } catch (Exception ex) {
                ;
            }

            Instance.SettingsChanged = false;
        }


        private int isSending = 0;
        private async void SendMsgsToOperatorScreen(object sender, EventArgs e) {
            if (Interlocked.CompareExchange(ref isSending, 1, 0) != 0) return;

            // 指定のクラスにセット
            Msgs.GameTime = Instance.GameTime;
            Msgs.GameSystem = (int)Instance.GameFormat;
            Msgs.RedDeathCnt = Instance.TotalRedDefeated;
            Msgs.BlueDeathCnt = Instance.TotalBlueDefeated;
            Msgs.RedReceivedDamage = Instance.TotalRedDamageTaken;
            Msgs.BlueReceivedDamage = Instance.TotalBlueDamageTaken;

            // 強化素材
            Msgs.RedSpot[0] = Bool2Int(Instance.IsRedAttackBuff1Active);
            Msgs.RedSpot[1] = Bool2Int(Instance.IsRedAttackBuff2Active);
            Msgs.RedSpot[2] = Bool2Int(Instance.RedHealing);
            Msgs.RedSpot[3] = Bool2Int(Instance.IsRedAttackBuff4Active);
            Msgs.RedSpot[4] = Bool2Int(Instance.IsRedAttackBuff5Active);

            Msgs.BlueSpot[0] = Bool2Int(Instance.IsBlueAttackBuff1Active);
            Msgs.BlueSpot[1] = Bool2Int(Instance.IsBlueAttackBuff2Active);
            Msgs.BlueSpot[2] = Bool2Int(Instance.BlueHealing);
            Msgs.BlueSpot[3] = Bool2Int(Instance.IsBlueAttackBuff4Active);
            Msgs.BlueSpot[4] = Bool2Int(Instance.IsBlueAttackBuff5Active);

            // 陣地
            var window = GetMainWindow();
            Msgs.RedArea = 10 - (window.RedBase.Status.OccupationLevel + 5);
            Msgs.CenterArea = window.CommonBase.Status.OccupationLevel;
            Msgs.BlueArea = 10 - (window.BlueBase.Status.OccupationLevel + 5);

            // 試合結果
            Msgs.RedWin = 0;
            Msgs.BlueWin = 0;
            Msgs.Winner = (uint)Instance.Winner;

            // Robot
            RobotClass redAutoRobot = new RobotClass();
            RobotClass blueAutoRobot = new RobotClass();
            redAutoRobot.TeamColor = "Red6";  redAutoRobot.TeamID = 18;
            blueAutoRobot.TeamColor = "Blue6"; blueAutoRobot.TeamID = 18;


            RobotStatusManager.RobotStatus[] AllRobotStatus = {
                window.Red12.Robot1.Status,
                window.Red12.Robot2.Status,
                window.Red34.Robot1.Status,
                window.Red34.Robot2.Status,
                window.Red5.Robot1.Status,
                window.Blue12.Robot1.Status,
                window.Blue12.Robot2.Status,
                window.Blue34.Robot1.Status,
                window.Blue34.Robot2.Status,
                window.Blue5.Robot1.Status
            };

            for (int i = 0; i < AllRobotStatus.Length; i++) {
                Msgs.Robot[i].TeamID = AllRobotStatus[i].TeamID;
                Msgs.Robot[i].TeamColor = AllRobotStatus[i].TeamColor;
                Msgs.Robot[i].HP = AllRobotStatus[i].HP;
                Msgs.Robot[i].MaxHP = AllRobotStatus[i].MaxHP;
                if (Instance.GameFormat == GameFormatEnum.PRELIMINALY && AllRobotStatus[i].DefeatedFlag)
                    Msgs.Robot[i].DeathFlag = 2;
                else if (Instance.GameFormat != GameFormatEnum.PRELIMINALY && AllRobotStatus[i].DefeatedFlag)
                    Msgs.Robot[i].DeathFlag = 1;
                else
                    Msgs.Robot[i].DeathFlag = 0;

                Msgs.Robot[i].RespawnTime = AllRobotStatus[i].RespawnTimeString;
            }

            Msgs.Robot[10] = redAutoRobot;
            Msgs.Robot[11] = blueAutoRobot;

            // UDPでデータを送信
            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _operatorUdpSender.SendAsync(data, data.Length, "192.168.100.100", _sendPort);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _operatorUdpSender.SendAsync(data, data.Length, "192.168.100.101", _sendPort);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _operatorUdpSender.SendAsync(data, data.Length, "192.168.100.102", _sendPort);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _operatorUdpSender.SendAsync(data, data.Length, "192.168.100.103", _sendPort);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            Interlocked.Exchange(ref isSending, 0);
        }

        private int isParsing = 0;
        private async void ReceiveAndTriggerBuff(object sender, EventArgs e) {
            if (Interlocked.CompareExchange(ref isParsing, 1, 0) != 0) return;

            try {
                while (_buffUdpReciever.Available > 0) {
                    byte[] receivedBytes = _buffUdpReciever.Receive(ref _remoteEndPoint);
                    string receivedData = Encoding.UTF8.GetString(receivedBytes);
                    Debug.WriteLine(receivedData);
                    IPAddress senderIp = _remoteEndPoint.Address;
                    _latestDataByIp[senderIp] = receivedData;
                }

                // バンカー
                Application.Current.Dispatcher.Invoke(() => {
                    var window = GetMainWindow();
                    
                    // 192.168.11.230と231の値を処理（シールドバフ）
                    bool[] redShieldBuffs = new bool[5]; // 各ロボット用
                    bool[] blueShieldBuffs = new bool[5]; // 各ロボット用
                    
                    foreach (var ipString in new[] { "192.168.11.230", "192.168.11.231" }) {
                        IPAddress ip = IPAddress.Parse(ipString);
                        if (_latestDataByIp.TryGetValue(ip, out string data)) {
                            if (data.StartsWith("R") && data.Contains("B")) {
                                string redPart = data.Substring(1, 5);
                                string bluePart = data.Substring(data.IndexOf('B') + 1, 5);

                                for (int i = 0; i < 5; i++) {
                                    redShieldBuffs[i] |= (redPart[i] == '1');
                                    blueShieldBuffs[i] |= (bluePart[i] == '1');
                                }
                            }
                        }
                    }
                    window.ShieldRed1ToggleButton.IsChecked = redShieldBuffs[0];
                    window.ShieldRed2ToggleButton.IsChecked = redShieldBuffs[1];
                    window.ShieldRed3ToggleButton.IsChecked = redShieldBuffs[2];
                    window.ShieldRed4ToggleButton.IsChecked = redShieldBuffs[3];
                    window.ShieldRed5ToggleButton.IsChecked = redShieldBuffs[4];

                    window.ShieldBlue1ToggleButton.IsChecked = blueShieldBuffs[0];
                    window.ShieldBlue2ToggleButton.IsChecked = blueShieldBuffs[1];
                    window.ShieldBlue3ToggleButton.IsChecked = blueShieldBuffs[2];
                    window.ShieldBlue4ToggleButton.IsChecked = blueShieldBuffs[3];
                    window.ShieldBlue5ToggleButton.IsChecked = blueShieldBuffs[4];
                    
                    // 192.168.11.220と221の値を処理（アクティブバフ）
                    bool[] redActiveBuffs = new bool[5]; // 各ロボット用
                    bool[] blueActiveBuffs = new bool[5]; // 各ロボット用
                    
                    foreach (var ipString in new[] { "192.168.11.220", "192.168.11.221" }) {
                        IPAddress ip = IPAddress.Parse(ipString);
                        if (_latestDataByIp.TryGetValue(ip, out string data)) {
                            if (data.StartsWith("R") && data.Contains("B")) {
                                string redPart = data.Substring(1, 5);
                                string bluePart = data.Substring(data.IndexOf('B') + 1, 5);
                                
                                for (int i = 0; i < 5; i++) {
                                    redActiveBuffs[i] |= (redPart[i] == '1');
                                    blueActiveBuffs[i] |= (bluePart[i] == '1');
                                }
                            }
                        }
                    }

                    if (redActiveBuffs[0] && window.RedEMSpot1Button.IsEnabled) {
                        Instance.IsRedAttackBuff1Active = true;
                        Instance._redAttackBuff1StartTime = DateTime.Now;
                        window.RedAttackbuff1TimeTextBlock.IsEnabled = true;
                        window.RedEMSpot1Button.IsEnabled = false;
                    }

                    if (redActiveBuffs[1] && window.RedEMSpot2Button.IsEnabled) {
                        Instance.IsRedAttackBuff2Active = true;
                        Instance._redAttackBuff2StartTime = DateTime.Now;
                        window.RedAttackbuff2TimeTextBlock.IsEnabled = true;
                        window.RedEMSpot2Button.IsEnabled = false;
                    }

                    if (redActiveBuffs[2] && window.RedEMSpot3Button.IsEnabled) {
                        Instance.RedHealing = true;
                        if (!window.Red12.Robot1.Status.DefeatedFlag) window.Red12.Robot1.Status.HP = window.Red12.Robot1.Status.MaxHP;
                        if (!window.Red12.Robot2.Status.DefeatedFlag) window.Red12.Robot2.Status.HP = window.Red12.Robot2.Status.MaxHP;
                        if (!window.Red34.Robot1.Status.DefeatedFlag) window.Red34.Robot1.Status.HP = window.Red34.Robot1.Status.MaxHP;
                        if (!window.Red34.Robot2.Status.DefeatedFlag) window.Red34.Robot2.Status.HP = window.Red34.Robot2.Status.MaxHP;

                        if (Master.Instance.GameFormat == Master.GameFormatEnum.FINALS
                            && !window.Red5.Robot1.Status.DefeatedFlag)
                            window.Red5.Robot1.Status.HP = window.Red5.Robot1.Status.MaxHP;

                        window.RedEMSpot3Button.IsEnabled = false;
                    }

                    if (redActiveBuffs[3] && window.RedEMSpot4Button.IsEnabled) {
                        Instance.IsRedAttackBuff4Active = true;
                        Instance._redAttackBuff4StartTime = DateTime.Now;
                        window.RedAttackbuff4TimeTextBlock.IsEnabled = true;
                        window.RedEMSpot4Button.IsEnabled = false;
                    }

                    if (redActiveBuffs[4] && window.RedEMSpot5Button.IsEnabled) {
                        Instance.IsRedAttackBuff5Active = true;
                        Instance._redAttackBuff5StartTime = DateTime.Now;
                        window.RedAttackbuff5TimeTextBlock.IsEnabled = true;
                        window.RedEMSpot5Button.IsEnabled = false;
                    }

                    if (blueActiveBuffs[0] && window.BlueEMSpot1Button.IsEnabled) {
                        Instance.IsBlueAttackBuff1Active = true;
                        Instance._blueAttackBuff1StartTime = DateTime.Now;
                        window.BlueAttackbuff1TimeTextBlock.IsEnabled = true;
                        window.BlueEMSpot1Button.IsEnabled = false;
                    }

                    if (blueActiveBuffs[1] && window.BlueEMSpot2Button.IsEnabled) {
                        Instance.IsBlueAttackBuff2Active = true;
                        Instance._blueAttackBuff2StartTime = DateTime.Now;
                        window.BlueAttackbuff2TimeTextBlock.IsEnabled = true;
                        window.BlueEMSpot2Button.IsEnabled = false;
                    }

                    if (blueActiveBuffs[2] && window.BlueEMSpot3Button.IsEnabled) {
                        Instance.BlueHealing = true;
                        if (!window.Blue12.Robot1.Status.DefeatedFlag) window.Blue12.Robot1.Status.HP = window.Blue12.Robot1.Status.MaxHP;
                        if (!window.Blue12.Robot2.Status.DefeatedFlag) window.Blue12.Robot2.Status.HP = window.Blue12.Robot2.Status.MaxHP;
                        if (!window.Blue34.Robot1.Status.DefeatedFlag) window.Blue34.Robot1.Status.HP = window.Blue34.Robot1.Status.MaxHP;
                        if (!window.Blue34.Robot2.Status.DefeatedFlag) window.Blue34.Robot2.Status.HP = window.Blue34.Robot2.Status.MaxHP;

                        if (Master.Instance.GameFormat == Master.GameFormatEnum.FINALS
                            && !window.Blue5.Robot1.Status.DefeatedFlag)
                            window.Blue5.Robot1.Status.HP = window.Blue5.Robot1.Status.MaxHP;

                        window.BlueEMSpot3Button.IsEnabled = false;
                    }

                    if (blueActiveBuffs[3] && window.BlueEMSpot4Button.IsEnabled) {
                        Instance.IsBlueAttackBuff4Active = true;
                        Instance._blueAttackBuff4StartTime = DateTime.Now;
                        window.BlueAttackbuff4TimeTextBlock.IsEnabled = true;
                        window.BlueEMSpot4Button.IsEnabled = false;
                    }

                    if (blueActiveBuffs[4] && window.BlueEMSpot5Button.IsEnabled) {
                        Instance.IsBlueAttackBuff5Active = true;
                        Instance._blueAttackBuff5StartTime = DateTime.Now;
                        window.BlueAttackbuff5TimeTextBlock.IsEnabled = true;
                        window.BlueEMSpot5Button.IsEnabled = false;
                    }
                });
            } catch (Exception ex) {
                ;
            } finally {
                Interlocked.Exchange(ref isParsing, 0);
            }
        }

        private int Bool2Int(bool value) {
            return value ? 1 : 0;
        }
    }
}
