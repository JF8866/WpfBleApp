using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using WpfBleApp.Ble;
using WpfBleApp.Model;
using WpfBleApp.Utils;

namespace WpfBleApp
{
    /// <summary>
    /// 蓝牙通信页面
    /// </summary>
    public partial class CommPage : Page
    {
        private readonly CommPageViewModel ViewModel = new CommPageViewModel();
        private readonly ObservableCollection<string> Logs = new ObservableCollection<string>();
        private int LogCount;

        

        public CommPage()
        {
            InitializeComponent();

            listBoxLog.ItemsSource = Logs;
        }

        private void AddLog(string msg)
        {
            listBoxLog.Dispatcher.Invoke(delegate
            {
                if (LogCount > 500)
                {
                    Logs.RemoveAt(0);
                }
                Logs.Add($"{DateTime.Now:HH:mm:ss.fff} - {msg}");
                LogCount++;

            });
        }

        public CommPage(BleDevice device) : this()
        {
            ViewModel.BleDevice = device;
            ViewModel.BleDevice.CharacteristicValueChanged += BleDevice_CharacteristicValueChanged;
            ViewModel.BleDevice.ConnectionStateChanged += BleDevice_ConnectionStateChanged;
            DataContext = ViewModel;
            device.Connect();
        }

        private void UpdateConnectButton(bool connected)
        {
            buttonConnect.Dispatcher.Invoke(delegate
            {
                if (connected)
                {
                    buttonConnect.Content = "断开连接";
                    buttonConnect.Background = new SolidColorBrush(Color.FromRgb(199, 0, 6));
                    buttonConnect.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    buttonConnect.Content = "连接设备";
                    buttonConnect.Background = new SolidColorBrush(Colors.LightGray);
                    buttonConnect.Foreground = new SolidColorBrush(Colors.Black);
                }
            });
        }

        private void BleDevice_ConnectionStateChanged(BleDevice sender, bool args)
        {
            if (args)
            {
                AddLog("连接成功！");
            }
            else
            {
                AddLog("连接已断开！");
            }
            UpdateConnectButton(args);
        }

        private void BleDevice_CharacteristicValueChanged(GattCharacteristic sender, byte[] args)
        {
            AddLog($"收到数据 <<< {args.ToHex()}");
        }

        private async void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = ViewModel.TxDataHex.ToData();
            bool success = await ViewModel.BleDevice.Write(data);
            if (success)
            {
                AddLog("发送成功 >>> " + data.ToHex());
            }
        }

        private async void buttonRead_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = await ViewModel.BleDevice.Read(Uuids.GenericAccess, Uuids.DeviceName);
            if (data != null)
            {
                AddLog($"读取的设备名称: {Encoding.UTF8.GetString(data)}");
            }
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.BleDevice.IsConnected)
            {
                ViewModel.BleDevice.Disconnect();
            }
            else
            {
                ViewModel.BleDevice.Connect();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Page_Unloaded()");
            ViewModel.BleDevice.CharacteristicValueChanged -= BleDevice_CharacteristicValueChanged;
            ViewModel.BleDevice.ConnectionStateChanged -= BleDevice_ConnectionStateChanged;
            ViewModel.BleDevice.Disconnect();
        }
    }
}
