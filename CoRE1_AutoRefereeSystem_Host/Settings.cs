using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;


namespace CoRE1_AutoRefereeSystem_Host
{
    public class Settings
    {
        public Master.GameFormatEnum GameFormat { get; set; } = Master.GameFormatEnum.NONE;

        public int NumRedWins { get; set; } = 0;
        public int NumBlueWins { get; set; } = 0;
        public string Red1TeamName { get; set; } = "";
        public string Red2TeamName { get; set; } = "";
        public string Red3TeamName { get; set; } = "";
        public string Red4TeamName { get; set; } = "";
        public string Red5TeamName { get; set; } = "";
        public string Red6TeamName { get; set; } = ""; // オートタレット
        public string Red7TeamName { get; set; } = ""; // ストライダー
        public string Blue1TeamName { get; set; } = "";
        public string Blue2TeamName { get; set; } = "";
        public string Blue3TeamName { get; set; } = "";
        public string Blue4TeamName { get; set; } = "";
        public string Blue5TeamName { get; set; } = "";
        public string Blue6TeamName { get; set; } = ""; // オートタレット
        public string Blue7TeamName { get; set; } = ""; // ストライダー

        public Master.RobotTypeEnum Red1RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Red2RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Red3RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Red4RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Red5RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Red6RobotType { get; set; } = Master.RobotTypeEnum.AUTOTURRET;
        public Master.RobotTypeEnum Red7RobotType { get; set; } = Master.RobotTypeEnum.STRIDER;
        public Master.RobotTypeEnum Blue1RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Blue2RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Blue3RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Blue4RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Blue5RobotType { get; set; } = Master.RobotTypeEnum.NONE;
        public Master.RobotTypeEnum Blue6RobotType { get; set; } = Master.RobotTypeEnum.AUTOTURRET;
        public Master.RobotTypeEnum Blue7RobotType { get; set; } = Master.RobotTypeEnum.STRIDER;

        public object Red12ComPort { get; set; } = "";
        public object Red34ComPort { get; set; } = "";
        public object Red5ComPort { get; set; } = "";
        public object Blue12ComPort { get; set; } = "";
        public object Blue34ComPort { get; set; } = "";
        public object Blue5ComPort { get; set; } = "";
        
        public string Red6EndPoint { get; set; } = "192.168.11.200:8888";
        public string Blue6EndPoint { get; set; } = "192.168.11.201:8888";
        public string RedBaseEndPoint { get; set; } = "192.168.11.210:8888";
        public string BlueBaseEndPoint { get; set; } = "192.168.11.211:8888";
        public string CommonBaseEndPoint { get; set; } = "192.168.11.212:8888";
        public string BuffHost { get; set; } = "192.168.11.10:8888";
    }

    public class SettingsManager
    {
        // シングルトン
        private static readonly Lazy<SettingsManager> _instance = new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance => _instance.Value;

        private readonly string settingsFilePath = Path.Combine("config", "Settings.json");
        public bool FileExsits { get; set; } = false;

        private int isBusy = 0;
        public Settings LoadSettings() {
            if (Interlocked.CompareExchange(ref isBusy, 1, 0) != 0)
                throw new BusyException();

            try {
                Directory.CreateDirectory("config");
                
                if (File.Exists(settingsFilePath)) {
                    FileExsits = true;
                    string json = File.ReadAllText(settingsFilePath);
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
                return new Settings();
            } catch (Exception ex) {
                return new Settings();
            } finally {
                Interlocked.Exchange(ref isBusy, 0);
            }
        }

        public void SaveSettings(Settings settings) {
            if (Interlocked.CompareExchange(ref isBusy, 1, 0) != 0)
                throw new BusyException();

            try {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);
            } catch (Exception ex) {
                ;
            } finally {
                Interlocked.Exchange(ref isBusy, 0);
            }
        }
    }

    public class NnChSettings
    {
        public Dictionary<string, int> TeamNodeNo = new Dictionary<string, int> {
            {"AGSR [ATK]", 9999},
            {"AVNT [ATK]", 9999},
            {"KNIT [ATK]", 9999},
            {"MKNG [ATK]", 9999},
            {"RTUS [ATK]", 9999},
            {"SEC [ATK]", 9999},
            {"TMC [ATK]", 9999},
            {"TKG [ATK]", 9999},
            {"TRU [ATK]", 9999},
            {"CNTN [ATK]", 9999},
            {"DTB [ATK]", 9999},
            {"TSTM [ATK]", 9999},
            {"KSHH [ATK]", 9999},
            {"DRBS [ATK]", 9999},
            {"RISN [ATK]", 9999},
            {"KSKN [BLD]", 9999},
            {"AGSR [BLD]", 9999},
            {"TRU [BLD]", 9999},
            {"RISN [BLD]", 9999},
            {"SEC [ATR]", 9999},
            {"TKG [ATR]", 9999},
            {"DTB [ATR]", 9999},
            {"KNIT [STR]", 9999},
            {"MKNG [STR]", 9999},
        };

        public Dictionary<string, int> HostCH = new Dictionary<string, int> {
            {"Red12", 1},
            {"Red34", 3},
            {"Red5", 4},
            {"Blue12", 5},
            {"Blue34", 7},
            {"Blue5", 9}
        };
    }

    public class NnChSettingsManager
    {
        // シングルトン
        private static readonly Lazy<NnChSettingsManager> _instance = new Lazy<NnChSettingsManager>(() => new NnChSettingsManager());
        public static NnChSettingsManager Instance => _instance.Value;


        private readonly string settingsFilePath = Path.Combine("config", "NnChSettings.json");
        public bool FileExsits { get; set; } = false;

        private int isBusy = 0;
        public NnChSettings LoadSettings() {
            if (Interlocked.CompareExchange(ref isBusy, 1, 0) != 0)
                throw new BusyException();

            try {
                Directory.CreateDirectory("config");
                
                if (File.Exists(settingsFilePath)) {
                    FileExsits = true;
                    string json = File.ReadAllText(settingsFilePath);
                    return JsonConvert.DeserializeObject<NnChSettings>(json);
                } else {
                    var settings = new NnChSettings();
                    return settings;
                }
            } catch (Exception ex) {
                return new NnChSettings();
            } finally {
                Interlocked.Exchange(ref isBusy, 0);
            }
        }

        public void SaveSettings(NnChSettings settings) {
            if (Interlocked.CompareExchange(ref isBusy, 1, 0) != 0)
                throw new BusyException();

            try {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsFilePath, json);
            } catch (Exception ex) {
                ;
            } finally {
                Interlocked.Exchange(ref isBusy, 0);
            }
        }
    }

    [Serializable]
    public class BusyException : Exception
    {
        public BusyException() { }
        public BusyException(string message) : base(message) { }
        public BusyException(string message, Exception inner) : base(message, inner) { }
        protected BusyException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context
        ) : base(info, context) { }
    }
}
