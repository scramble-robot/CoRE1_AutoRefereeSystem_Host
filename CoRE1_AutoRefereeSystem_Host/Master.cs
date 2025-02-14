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


namespace CoRE1_AutoRefereeSystem_Host
{
    public class Master {
        // シングルトン
        private static readonly Lazy<Master> _instance = new Lazy<Master>(() => new Master());
        public static Master Instance => _instance.Value;

        // 出場チーム
        public string[] TeamName = {    "",
                                        "2001",
                                        "2002",
                                        "2003",
                                        "2004",
                                        "2005",
                                        "[AGSR]AGA'star.",
                                        "[KSHH]機襲藩",
                                        "[AVNT]AVANT",
                                        "[RISN]雷閃",
                                        "[KDSH]近大シューターズ",
                                        "[GRSL]豊田ジラソーレ",
                                        "[VRTX]大阪ヴェルテックス",
                                        "[FRNT]精華フレンテ",
                                        "[RTUS]Ro.T.U.S.",
                                        "[TSTM]チーム薩摩",
                                        "[TRK]TRK",
                                        "[TKG]TKG",
                                        "[DRBS]大ロボーズ",
                                        "[MKNG]MA-KING",
                                        "[TMC]Tactical Majestic Creators",
                                        "[SEC]SETAGAYA Eclipse",
                                        "[DTB]でんとつーとビーバー",
                                        "[SCAT]StrayedCats"
        };

        public Dictionary<string, int> TeamNodeNo = new Dictionary<string, int> {
            {"", 2000},
            {"2001", 2001},
            {"2002", 2002},
            {"2003", 2003},
            {"2004", 2004},
            {"2005", 2005},
            {"[AGSR]AGA'star.", 2603},
            {"[KSHH]機襲藩", 2703},
            {"[AVNT]AVANT", 2803},
            {"[RISN]雷閃", 2903},
            {"[KDSH]近大シューターズ", 3403},
            {"[GRSL]豊田ジラソーレ", 3203},
            {"[VRTX]大阪ヴェルテックス", 3903},
            {"[FRNT]精華フレンテ", 3803},
            {"[RTUS]Ro.T.U.S.", 2403},
            {"[TSTM]チーム薩摩", 2103},
            {"[TRK]TRK", 3303},
            {"[TKG]TKG", 3003},
            {"[DRBS]大ロボーズ", 3103},
            {"[MKNG]MA-KING", 3503},
            {"[TMC]Tactical Majestic Creators", 2503},
            {"[SEC]SETAGAYA Eclipse", 2203},
            {"[DTB]でんとつーとビーバー", 2303},
            {"[SCAT]StrayedCats", 1},
        };

        public Dictionary<string, string> BaseNodeNo = new Dictionary<string, string> {
            {"BaseL", "2001 2002"},
            {"BaseC", "2003 2004"},
            {"BaseR", "2005 2006"},
        };

        public Dictionary<string, int> HostCH = new Dictionary<string, int> {
            {"Red12", 1},
            {"Red34", 2},
            {"Red5", 3},
            {"BaseL", 4},
            {"BaseC", 7},
            {"BaseR", 6},
            {"Blue12", 5},
            {"Blue34", 8},
            {"Blue5", 9}
        };

        /***** 試合のルール *******************************************************************************************************/
        public int SettingTimeMin { private set; get; } = 3;
        public int AllianceMtgTimeMin { private set; get; } = 3;
        public int PreSettingTimeMin { private set; get; } = 2;

        public int GameTimeMin { private set; get; } = 5;
        public int MaxHP { private set; get; } = 40;
        public int PreGameTimeMin { private set; get; } = 2;

        public int PreRedMaxHP { private set; get; } = 100;  // 予選の赤（攻撃サイド）のMaxHP

        public int PreBlueMaxHP { private set; get; } = 20;  // 予選の青（迎撃サイド）のMaxHP

        public int HitDamage { private set; get; } = 10;

        public int AttackBuffTime { private set; get; } = 30;

        public int PenaltyDamage { private set; get; } = 10;
        public int RespawnTime { private set; get; } = 60;
        public int RespawnHP { private set; get; } = 30;
        public int InvincibleTime { private set; get; } = 5;


        /***** 各種フラグ *******************************************************************************************************/
        public bool AutoConnect { set; get; } = false;
        public bool GameEndFlag { set; get; } = false;
        public bool DuringGame { private set; get; } = false;

        public bool Added3min { private set; get; } = false;


        /***** enum定義 *******************************************************************************************************/
        #region
        public enum RobotConnectionEnum {
            DISABLED,
            ENABLED,
            CONNECTED,
            // DISCONNECTED,
        }

        public enum BaseConnectionEnum {
            CONNECTED,
            DISCONNECTED
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

        #endregion

        /***** 各種設定 *******************************************************************************************************/
        public GameFormatEnum GameFormat { set; get; } = GameFormatEnum.NONE;
        public GameStatusEnum GameStatus { set; get; } = GameStatusEnum.PREGAME;
        public SettingStatusEnum SettingStatus { set; get; } = SettingStatusEnum.NONE;

        public bool IsUpdatingStatus { set; get; } = false;

        private static bool _addedTimeout = false;

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
                if (Instance.BaseLOccupationLevel == -5) num++;
                if (Instance.BaseCOccupationLevel == -5) num++;
                if (Instance.BaseROccupationLevel == -5) num++;
                return num;
            }
        }

        public int TotalBlueOccupatedBase {
            private set {;}
            get {
                int num = 0;
                if (Instance.BaseLOccupationLevel == 5) num++;
                if (Instance.BaseCOccupationLevel == 5) num++;
                if (Instance.BaseROccupationLevel == 5) num++;
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
                    if (!window.RedEMSpot3Button.IsEnabled) num++;
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
                    if (!window.BlueEMSpot3Button.IsEnabled) num++;
                });
                return num;
            }
        }

        public int RedAttackBuff { set; get; } = 1;
        public bool IsRedAttackBuff1Active { set; get; } = false;
        public bool IsRedAttackBuff3Active { set; get; } = false;

        public int BlueAttackBuff { set; get; } = 1;
        public bool IsBlueAttackBuff1Active { set; get; } = false;
        public bool IsBlueAttackBuff3Active { set; get; } = false;

        public bool RedHealing {  set; get; } = false;
        public bool BlueHealing { set; get; } = false;

        public bool RedFirstBloodAchieved { set; get; } = false;
        public bool BlueFirstBloodAchieved { set; get; } = false;

        // -5 ~ -1: Red
        // 1 ~ 5: Blue
        public int BaseLOccupationLevel { set; get; } = 0;
        public int BaseCOccupationLevel { set; get; } = 0;
        public int BaseROccupationLevel { set; get; } = 0;

        public WinnerEnum Winner { set; get; } = WinnerEnum.NONE;

        public RobotStatusManager.RobotStatus[] RedRobot { set; get; }
        public RobotStatusManager.RobotStatus[] BlueRobot { set; get; }

        // 操縦画面用プログラムに送信するためのクラス
        public CoreClass Msgs = new CoreClass();

        private const int _port = 12345;
        private UdpClient _udpClient = new UdpClient();

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
        public DateTime _redAttackBuff3StartTime;
        public DateTime _blueAttackBuff1StartTime;
        public DateTime _blueAttackBuff3StartTime;

        // 試合時間のタイマー関係
        private static System.Timers.Timer _countDownTimer;
        private static DateTime _startTime;
        private static TimeSpan _remainingTime;
        private static bool _isPaused = false;

        // UDPのタイマー
        public readonly DispatcherTimer _udpTimer;

        private Master() {
            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 100;
            _updateTimer.Elapsed += UpdateAttackBuff;
            _updateTimer.Elapsed += OnEventArrived;
            _updateTimer.Elapsed += AggregateDamage;
            _updateTimer.Elapsed += CheckGameEnd;
            _updateTimer.Start();

            _countDownTimer = new System.Timers.Timer();
            _countDownTimer.Interval = 50;
            _countDownTimer.Elapsed += OnCountDownTimedEvent;

            _udpTimer = new DispatcherTimer();
            _udpTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            _udpTimer.Tick += new EventHandler(SendMsgsToOperatorScreen);
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
                );

                Instance.TotalBlueDamageTaken = (
                    window.Blue12.Robot1.Status.DamageTaken
                    + window.Blue12.Robot2.Status.DamageTaken
                    + window.Blue34.Robot1.Status.DamageTaken
                    + window.Blue34.Robot2.Status.DamageTaken
                    + window.Blue5.Robot1.Status.DamageTaken
                );

                Instance.TotalRedDefeated = (
                    window.Red12.Robot1.Status.DefeatedNum
                    + window.Red12.Robot2.Status.DefeatedNum
                    + window.Red34.Robot1.Status.DefeatedNum
                    + window.Red34.Robot2.Status.DefeatedNum
                    + window.Red5.Robot1.Status.DefeatedNum
                );

                Instance.TotalBlueDefeated = (
                    window.Blue12.Robot1.Status.DefeatedNum
                    + window.Blue12.Robot2.Status.DefeatedNum
                    + window.Blue34.Robot1.Status.DefeatedNum
                    + window.Blue34.Robot2.Status.DefeatedNum
                    + window.Blue5.Robot1.Status.DefeatedNum
                );

                Instance.RedFirstBloodAchieved = Instance.TotalBlueDefeated >= 1;
                Instance.BlueFirstBloodAchieved = Instance.TotalRedDefeated >= 1;

                window.RedDamageTakenTextBlock.Text = $"{Instance.TotalRedDamageTaken:0000}";
                window.BlueDamageTakenTextBlock.Text = $"{Instance.TotalBlueDamageTaken:0000}";
                window.RedDefeatedTextBlock.Text = $"{Instance.TotalRedDefeated:0000}";
                window.BlueDefeatedTextBlock.Text = $"{Instance.TotalBlueDefeated:0000}";

                window.RedAttackBuffTextBlock.Text = $"x{Instance.RedAttackBuff:0}";
                window.BlueAttackBuffTextBlock.Text = $"x{Instance.BlueAttackBuff:0}";

                if (Instance.RedFirstBloodAchieved) 
                    window.RedFirstBloodTextBlock.Text = "yes";
                else 
                    window.RedFirstBloodTextBlock.Text = "no";

                if (Instance.BlueFirstBloodAchieved)
                    window.BlueFirstBloodTextBlock.Text = "yes";
                else
                    window.BlueFirstBloodTextBlock.Text = "no";
            }));
        }

        private void UpdateAttackBuff(object sender, EventArgs e) {
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY) return;

            Application.Current.Dispatcher.Invoke(() => {
                var window = GetMainWindow();
                Instance.RedAttackBuff = 1;
                if (Instance.IsRedAttackBuff1Active) {
                    var timePassed = DateTime.Now - Instance._redAttackBuff1StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedAttackBuff1Active = false;
                        window.RedAttackbuff1TimeTextBlock.Text = "30 sec..";
                        window.RedAttackbuff1TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.RedAttackBuff *= 2;
                        window.RedAttackbuff1TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsRedAttackBuff3Active) {
                    var timePassed = DateTime.Now - Instance._redAttackBuff3StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsRedAttackBuff3Active = false;
                        window.RedAttackbuff3TimeTextBlock.Text = "30 sec..";
                        window.RedAttackbuff3TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.RedAttackBuff *= 2;
                        window.RedAttackbuff3TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                Instance.BlueAttackBuff = 1;
                if (Instance.IsBlueAttackBuff1Active) {
                    var timePassed = DateTime.Now - Instance._blueAttackBuff1StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueAttackBuff1Active = false;
                        window.BlueAttackbuff1TimeTextBlock.Text = "30 sec..";
                        window.BlueAttackbuff1TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.BlueAttackBuff *= 2;
                        window.BlueAttackbuff1TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }

                if (Instance.IsBlueAttackBuff3Active) {
                    var timePassed = DateTime.Now - Instance._blueAttackBuff3StartTime;
                    var remainingTime = TimeSpan.FromSeconds(Instance.AttackBuffTime) - timePassed;
                    if (remainingTime.TotalSeconds <= 0) {
                        Instance.IsBlueAttackBuff3Active = false;
                        window.BlueAttackbuff3TimeTextBlock.Text = "30 sec..";
                        window.BlueAttackbuff3TimeTextBlock.IsEnabled = false;
                    } else {
                        Instance.BlueAttackBuff *= 2;
                        window.BlueAttackbuff3TimeTextBlock.Text = $"{remainingTime.TotalSeconds:00} sec..";
                    }
                }
                window.RedAttackBuffTextBlock.Text = $"x{Instance.RedAttackBuff:0}";
                window.BlueAttackBuffTextBlock.Text = $"x{Instance.BlueAttackBuff:0}";
            });
        }

        // 試合時間中に勝敗が決定しているか確認
        private void CheckGameEnd(object sender, EventArgs e) {
        //private void CheckGameEnd() {
            if (!Instance.DuringGame) return;

            // 予選の勝敗条件
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

                // 準決勝・決勝の勝敗条件
                // 3つの陣地すべてを同時刻に占拠状態とした同盟の勝利
                // 相手操縦ロボットすべてを同時刻に撃破状態とした同盟の勝利
                //相手操縦ロボットを撃破した回数が多い同盟の勝利
                //5分経過時に陣地を多く占拠している同盟の勝利
                //5分経過時にスポットをより多く獲得した同盟の勝利
                //相手同盟への与ダメージが多い同盟の勝利
                //以上の条件で決定できない場合、引き分けとなり当該ラウンドは再試合となる

                else if (Instance.GameFormat == GameFormatEnum.SEMIFINALS) {
                    // 条件１
                    if (Instance.BaseLOccupationLevel == -5
                        && Instance.BaseCOccupationLevel == -5
                        && Instance.BaseROccupationLevel == -5) { // 赤が占拠
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }
                    else if (Instance.BaseLOccupationLevel == 5
                        && Instance.BaseCOccupationLevel == 5
                        && Instance.BaseROccupationLevel == 5) { // 青が占拠
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.BLUE;
                        window.TimerLabel.Text = "BLUE WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }

                    // 条件２
                    else if (window.Red12.Robot1.Status.DefeatedFlag &&
                        window.Red12.Robot2.Status.DefeatedFlag &&
                        window.Red34.Robot1.Status.DefeatedFlag &&
                        window.Red34.Robot2.Status.DefeatedFlag) { // 赤同盟がすべて撃破
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.BLUE;
                        window.TimerLabel.Text = "BLUE WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    } 
                    else if (window.Blue12.Robot1.Status.DefeatedFlag &&
                               window.Blue12.Robot2.Status.DefeatedFlag &&
                               window.Blue34.Robot1.Status.DefeatedFlag &&
                               window.Blue34.Robot2.Status.DefeatedFlag) { // 青同盟がすべて撃破
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }
                }                
                else {
                    // 条件１
                    if (Instance.BaseLOccupationLevel == -5
                        && Instance.BaseCOccupationLevel == -5
                        && Instance.BaseROccupationLevel == -5) { // 赤が占拠
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    } else if (Instance.BaseLOccupationLevel == 5
                          && Instance.BaseCOccupationLevel == 5
                          && Instance.BaseROccupationLevel == 5) { // 青が占拠
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    }

                    // 条件２
                    if (window.Red12.Robot1.Status.DefeatedFlag &&
                        window.Red12.Robot2.Status.DefeatedFlag &&
                        window.Red34.Robot1.Status.DefeatedFlag &&
                        window.Red34.Robot2.Status.DefeatedFlag &&
                        window.Red5.Robot1.Status.DefeatedFlag) { // 赤同盟がすべて撃破
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.BLUE;
                        window.TimerLabel.Text = "BLUE WINS!!!";
                        Instance.DuringGame = false;
                        Instance.GameStatus = GameStatusEnum.POSTGAME;
                    } else if (window.Blue12.Robot1.Status.DefeatedFlag &&
                               window.Blue12.Robot2.Status.DefeatedFlag &&
                               window.Blue34.Robot1.Status.DefeatedFlag &&
                               window.Blue34.Robot2.Status.DefeatedFlag &&
                               window.Red5.Robot1.Status.DefeatedFlag) { // 赤同盟がすべて撃破
                        Instance.GameEndFlag = true;
                        Instance.Winner = WinnerEnum.RED;
                        window.TimerLabel.Text = "RED WINS!!!";
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

        public void Add3minSetting() {
            Added3min = true;
        }

        public void SettingTimeStart() {
            GetMainWindow().TimerLabel.Text = "SETTING TIME";
            Instance.GameStatus = GameStatusEnum.SETTING;
            Instance.SettingStatus= SettingStatusEnum.RUNNING;
            int settingTime = Added3min ? Instance.AllianceMtgTimeMin + Instance.SettingTimeMin : Instance.SettingTimeMin;
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY)
                settingTime = Instance.PreSettingTimeMin;
            StartTimer(settingTime * 60);
            AllocateButton();
        }

        public void ResetSettingTime() {
            GetMainWindow().TimerLabel.Text = "SETTING READY?";
            Instance.GameStatus = GameStatusEnum.NONE;
            Instance.SettingStatus = SettingStatusEnum.NONE;
            int settingTime = Instance.AllianceMtgTimeMin + Instance.SettingTimeMin;
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY)
                settingTime = Instance.PreSettingTimeMin;
            Added3min = false;
            ResetTimer(settingTime * 60);
            AllocateButton();
        }

        public void ResumeSettingTime() {
            GetMainWindow().TimerLabel.Text = "SETTING TIME";
            Instance.SettingStatus = SettingStatusEnum.RUNNING;
            ResumeTimer();
            AllocateButton();
        }

        public void SkipSettingTime() {
            GetMainWindow().TimerLabel.Text = "GAME READY?";
            Instance.GameStatus = GameStatusEnum.PREGAME;
            Instance.SettingStatus = SettingStatusEnum.SKIP;

            int gameTime = Instance.GameTimeMin;
            if (Instance.GameFormat == GameFormatEnum.PRELIMINALY)
                gameTime = Instance.PreGameTimeMin;
            Added3min = false;
            ResetTimer(gameTime * 60);
            AllocateButton();
        }

        public void Timeout() {
            if (!_addedTimeout) {
                _remainingTime += TimeSpan.FromSeconds(2 * 60);
                _addedTimeout = true;
            }
            AllocateButton();
        }
        public void TechnicalTimeout() {
            GetMainWindow().TimerLabel.Text = "TECH. TIMEOUT";
            Instance.SettingStatus = SettingStatusEnum.TECH_TIMEOUT;
            PauseTimer();
            AllocateButton();
        }
        public event Action ClearDataEvent;
        public void ClearData() {
            Instance.DuringGame = false;
            Instance.GameEndFlag = false;
            Added3min = false;
            _addedTimeout = false;
            ClearDataEvent?.Invoke();
        }


        private static void StartTimer(double timeSec) {
            _remainingTime = TimeSpan.FromSeconds(timeSec);
            _startTime = DateTime.Now;
            _countDownTimer.Start();
        }

        private static void PauseTimer() {
            if (_isPaused) return;

            _countDownTimer.Stop();
            _remainingTime -= DateTime.Now - _startTime;
            _isPaused = true;
        }

        private static void ResumeTimer() {
            if (!_isPaused) return;
            _startTime = DateTime.Now;
            _countDownTimer.Start();
            _isPaused = false;
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

                    // 決勝トーナメントにおけるラウンドの勝敗条件3以降
                    // 条件3 相手操縦ロボットを撃破した回数が多い同盟の勝利
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

                    // 条件4 5分経過時に陣地を多く占拠している同盟の勝利
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

                    // 条件５ 5分経過時にスポットをより多く獲得した同盟の勝利
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

                    // 条件６ 相手同盟への与ダメージが多い同盟の勝利
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
                    window.SettingStartButton.IsEnabled = true;
                    window.SettingResetButton.IsEnabled = false;
                    window.TimeoutButton.IsEnabled = false;
                    window.TechnicalTimeOutButton.IsEnabled = false;
                    window.SettingResumeButton.IsEnabled = false;
                    window.SettingSkipButton.IsEnabled = false;
                    window.GameStartButton.IsEnabled = true;
                    window.GameResetButton.IsEnabled = false;
                } else if (Instance.GameStatus == GameStatusEnum.SETTING) {
                    if (Instance.SettingStatus == SettingStatusEnum.RUNNING) {
                        window.PreliminaryRadioButton.IsEnabled = false;
                        window.SettingStartButton.IsEnabled = false;
                        window.SettingResetButton.IsEnabled = true;
                        window.TimeoutButton.IsEnabled = Instance.GameFormat == GameFormatEnum.PRELIMINALY ? false : !_addedTimeout;
                        window.TechnicalTimeOutButton.IsEnabled = true;
                        window.SettingResumeButton.IsEnabled = false;
                        window.SettingSkipButton.IsEnabled = true;
                        window.GameStartButton.IsEnabled = false;
                        window.GameResetButton.IsEnabled = false;
                    } else if (Instance.SettingStatus == SettingStatusEnum.TECH_TIMEOUT) {
                        window.PreliminaryRadioButton.IsEnabled = false;
                        window.SettingStartButton.IsEnabled = false;
                        window.SettingResetButton.IsEnabled = false;
                        window.TimeoutButton.IsEnabled = false;
                        window.TechnicalTimeOutButton.IsEnabled = false;
                        window.SettingResumeButton.IsEnabled = true;
                        window.SettingSkipButton.IsEnabled = false;
                        window.GameStartButton.IsEnabled = false;
                        window.GameResetButton.IsEnabled = false;
                    }
                } else if (Instance.GameStatus == GameStatusEnum.PREGAME) {
                    window.PreliminaryRadioButton.IsEnabled = false;
                    window.SettingStartButton.IsEnabled = true;
                    window.SettingResetButton.IsEnabled = false;
                    window.TimeoutButton.IsEnabled = false;
                    window.TechnicalTimeOutButton.IsEnabled = false;
                    window.SettingResumeButton.IsEnabled = false;
                    window.SettingSkipButton.IsEnabled = false;
                    window.GameStartButton.IsEnabled = true;
                    window.GameResetButton.IsEnabled = false;      
                } else if (Instance.GameStatus == GameStatusEnum.GAME) {
                    window.PreliminaryRadioButton.IsEnabled = false;
                    window.SettingStartButton.IsEnabled = false;
                    window.SettingResetButton.IsEnabled = false;
                    window.TimeoutButton.IsEnabled = false;
                    window.TechnicalTimeOutButton.IsEnabled = false;
                    window.SettingResumeButton.IsEnabled = false;
                    window.SettingSkipButton.IsEnabled = false;
                    window.GameStartButton.IsEnabled = false;
                    window.GameResetButton.IsEnabled = true;
                 }
            }));
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
            Msgs.RedSpot[1] = Bool2Int(Instance.RedHealing);
            Msgs.RedSpot[2] = Bool2Int(Instance.IsRedAttackBuff3Active);

            Msgs.BlueSpot[0] = Bool2Int(Instance.IsBlueAttackBuff1Active);
            Msgs.BlueSpot[1] = Bool2Int(Instance.BlueHealing);
            Msgs.BlueSpot[2] = Bool2Int(Instance.IsBlueAttackBuff3Active);


            // 陣地
            Msgs.RedArea = 10 - (Master.Instance.BaseLOccupationLevel + 5);
            Msgs.CenterArea = 10 - (Master.Instance.BaseCOccupationLevel + 5);
            Msgs.BlueArea = 10 - (Master.Instance.BaseROccupationLevel + 5);

            // 試合結果
            Msgs.RedWin = 0;
            Msgs.BlueWin = 0;
            Msgs.Winner = (uint)Instance.Winner;

            // Robot
            var window = GetMainWindow();
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

                await _udpClient.SendAsync(data, data.Length, "192.168.100.100", _port);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _udpClient.SendAsync(data, data.Length, "192.168.100.101", _port);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _udpClient.SendAsync(data, data.Length, "192.168.100.102", _port);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            try {
                string json = JsonConvert.SerializeObject(Msgs);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _udpClient.SendAsync(data, data.Length, "192.168.100.103", _port);
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }

            Interlocked.Exchange(ref isSending, 0);
        }

        private int Bool2Int(bool value) {
            return value ? 1 : 0;
        }
    }
}
