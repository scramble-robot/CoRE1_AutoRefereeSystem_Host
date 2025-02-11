using System;
using System.IO.Ports;
using System.Management;


namespace CoRE1_AutoRefereeSystem_Host
{
    // USBを抜き差ししたタイミングで自動的に選択可能なCOMポートを更新するためのクラス
    public class COMPortWatcher
    {
        // シングルトン
        private static readonly Lazy<COMPortWatcher> _instance = new Lazy<COMPortWatcher>(() => new COMPortWatcher());
        public static COMPortWatcher Instance => _instance.Value;

        // イベントの定義
        public event Action PortsUpdated;

        private ManagementEventWatcher _watcher;
        private COMPortWatcher() {
            _watcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3"));
            _watcher.EventArrived += OnEventArrived;
            _watcher.Start();
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e) {
            // PortsUpdatedイベントを発火
            PortsUpdated?.Invoke();
        }

        public string[] GetAvailablePorts() {
            return SerialPort.GetPortNames();
        }

        public void Stop() {
            _watcher.Stop();
        }
    }
}
