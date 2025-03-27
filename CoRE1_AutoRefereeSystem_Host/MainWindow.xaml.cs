using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Net;
using System.Configuration;


namespace CoRE1_AutoRefereeSystem_Host
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();

            RedStriderTeamNameComboBox.Items.Clear();
            foreach (string tn in Master.Instance.TeamName)
                if (tn.Contains("[STR]")) RedStriderTeamNameComboBox.Items.Add(tn);

            BlueStriderTeamNameComboBox.Items.Clear();
            foreach (string tn in Master.Instance.TeamName)
                if (tn.Contains("[STR]")) BlueStriderTeamNameComboBox.Items.Add(tn);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            GameStartButton.IsEnabled = false;
            Settings settings = SettingsManager.Instance.LoadSettings();

            if (settings.GameFormat == Master.GameFormatEnum.PRELIMINALY)
                PreliminaryRadioButton.IsChecked = true;
            else if (settings.GameFormat == Master.GameFormatEnum.SEMIFINALS)
                SemifinalsRadioButton.IsChecked = true;
            else
                FinalsRadioButton.IsChecked = true;

            Red12.Robot1.TeamNameComboBox.SelectedItem = settings.Red1TeamName;
            Red12.Robot2.TeamNameComboBox.SelectedItem = settings.Red2TeamName;
            Red34.Robot1.TeamNameComboBox.SelectedItem = settings.Red3TeamName;
            Red34.Robot2.TeamNameComboBox.SelectedItem = settings.Red4TeamName;
            Red5.Robot1.TeamNameComboBox.SelectedItem = settings.Red5TeamName;
            Red6.Robot1.TeamNameComboBox.SelectedItem = settings.Red6TeamName;
            RedStriderTeamNameComboBox.SelectedItem = settings.Red7TeamName;

            Blue12.Robot1.TeamNameComboBox.SelectedItem = settings.Blue1TeamName;
            Blue12.Robot2.TeamNameComboBox.SelectedItem = settings.Blue2TeamName;
            Blue34.Robot1.TeamNameComboBox.SelectedItem = settings.Blue3TeamName;
            Blue34.Robot2.TeamNameComboBox.SelectedItem = settings.Blue4TeamName;
            Blue5.Robot1.TeamNameComboBox.SelectedItem = settings.Blue5TeamName;
            Blue6.Robot1.TeamNameComboBox.SelectedItem = settings.Blue6TeamName;
            BlueStriderTeamNameComboBox.SelectedItem = settings.Blue7TeamName;

            Red12.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Red1RobotType;
            Red12.Robot2.RobotTypeComboBox.SelectedIndex = (int)settings.Red2RobotType;
            Red34.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Red3RobotType;
            Red34.Robot2.RobotTypeComboBox.SelectedIndex = (int)settings.Red4RobotType;
            Red5.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Red5RobotType;
            Red6.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Red6RobotType;
            
            Blue12.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Blue1RobotType;
            Blue12.Robot2.RobotTypeComboBox.SelectedIndex = (int)settings.Blue2RobotType;
            Blue34.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Blue3RobotType;
            Blue34.Robot2.RobotTypeComboBox.SelectedIndex = (int)settings.Blue4RobotType;
            Blue5.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Blue5RobotType;
            Blue6.Robot1.RobotTypeComboBox.SelectedIndex = (int)settings.Blue6RobotType;

            RedNumWinsTextBox.Text = settings.NumRedWins.ToString();
            EnterRedNumWins();

            BlueNumWinsTextBox.Text = settings.NumBlueWins.ToString();
            EnterBlueNumWins();

            Red12.ComPortSelectionComboBox.SelectedItem = settings.Red12ComPort;
            Red34.ComPortSelectionComboBox.SelectedItem = settings.Red34ComPort;
            Red5.ComPortSelectionComboBox.SelectedItem = settings.Red5ComPort;
            Red6.EndPointTextBox.Text = settings.Red6EndPoint;
            Red6.EnterEndPoint();

            Blue12.ComPortSelectionComboBox.SelectedItem = settings.Blue12ComPort;
            Blue34.ComPortSelectionComboBox.SelectedItem = settings.Blue34ComPort;
            Blue5.ComPortSelectionComboBox.SelectedItem = settings.Blue5ComPort;
            Blue6.EndPointTextBox.Text = settings.Blue6EndPoint;
            Blue6.EnterEndPoint();

            RedBase.EndPointTextBox.Text = settings.RedBaseEndPoint;
            RedBase.EnterEndPoint();

            BlueBase.EndPointTextBox.Text = settings.BlueBaseEndPoint;
            BlueBase.EnterEndPoint();

            CommonBase.EndPointTextBox.Text = settings.CommonBaseEndPoint;
            CommonBase.EnterEndPoint();

            //BuffHostTextBox.Text = settings.BuffHost;


            Master.Instance.SettingsJson = settings;

            NnChSettings nnChSettings = NnChSettingsManager.Instance.LoadSettings();
            NnChSettingsManager.Instance.SaveSettings(nnChSettings);
            Master.Instance.TeamNodeNo = nnChSettings.TeamNodeNo;
            Master.Instance.HostCH = nnChSettings.HostCH;
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
             );

            if (!allHostShutdown) {
                MessageBox.Show("You must excecute 'shutdown' command on all booted HostPCBs.",
                    "Failed to terminate CoRE-1 2025 Host program", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Cancel = true;
            }
        }

        private void GameStartButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.GameStart();
        }

        private void GameResetButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.GameReset();
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
            SheildbuffStackPanel.IsEnabled = false;

            PreliminaryRadioButton.IsEnabled = true;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            Master.Instance.GameStatus = Master.GameStatusEnum.PREGAME;
            int time = Master.Instance.PreliminaryGameTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;

            Master.Instance.SettingsChanged = true;
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
            Red34.CommEnabledToggleButton1.IsEnabled = true;
            Red34.CommEnabledToggleButton1.IsChecked = true;

            Red34.Robot2.IsEnabled = true;
            Red34.CommEnabledToggleButton2.IsEnabled = true;
            Red34.CommEnabledToggleButton2.IsChecked = true;

            Red6.IsEnabled = true;
            Red6.Robot1.IsEnabled = true;
            Red6.CommEnabledToggleButton1.IsEnabled= true;
            Red6.CommEnabledToggleButton1.IsChecked= true;

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

            Blue6.IsEnabled = true;
            Blue6.Robot1.IsEnabled = true;
            Blue6.CommEnabledToggleButton1.IsEnabled = true;
            Blue6.CommEnabledToggleButton1.IsChecked = true;

            RedZone1Button.IsEnabled = true;
            BlueZone1Button.IsEnabled= true;

            RedBase.IsEnabled = true;
            CommonBase.IsEnabled = true;
            BlueBase.IsEnabled = true;

            RedTeamEMStackPanel.IsEnabled = true;
            BlueTeamEMStackPanel.IsEnabled = true;
            SheildbuffStackPanel.IsEnabled = true;

            PreliminaryRadioButton.IsEnabled = true;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            Master.Instance.GameStatus = Master.GameStatusEnum.PREGAME;
            int time = Master.Instance.TornamentGameTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;

            Master.Instance.SettingsChanged = true;
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

            Red6.IsEnabled = true;
            Red6.Robot1.IsEnabled = true;
            Red6.CommEnabledToggleButton1.IsEnabled = true;
            Red6.CommEnabledToggleButton1.IsChecked = true;

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

            Blue6.IsEnabled = true;
            Blue6.Robot1.IsEnabled = true;
            Blue6.CommEnabledToggleButton1.IsEnabled = true;
            Blue6.CommEnabledToggleButton1.IsChecked = true;

            RedZone1Button.IsEnabled = true;
            BlueZone1Button.IsEnabled = true;

            RedBase.IsEnabled = true;
            CommonBase.IsEnabled = true;
            BlueBase.IsEnabled = true;

            RedTeamEMStackPanel.IsEnabled = true;
            BlueTeamEMStackPanel.IsEnabled = true;
            SheildbuffStackPanel.IsEnabled = true;

            PreliminaryRadioButton.IsEnabled = true;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            Master.Instance.GameStatus = Master.GameStatusEnum.PREGAME;
            /*int settingTime = Master.Instance.SettingTimeMin;
            if (Master.Instance.Added3min) settingTime += Master.Instance.AllianceMtgTimeMin;
            string timeText = $"{settingTime:D2}:00";*/
            int time = Master.Instance.TornamentGameTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            Master.Instance.GameTime = timeText;
            Master.Instance.SettingTime = timeText;

            Master.Instance.SettingsChanged = true;
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

            // Red6
            Red6.IsEnabled = false;
            Red6.Robot1.IsEnabled = false; 
            Red6.CommEnabledToggleButton1.IsEnabled = false;
            Red6.CommEnabledToggleButton1.IsChecked = false;

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

            // Blue6
            Blue6.IsEnabled = false;
            Blue6.Robot1.IsEnabled = false; 
            Blue6.CommEnabledToggleButton1.IsEnabled = false;
            Blue6.CommEnabledToggleButton1.IsChecked = false;

            // ZONE
            RedZone1Button.IsEnabled = false;
            RedZone2Button.IsEnabled = false;
            BlueZone1Button.IsEnabled = false;
            BlueZone2Button.IsEnabled = false;

            // RedBase
            RedBase.IsEnabled = false;

            // CommonBase
            CommonBase.IsEnabled = false;

            // BlueBase
            BlueBase.IsEnabled = false;
        }
        /***** 失格による試合終了 *******************************************************************************************************/

        private void RedDQButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.DisqualifiedFlag = Master.DisqualifiedFlagEnum.RED;
        }

        private void BlueDQButton_Click(object sender, RoutedEventArgs e) {
            Master.Instance.DisqualifiedFlag = Master.DisqualifiedFlagEnum.BLUE;
        }

        /***** ストライダーのチーム選択 *******************************************************************************************************/
        private void RedStriderTeamNameComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (RedStriderTeamNameComboBox.SelectedItem is null) return;
            Master.Instance.RedStriderTeamName = RedStriderTeamNameComboBox.SelectedItem.ToString();
            Master.Instance.SettingsChanged = true;
        }

        private void BlueStriderTeamNameComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (BlueStriderTeamNameComboBox.SelectedItem is null) return;
            Master.Instance.BlueStriderTeamName = BlueStriderTeamNameComboBox.SelectedItem.ToString();
            Master.Instance.SettingsChanged = true;
        }

        /***** 強化素材によるバフ効果 *******************************************************************************************************/
        private void RedEMSpot1Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedAttackBuff1Active = true;
            Master.Instance._redAttackBuff1StartTime = DateTime.Now;
            RedAttackbuff1TimeTextBlock.IsEnabled = true;
            RedEMSpot1Button.IsEnabled = false;
        }

        private void RedEMSpot2Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedAttackBuff2Active = true;
            Master.Instance._redAttackBuff2StartTime = DateTime.Now;
            RedAttackbuff2TimeTextBlock.IsEnabled = true;
            RedEMSpot2Button.IsEnabled = false;
        }

        private void RedEMSpot3Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.RedHealing = true;
            if (!Red12.Robot1.Status.DefeatedFlag) Red12.Robot1.Status.HP = Red12.Robot1.Status.MaxHP;
            if (!Red12.Robot2.Status.DefeatedFlag) Red12.Robot2.Status.HP = Red12.Robot2.Status.MaxHP;
            if (!Red34.Robot1.Status.DefeatedFlag) Red34.Robot1.Status.HP = Red34.Robot1.Status.MaxHP;
            if (!Red34.Robot2.Status.DefeatedFlag) Red34.Robot2.Status.HP = Red34.Robot2.Status.MaxHP;

            if (Master.Instance.GameFormat == Master.GameFormatEnum.FINALS
                && !Red5.Robot1.Status.DefeatedFlag)
                Red5.Robot1.Status.HP = Red5.Robot1.Status.MaxHP;

            RedEMSpot3Button.IsEnabled = false;
        }

        private void RedEMSpot4Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedAttackBuff4Active = true;
            Master.Instance._redAttackBuff4StartTime = DateTime.Now;
            RedAttackbuff4TimeTextBlock.IsEnabled = true;
            RedEMSpot4Button.IsEnabled = false;
        }

        private void RedEMSpot5Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedAttackBuff5Active = true;
            Master.Instance._redAttackBuff5StartTime = DateTime.Now;
            RedAttackbuff5TimeTextBlock.IsEnabled = true;
            RedEMSpot5Button.IsEnabled = false;
        }

        private void BlueEMSpot1Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueAttackBuff1Active = true;
            Master.Instance._blueAttackBuff1StartTime = DateTime.Now;
            BlueAttackbuff1TimeTextBlock.IsEnabled = true;
            BlueEMSpot1Button.IsEnabled = false;
        }

        private void BlueEMSpot2Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueAttackBuff2Active = true;
            Master.Instance._blueAttackBuff2StartTime = DateTime.Now;
            BlueAttackbuff2TimeTextBlock.IsEnabled = true;
            BlueEMSpot2Button.IsEnabled = false;
        }

        private void BlueEMSpot3Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.BlueHealing = true;
            if (!Blue12.Robot1.Status.DefeatedFlag) Blue12.Robot1.Status.HP = Blue12.Robot1.Status.MaxHP;
            if (!Blue12.Robot2.Status.DefeatedFlag) Blue12.Robot2.Status.HP = Blue12.Robot2.Status.MaxHP;
            if (!Blue34.Robot1.Status.DefeatedFlag) Blue34.Robot1.Status.HP = Blue34.Robot1.Status.MaxHP;
            if (!Blue34.Robot2.Status.DefeatedFlag) Blue34.Robot2.Status.HP = Blue34.Robot2.Status.MaxHP;

            if (Master.Instance.GameFormat == Master.GameFormatEnum.FINALS
                && !Blue5.Robot1.Status.DefeatedFlag)
                Blue5.Robot1.Status.HP = Blue5.Robot1.Status.MaxHP;

            BlueEMSpot3Button.IsEnabled = false;
        }

        private void BlueEMSpot4Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueAttackBuff4Active = true;
            Master.Instance._blueAttackBuff4StartTime = DateTime.Now;
            BlueAttackbuff4TimeTextBlock.IsEnabled = true;
            BlueEMSpot4Button.IsEnabled = false;
        }

        private void BlueEMSpot5Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueAttackBuff5Active = true;
            Master.Instance._blueAttackBuff5StartTime = DateTime.Now;
            BlueAttackbuff5TimeTextBlock.IsEnabled = true;
            BlueEMSpot5Button.IsEnabled = false;
        }

        /***** ストライダーによるバフ効果 *******************************************************************************************************/
        private void RedZone1Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsRedZone1ShieldBuffActive = true;
            Master.Instance._redZone1ShieldBuffStartTime = DateTime.Now;
            RedZone1TimeTextBlock.IsEnabled = true;

            RedZone1Button.IsEnabled = false;
            RedZone2Button.IsEnabled = true;
        }

        private void RedZone2Button_Click(object sender, RoutedEventArgs e) {
            if (Master.Instance.IsRedZone1ShieldBuffActive) { // ゾーン1のバフ効果が継続中
                Master.Instance.IsRedZone2ShieldBuffActiveWaiting = true;
            } else {
                Master.Instance.IsRedZone2ShieldBuffActive = true;
                Master.Instance._redZone2ShieldBuffStartTime = DateTime.Now;
            }
            RedZone2TimeTextBlock.IsEnabled = true;
            RedZone2Button.IsEnabled = false;
        }

        private void BlueZone1Button_Click(object sender, RoutedEventArgs e) {
            Master.Instance.IsBlueZone1ShieldBuffActive = true;
            Master.Instance._blueZone1ShieldBuffStartTime = DateTime.Now;
            BlueZone1TimeTextBlock.IsEnabled = true;

            BlueZone1Button.IsEnabled = false;
            BlueZone2Button.IsEnabled = true;
        }

        private void BlueZone2Button_Click(object sender, RoutedEventArgs e) {
            if (Master.Instance.IsBlueZone1ShieldBuffActive) { // ゾーン1のバフ効果が継続中
                Master.Instance.IsBlueZone2ShieldBuffActiveWaiting = true;
            } else {
                Master.Instance.IsBlueZone2ShieldBuffActive = true;
                Master.Instance._blueZone2ShieldBuffStartTime = DateTime.Now;
            }
            BlueZone2TimeTextBlock.IsEnabled = true;
            BlueZone2Button.IsEnabled = false;
        }

        /***** デバフのcallback *******************************************************************************************************/

        private void ShieldRed1ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldRed1ToggleButton.IsChecked == true) {
                Red12.Robot1.Status.IsShieldBuffActive = true;
            } else {
                Red12.Robot1.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldRed2ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldRed2ToggleButton.IsChecked == true) {
                Red12.Robot2.Status.IsShieldBuffActive = true;
            } else {
                Red12.Robot2.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldRed3ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldRed3ToggleButton.IsChecked == true) {
                Red34.Robot1.Status.IsShieldBuffActive = true;
            } else {
                Red34.Robot1.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldRed4ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldRed4ToggleButton.IsChecked == true) {
                Red34.Robot2.Status.IsShieldBuffActive = true;
            } else {
                Red34.Robot2.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldRed5ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldRed5ToggleButton.IsChecked == true) {
                Red5.Robot1.Status.IsShieldBuffActive = true;
            } else {
                Red5.Robot1.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldBlue1ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldBlue1ToggleButton.IsChecked == true) {
                Blue12.Robot1.Status.IsShieldBuffActive = true;
            } else {
                Blue12.Robot1.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldBlue2ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldBlue2ToggleButton.IsChecked == true) {
                Blue12.Robot2.Status.IsShieldBuffActive = true;
            } else {
                Blue12.Robot2.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldBlue3ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldBlue3ToggleButton.IsChecked == true) {
                Blue34.Robot1.Status.IsShieldBuffActive = true;
            } else {
                Blue34.Robot1.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldBlue4ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldBlue4ToggleButton.IsChecked == true) {
                Blue34.Robot2.Status.IsShieldBuffActive = true;
            } else {
                Blue34.Robot2.Status.IsShieldBuffActive=false;
            }
        }

        private void ShieldBlue5ToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (ShieldBlue5ToggleButton.IsChecked == true) {
                Blue5.Robot1.Status.IsShieldBuffActive = true;
            } else {
                Blue5.Robot1.Status.IsShieldBuffActive=false;
            }
        }

        /***** チームごとの試合勝利数入力callback *******************************************************************************************************/
        private void RedNumWinsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            var converter = new System.Windows.Media.BrushConverter();
            RedNumWinsTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#30FF0000");
        }

        private void RedNumWinsTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                try {
                    Master.Instance.NumRedWins = int.Parse(RedNumWinsTextBox.Text);
                    var converter = new System.Windows.Media.BrushConverter();
                    RedNumWinsTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                    Master.Instance.SettingsChanged = true;
                } catch {
                    ;
                }
            }
        }

        public void EnterRedNumWins() {
            try {
                Master.Instance.NumRedWins = int.Parse(RedNumWinsTextBox.Text);
                var converter = new System.Windows.Media.BrushConverter();
                RedNumWinsTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                Master.Instance.SettingsChanged = true;
            } catch {
                ;
            }
        }

        private void BlueNumWinsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            var converter = new System.Windows.Media.BrushConverter();
            BlueNumWinsTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#30FF0000");
        }

        private void BlueNumWinsTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                try {
                    Master.Instance.NumBlueWins = int.Parse(BlueNumWinsTextBox.Text);
                    var converter = new System.Windows.Media.BrushConverter();
                    BlueNumWinsTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                    Master.Instance.SettingsChanged = true;
                } catch {
                    ;
                }
            }
        }

        public void EnterBlueNumWins() {
            try {
                Master.Instance.NumBlueWins = int.Parse(BlueNumWinsTextBox.Text);
                var converter = new System.Windows.Media.BrushConverter();
                BlueNumWinsTextBox.Background = (System.Windows.Media.Brush)converter.ConvertFromString("#3000FF00");
                Master.Instance.SettingsChanged = true;
            } catch {
                ;
            }
        }

        /***** 手動判定の際のキーボードショートカット *******************************************************************************************************/
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (!Master.Instance.IsGameRunning) return;

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
        }
    }
}
