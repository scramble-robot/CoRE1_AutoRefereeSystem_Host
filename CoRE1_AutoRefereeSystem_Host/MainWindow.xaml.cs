using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;


namespace CoRE1_AutoRefereeSystem_Host
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            PlayerScreenToggleButton.IsChecked = true;
            UpdateStatusToggleButton.IsChecked = false;
            GameStartButton.IsEnabled = false;
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);

            bool allHostShutdown = (
                !Red12.ShutdownButton.IsEnabled
                && !Red34.ShutdownButton.IsEnabled
                && !Red5.ShutdownButton.IsEnabled
                && !Blue12.ShutdownButton.IsEnabled
                && !Blue34.ShutdownButton.IsEnabled
                && !Blue5.ShutdownButton.IsEnabled
                //&& BaseL.BootButton.Content.ToString() == "Boot"
                //&& BaseC.BootButton.Content.ToString() == "Boot"
                //&& BaseR.BootButton.Content.ToString() == "Boot"
            );

            if (!allHostShutdown) {
                MessageBox.Show("You must excecute 'shutdown' command on all booted HostPCBs.",
                    "Failed to terminate CoRE-1: 2025 Host program", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
        }

        private void GameStartButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.GameStart();
        }

        private void GameResetButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.GameReset();
        }


        private void SettingStartButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.SettingTimeStart();
        }

        private void TimeoutButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.Timeout();
            TimeoutButton.IsEnabled = false;
        }

        private void TechnicalTimeOutButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.TechnicalTimeout();
        }

        private void SettingResumeButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.ResumeSettingTime();
        }

        private void SettingResetButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.ResetSettingTime();
        }
        private void SettingSkipButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.SkipSettingTime();
        }

        private void PreliminaryRadioButton_Checked(object sender, RoutedEventArgs e) {
            Master.Instance.GameFormat = Master.GameFormatEnum.PRELIMINALY;

            AllControlPanelDisabled();

            Red12.IsEnabled = true;
            Red12.Robot1.IsEnabled = true; 
            Red12.CommEnabledToggleButton1.IsEnabled = true;
            Red12.CommEnabledToggleButton1.IsChecked = true;

            Blue12.IsEnabled = true;
            Blue12.Robot1.IsEnabled = true; 
            Blue12.CommEnabledToggleButton1.IsEnabled = true;
            Blue12.CommEnabledToggleButton1.IsChecked = true;
            Blue12.Robot2.IsEnabled = true; 
            Blue12.CommEnabledToggleButton2.IsEnabled = true;
            Blue12.CommEnabledToggleButton2.IsChecked = true;

            Blue34.IsEnabled = true;
            Blue34.Robot1.IsEnabled = true; 
            Blue34.CommEnabledToggleButton1.IsEnabled = true;
            Blue34.CommEnabledToggleButton1.IsChecked = true;

            RedTeamEMStackPanel.IsEnabled = false;
            BlueTeamEMStackPanel.IsEnabled = false;

            PreliminaryRadioButton.IsEnabled = true;
            SettingStartButton.IsEnabled = true;
            SettingResetButton.IsEnabled = false;
            TimeoutButton.IsEnabled = false;
            TechnicalTimeOutButton.IsEnabled = false;
            SettingResumeButton.IsEnabled = false;
            SettingSkipButton.IsEnabled = false;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            Master.Instance.GameStatus = Master.GameStatusEnum.NONE;
            int time = Master.Instance.PreSettingTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;
        }

        private void SemifinalsRadioButton_Checked(object sender, RoutedEventArgs e) {
            Master.Instance.GameFormat = Master.GameFormatEnum.SEMIFINALS;

            AllControlPanelDisabled();

            Red12.IsEnabled = true;
            Red12.Robot1.IsEnabled = true; 
            Red12.CommEnabledToggleButton1.IsEnabled = true;
            Red12.CommEnabledToggleButton1.IsChecked = true;
            Red12.Robot2.IsEnabled = true; 
            Red12.CommEnabledToggleButton2.IsEnabled = true;
            Red12.CommEnabledToggleButton2.IsChecked = true;

            Red34.IsEnabled = true; 
            Red34.Robot1.IsEnabled = true; 
            Red34.CommEnabledToggleButton1.IsEnabled= true;
            Red34.CommEnabledToggleButton1.IsChecked = true;
            Red34.Robot2.IsEnabled = true; 
            Red34.CommEnabledToggleButton2.IsEnabled= true;
            Red34.CommEnabledToggleButton2.IsChecked= true;

            BaseL.IsEnabled = true;
            BaseC.IsEnabled = true;
            BaseR.IsEnabled = true;

            Blue12.IsEnabled = true;
            Blue12.Robot1.IsEnabled = true; 
            Blue12.CommEnabledToggleButton1.IsEnabled = true;
            Blue12.CommEnabledToggleButton1.IsChecked= true;
            Blue12.Robot2.IsEnabled = true; 
            Blue12.CommEnabledToggleButton2.IsEnabled = true;
            Blue12.CommEnabledToggleButton2.IsChecked = true;

            Blue34.IsEnabled = true;
            Blue34.Robot1.IsEnabled = true; 
            Blue34.CommEnabledToggleButton1.IsEnabled = true;
            Blue34.CommEnabledToggleButton1.IsChecked = true;
            Blue34.Robot2.IsEnabled = true; 
            Blue34.CommEnabledToggleButton2.IsEnabled = true;
            Blue34.CommEnabledToggleButton2.IsChecked = true;

            RedTeamEMStackPanel.IsEnabled = true;
            BlueTeamEMStackPanel.IsEnabled = true;

            PreliminaryRadioButton.IsEnabled = true;
            SettingStartButton.IsEnabled = true;
            SettingResetButton.IsEnabled = false;
            TimeoutButton.IsEnabled = false;
            TechnicalTimeOutButton.IsEnabled = false;
            SettingResumeButton.IsEnabled = false;
            SettingSkipButton.IsEnabled = false;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            Master.Instance.GameStatus = Master.GameStatusEnum.PREGAME;
            /*int time = Master.Instance.SettingTimeMin;
            if (Master.Instance.Added3min) time += Master.Instance.AllianceMtgTimeMin;

            string timeText = $"{time:D2}:00";*/
            int time = Master.Instance.GameTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;
        }

        private void FinalsRadioButton_Checked(object sender, RoutedEventArgs e) {
            Master.Instance.GameFormat = Master.GameFormatEnum.FINALS;

            AllControlPanelDisabled();

            Red12.IsEnabled = true;
            Red12.Robot1.IsEnabled = true;
            Red12.CommEnabledToggleButton1.IsEnabled = true;
            Red12.CommEnabledToggleButton1.IsChecked = true;
            Red12.Robot2.IsEnabled = true;
            Red12.CommEnabledToggleButton2.IsEnabled = true;
            Red12.CommEnabledToggleButton2.IsChecked = true;

            Red34.IsEnabled = true;
            Red34.Robot1.IsEnabled = true;
            Red34.CommEnabledToggleButton1.IsEnabled = true;
            Red34.CommEnabledToggleButton1.IsChecked = true;
            Red34.Robot2.IsEnabled = true;
            Red34.CommEnabledToggleButton2.IsEnabled = true;
            Red34.CommEnabledToggleButton2.IsChecked = true;

            Red5.IsEnabled = true;
            Red5.Robot1.IsEnabled = true;
            Red5.CommEnabledToggleButton1.IsEnabled = true;
            Red5.CommEnabledToggleButton1.IsChecked = true;

            BaseL.IsEnabled = true;
            BaseC.IsEnabled = true;
            BaseR.IsEnabled = true;

            Blue12.IsEnabled = true;
            Blue12.Robot1.IsEnabled = true;
            Blue12.CommEnabledToggleButton1.IsEnabled = true;
            Blue12.CommEnabledToggleButton1.IsChecked = true;
            Blue12.Robot2.IsEnabled = true;
            Blue12.CommEnabledToggleButton2.IsEnabled = true;
            Blue12.CommEnabledToggleButton2.IsChecked = true;

            Blue34.IsEnabled = true;
            Blue34.Robot1.IsEnabled = true;
            Blue34.CommEnabledToggleButton1.IsEnabled = true;
            Blue34.CommEnabledToggleButton1.IsChecked = true;
            Blue34.Robot2.IsEnabled = true;
            Blue34.CommEnabledToggleButton2.IsEnabled = true;
            Blue34.CommEnabledToggleButton2.IsChecked = true;

            Blue5.IsEnabled = true;
            Blue5.Robot1.IsEnabled = true;
            Blue5.CommEnabledToggleButton1.IsEnabled = true;
            Blue5.CommEnabledToggleButton1.IsChecked= true;

            RedTeamEMStackPanel.IsEnabled = true;
            BlueTeamEMStackPanel.IsEnabled = true;

            PreliminaryRadioButton.IsEnabled = true;
            SettingStartButton.IsEnabled = true;
            SettingResetButton.IsEnabled = false;
            TimeoutButton.IsEnabled = false;
            TechnicalTimeOutButton.IsEnabled = false;
            SettingResumeButton.IsEnabled = false;
            SettingSkipButton.IsEnabled = false;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            Master.Instance.GameStatus = Master.GameStatusEnum.PREGAME;
            /*int settingTime = Master.Instance.SettingTimeMin;
            if (Master.Instance.Added3min) settingTime += Master.Instance.AllianceMtgTimeMin;
            string timeText = $"{settingTime:D2}:00";*/
            int time = Master.Instance.GameTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;
        }

        private void AllControlPanelDisabled() {
            // Red12
            Red12.IsEnabled = false;
            Red12.Robot1.IsEnabled = false; 
            Red12.CommEnabledToggleButton1.IsEnabled = false;
            Red12.CommEnabledToggleButton1.IsChecked = false;
            Red12.Robot2.IsEnabled = false; 
            Red12.CommEnabledToggleButton2.IsEnabled = false;
            Red12.CommEnabledToggleButton2.IsChecked = false;

            // Red34
            Red34.IsEnabled = false;
            Red34.Robot1.IsEnabled = false; 
            Red34.CommEnabledToggleButton1.IsEnabled = false;
            Red34.CommEnabledToggleButton1.IsChecked = false;
            Red34.Robot2.IsEnabled = false; 
            Red34.CommEnabledToggleButton2.IsEnabled = false;
            Red34.CommEnabledToggleButton2.IsChecked= false;

            // Red5
            Red5.IsEnabled = false;
            Red5.Robot1.IsEnabled = false; 
            Red5.CommEnabledToggleButton1.IsEnabled = false;
            Red5.CommEnabledToggleButton1.IsChecked= false;

            // BaseL
            BaseL.IsEnabled = false;

            // BaseC
            BaseC.IsEnabled = false;

            // BaseR
            BaseR.IsEnabled = false;

            // Blue12
            Blue12.IsEnabled = false;
            Blue12.Robot1.IsEnabled = false; 
            Blue12.CommEnabledToggleButton1.IsEnabled = false;
            Blue12.CommEnabledToggleButton1.IsChecked = false;
            Blue12.Robot2.IsEnabled = false; 
            Blue12.CommEnabledToggleButton2.IsEnabled = false;
            Blue12.CommEnabledToggleButton2.IsChecked = false;

            // Blue34
            Blue34.IsEnabled = false;
            Blue34.Robot1.IsEnabled = false; 
            Blue34.CommEnabledToggleButton1.IsEnabled = false;
            Blue34.CommEnabledToggleButton1.IsChecked = false;
            Blue34.Robot2.IsEnabled = false; 
            Blue34.CommEnabledToggleButton2.IsEnabled = false;
            Blue34.CommEnabledToggleButton2.IsChecked = false;

            // Blue5
            Blue5.IsEnabled = false;
            Blue5.Robot1.IsEnabled = false; 
            Blue5.CommEnabledToggleButton1.IsEnabled = false;
            Blue5.CommEnabledToggleButton1.IsChecked = false;

        }

        private void PlayerScreenToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (PlayerScreenToggleButton.IsChecked == true) {
                Master.Instance._udpTimer.Start();
            } else {
                Master.Instance._udpTimer.Stop();
            }
        }

        private void UpdateStatusToggleButton_Checked(object sender, RoutedEventArgs e) {
            if (UpdateStatusToggleButton.IsChecked == true) {
                Master.Instance.IsUpdatingStatus = true;
                GameStartButton.IsEnabled = true;
            } else {
                Master.Instance.IsUpdatingStatus = false;
                GameStartButton.IsEnabled = false;
            }
        }

        private void AllClearButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.ClearData();
            TimerLabel.Text = "SETTING READY?";
            int time = Master.Instance.AllianceMtgTimeMin + Master.Instance.SettingTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;
        }

        private void Add3MinButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.Add3minSetting();
            int time = Master.Instance.AllianceMtgTimeMin + Master.Instance.SettingTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;
        }


        private void RedEMSpot1Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedAttackBuff1Active = true;
            Master.Instance._redAttackBuff1StartTime = DateTime.Now;
            RedAttackbuff1TimeTextBlock.IsEnabled = true;
            RedEMSpot1Button.IsEnabled = false;
        }

        private void RedEMSpot2Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.RedHealing = true;
            if (!Red12.Robot1.Status.DefeatedFlag) Red12.Robot1.Status.HP = Master.Instance.MaxHP;
            if (!Red12.Robot2.Status.DefeatedFlag) Red12.Robot2.Status.HP = Master.Instance.MaxHP;
            if (!Red34.Robot1.Status.DefeatedFlag) Red34.Robot1.Status.HP = Master.Instance.MaxHP;
            if (!Red34.Robot2.Status.DefeatedFlag) Red34.Robot2.Status.HP = Master.Instance.MaxHP;

            if (Master.Instance.GameFormat == Master.GameFormatEnum.FINALS
                && !Red5.Robot1.Status.DefeatedFlag)
                Red5.Robot1.Status.HP = Master.Instance.MaxHP;

            RedEMSpot2Button.IsEnabled = false;
        }

        private void RedEMSpot3Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedAttackBuff3Active = true;
            Master.Instance._redAttackBuff3StartTime = DateTime.Now;
            RedAttackbuff3TimeTextBlock.IsEnabled = true;
            RedEMSpot3Button.IsEnabled = false;
        }

        private void BlueEMSpot1Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueAttackBuff1Active = true;
            Master.Instance._blueAttackBuff1StartTime = DateTime.Now;
            BlueAttackbuff1TimeTextBlock.IsEnabled = true;
            BlueEMSpot1Button.IsEnabled = false;
        }

        private void BlueEMSpot2Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.BlueHealing = true;
            if (!Blue12.Robot1.Status.DefeatedFlag) Blue12.Robot1.Status.HP = Master.Instance.MaxHP;
            if (!Blue12.Robot2.Status.DefeatedFlag) Blue12.Robot2.Status.HP = Master.Instance.MaxHP;
            if (!Blue34.Robot1.Status.DefeatedFlag) Blue34.Robot1.Status.HP = Master.Instance.MaxHP;
            if (!Blue34.Robot2.Status.DefeatedFlag) Blue34.Robot2.Status.HP = Master.Instance.MaxHP;

            if (Master.Instance.GameFormat == Master.GameFormatEnum.FINALS
                && !Blue5.Robot1.Status.DefeatedFlag)
                Blue5.Robot1.Status.HP = Master.Instance.MaxHP;

            BlueEMSpot2Button.IsEnabled = false;
        }

        private void BlueEMSpot3Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueAttackBuff3Active = true;
            Master.Instance._blueAttackBuff3StartTime = DateTime.Now;
            BlueAttackbuff3TimeTextBlock.IsEnabled = true;
            BlueEMSpot3Button.IsEnabled = false;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (!Master.Instance.DuringGame) return;

            // ダメージ
            if (e.Key == Key.D1)
                Red12.Robot1.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D2)
                Red12.Robot2.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D3)
                Red34.Robot1.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D4)
                Red34.Robot2.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D5)
                Red5.Robot1.PunishButton_Click(sender, new RoutedEventArgs());

            if (e.Key == Key.D6)
                Blue12.Robot1.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D7)
                Blue12.Robot2.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D8)
                Blue34.Robot1.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D9)
                Blue34.Robot2.PunishButton_Click(sender, new RoutedEventArgs());

            else if (e.Key == Key.D0)
                Blue5.Robot1.PunishButton_Click(sender, new RoutedEventArgs());

            // 陣地
            if (e.Key == Key.A)
                BaseL.RedLevelButton_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.S)
                BaseL.NeurtralButton_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.D)
                BaseL.BlueLevelButton_Click(sender, new RoutedEventArgs());

            if (e.Key == Key.F)
                BaseC.RedLevelButton_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.G)
                BaseC.NeurtralButton_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.H)
                BaseC.BlueLevelButton_Click(sender, new RoutedEventArgs());

            if (e.Key == Key.J)
                BaseR.RedLevelButton_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.K)
                BaseR.NeurtralButton_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.L)
                BaseR.BlueLevelButton_Click(sender, new RoutedEventArgs());
        }
    }
}
