using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using WpfBleApp.Ble;
using WpfBleApp.Model;
using WpfBleApp.Utils;

namespace WpfBleApp
{
    /// <summary>
    /// 蓝牙扫描页面
    /// </summary>
    public partial class ScanPage : Page
    {
        private readonly BleManager BleManager = BleManager.Instance;
        private readonly ScanPageViewModel ViewModel = new ScanPageViewModel();

        public ScanPage()
        {
            InitializeComponent();
            DataContext = ViewModel;

            //deviceListBox.ItemsSource = ViewModel.Devices;

            BleManager.Started += BleManager_Started;
            BleManager.FoundDevice += BleManager_FoundDevice;
            BleManager.Stopped += BleManager_Stopped;

            Log.Info($"扫描页面创建 0x{GetHashCode():X8}");
        }

        ~ScanPage()
        {
            Log.Info($"扫描页面销毁 0x{GetHashCode():X8}");
        }

        private void BleManager_Started(BleManager sender, object args)
        {
            btnStartScan.Dispatcher.Invoke(delegate
            {
                btnStartScan.Content = "停止扫描";
            });
        }

        private void BleManager_Stopped(BleManager sender, object args)
        {
            btnStartScan.Dispatcher.Invoke(delegate
            {
                btnStartScan.Content = "开始扫描";
            });
        }

        private void BleManager_FoundDevice(BleManager sender, BleDevice args)
        {
            deviceListBox.Dispatcher.Invoke(() =>
            {
                ViewModel.AddDevice(args);
            });
        }

        private void btnStartScan_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Devices.Clear();
            BleManager.Instance.StartScan();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (deviceListBox.SelectedItem is BleDevice device)
            {
                BleManager.Instance.StopScan();
                NavigationService.Navigate(new CommPage(device));
            }
            else
            {
                MessageBox.Show("请选择要连接的设备！");
            }
        }

        private void deviceListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (deviceListBox.SelectedItem is BleDevice device)
            {
                BleManager.Instance.StopScan();
                NavigationService.Navigate(new CommPage(device));
            }
        }

        //
    }
}
