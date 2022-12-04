using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;
using WpfBleApp.Utils;

namespace WpfBleApp.Ble
{
    public class BleManager
    {
        // https://learn.microsoft.com/zh-cn/windows/uwp/devices-sensors/gatt-client
        // Query for extra properties you want returned
        private const string KeyIsConnected = "System.Devices.Aep.IsConnected";
        private readonly string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", KeyIsConnected };

        private readonly DeviceWatcher deviceWatcher;
        private readonly BluetoothLEAdvertisementWatcher advertisementWatcher = new BluetoothLEAdvertisementWatcher();

        public static readonly BleManager Instance = new BleManager();

        public event TypedEventHandler<BleManager, object> Started;
        public event TypedEventHandler<BleManager, BleDevice> FoundDevice;
        public event TypedEventHandler<BleManager, object> Stopped;

        //BleDevice 需要通过 MAC 组合 DeviceInformation 和 BluetoothLEAdvertisement 的数据
        //DeviceInformation 有可能是系统缓存的设备信息，以此信息作为缓存，以扫到的实时广播作为发现设备的依据
        //DeviceInformation 主要用来连接设备，广播保证设备的真实性
        private readonly ConcurrentDictionary<string, BleDevice> CachedDevices = new ConcurrentDictionary<string, BleDevice>();


        private BleManager()
        {
            deviceWatcher = DeviceInformation.CreateWatcher(
                           //BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                           BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Disconnected),
                            requestedProperties, DeviceInformationKind.AssociationEndpoint);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            advertisementWatcher.ScanningMode = BluetoothLEScanningMode.Passive;
            advertisementWatcher.Received += AdvertisementWatcher_Received;
            advertisementWatcher.Stopped += AdvertisementWatcher_Stopped;
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Log.Info("DeviceWatcher_Stopped");
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Log.Info("DeviceWatcher_EnumerationCompleted");
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //Log.Info("DeviceWatcher_Removed");
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Log.Info("################# DeviceWatcher_Updated #################");
            foreach (KeyValuePair<string, object> item in args.Properties)
            {
                Log.Info($"{item.Key}={item.Value}");
            }
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            //Log.Info("DeviceWatcher_Added");
            if (!CachedDevices.ContainsKey(args.Mac()))
            {
                CachedDevices[args.Mac()] = new BleDevice(args);
            }
        }

        private void AdvertisementWatcher_Stopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Log.Info("AdvertisementWatcher_Stopped");
            Stopped?.Invoke(this, args);
        }

        private void AdvertisementWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Log.Info("~~~~~~~~~~~~~~ 广播信息 ~~~~~~~~~~~~~~");
            Log.Info($"IsScanResponse={args.IsScanResponse}");
            Log.Info($"LocalName={args.Advertisement.LocalName}");
            Log.Info($"BluetoothAddress={args.Mac()}");
            Log.Info($"rssi={args.RawSignalStrengthInDBm}");
            PrintAdvertisement(args.Advertisement);

            if (CachedDevices.TryGetValue(args.Mac(), out BleDevice device))
            {
                Log.Info($"发现设备 {args.Mac()} 的广播");
                device.Update(args);//更新一下广播数据
                FoundDevice?.Invoke(this, device);
            }
        }


        private void PrintAdvertisement(BluetoothLEAdvertisement advertisement)
        {
            foreach (BluetoothLEManufacturerData mfrData in advertisement.ManufacturerData)
            {
                byte[] data = mfrData.Data.ToBytes();
                byte[] rawBytes = new byte[data.Length + 2];
                rawBytes[0] = (byte)((mfrData.CompanyId) & 0xff);
                rawBytes[1] = (byte)((mfrData.CompanyId >> 8) & 0xff);
                Array.Copy(data, 0, rawBytes, 2, data.Length);
                Log.Info($"厂商数据：{rawBytes.ToHex()}");
            }
            /*foreach (BluetoothLEAdvertisementDataSection section in advertisement.DataSections)
            {
                Log.Info($"Data Type: 0x{section.DataType:X2}, Data: {section.Data.ToHex()}");
            }*/
        }

        public async void StartScan()
        {
            if(advertisementWatcher.Status != BluetoothLEAdvertisementWatcherStatus.Started)
            {
                CachedDevices.Clear();//清空缓存

                advertisementWatcher.Start();
                deviceWatcher.Start();
                Started?.Invoke(this, null);
                await Task.Delay(5000);
                StopScan();
            }
        }

        public void StopScan()
        {
            //判断状态，防止 InvalidOperationException
            if (advertisementWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                advertisementWatcher.Stop();
                deviceWatcher.Stop();
                Log.Info("停止扫描");
            }
        }

        //
    }
}
