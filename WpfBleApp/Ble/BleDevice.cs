using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;
using WpfBleApp.Model;
using WpfBleApp.Utils;

using CharacteristicDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic>>;

//线程安全的版本，考虑到连接后不会更改。还是使用性能更好的 Dictionary
//using ConcurrentGattDictionary = System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic>>;

namespace WpfBleApp.Ble
{
    public class BleDevice : ObservableObject
    {
        private readonly DeviceInformation _deviceInfo;
        private BluetoothLEDevice _device;
        private GattCharacteristic CharacteristicWrite;
        private GattCharacteristic CharacteristicNotify;
        private string _mac;
        private string _name;
        private byte[] _mfrData;
        private int _rssi;

        private readonly List<GattDeviceService> GattServices = new List<GattDeviceService>();
        private readonly CharacteristicDictionary GattCharacteristics = new CharacteristicDictionary();

        public event TypedEventHandler<GattCharacteristic, byte[]> CharacteristicValueChanged;
        public event TypedEventHandler<BleDevice, bool> ConnectionStateChanged;

        public BleDevice() { }

        public BleDevice(DeviceInformation deviceInfo)
        {
            _deviceInfo = deviceInfo;
            _mac = deviceInfo.Mac();
            _name = deviceInfo.Name;

            Log.Info("################# BleDevice #################");
            foreach (KeyValuePair<string, object> item in deviceInfo.Properties)
            {
                Log.Info($"{item.Key}={item.Value}");
            }
        }

        public void Update(BleDevice other)
        {
            Name = other.Name;
            MfrData = other.MfrData;
            RSSI = other.RSSI;
        }

        public void Update(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            //通过方法属性更新是为了通知UI刷新
            string localName = args.LocalName();
            //有可能是广播回应包，不带LocalName，防止显示不出真实的名称
            if (!string.IsNullOrEmpty(localName))
            {
                Name = localName;
            }

            byte[] mfrData = args.MfrData();
            if (mfrData != null)
            {
                MfrData = args.MfrData();
            }
            RSSI = args.RawSignalStrengthInDBm;
        }

        public byte[] MfrData
        {
            get => _mfrData;
            set
            {
                Set(ref _mfrData, value);
                OnPropertyChanged(nameof(MfrDataHex));
            }
        }

        public string MfrDataHex => _mfrData?.ToHex();

        public int RSSI
        {
            get => -_rssi;
            set => Set(ref _rssi, value);
        }

        public string Name
        {
            get => string.IsNullOrEmpty(_name) ? "Unknown Device" : _name;
            set => Set(ref _name, value);
        }

        //System.Devices.Aep.DeviceAddress
        public string Mac => _mac;

        public string Id => _deviceInfo.Id;

        public bool IsConnected => _device != null && _device.ConnectionStatus == BluetoothConnectionStatus.Connected;

        /// <summary>
        /// 连接设备
        /// </summary>
        public async void Connect()
        {
            //下面一行是官方文档的写法，貌似版本低所以报错
            //BluetoothLEDevice bluetoothLEDevice = await BluetoothLEDevice.FromIdAsync(Id);

            //使用自定义的拓展方法 GetResultAsync()
            _device = await BluetoothLEDevice.FromIdAsync(Id).GetResultAsync();
            //监听连接状态
            _device.ConnectionStatusChanged += _device_ConnectionStatusChanged;
            Log.Info($"{DateTime.Now:HH:mm:ss.fff} - 开始连接: " + _device);
            DiscoverServices();
        }

        private void _device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            Log.Info("_device_ConnectionStatusChanged() - ConnectionStatus=" + sender.ConnectionStatus);
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
            }
            else
            {
                ConnectionStateChanged?.Invoke(this, false);
            }
        }

        private async void DiscoverServices()
        {
            GattServices.Clear();
            GattCharacteristics.Clear();

            GattDeviceServicesResult servicesResult = await _device.GetGattServicesAsync().GetResultAsync();
            if (servicesResult.Status == GattCommunicationStatus.Success)
            {
                foreach (GattDeviceService service in servicesResult.Services)
                {
                    //存储该服务下的特征值
                    Dictionary<string, GattCharacteristic> characteristics = new Dictionary<string, GattCharacteristic>();

                    string serviceUuid = service.Uuid.ToString();
                    Log.Info($"Gatt Service: {serviceUuid}");
                    GattCharacteristicsResult characteristicsResult = await service.GetCharacteristicsAsync().GetResultAsync();
                    if (characteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (GattCharacteristic characteristic in characteristicsResult.Characteristics)
                        {
                            //存储该特征值
                            characteristics[characteristic.Uuid.ToString()] = characteristic;

                            string characteristicUuid = characteristic.Uuid.ToString();
                            Log.Info($"\tGatt Characteristic: {characteristicUuid}");
                            if (serviceUuid == Uuids.SERVICE)
                            {
                                if (characteristicUuid == Uuids.WRITE)
                                {
                                    CharacteristicWrite = characteristic;
                                }
                                else if (characteristicUuid == Uuids.NOTIFY)
                                {
                                    CharacteristicNotify = characteristic;
                                }
                            }
                        }
                    }
                    GattCharacteristics[service.Uuid.ToString()] = characteristics;
                    GattServices.Add(service);
                }
                //订阅通知
                if (CharacteristicNotify != null)
                {
                    //需得用一个全局变量保持对 ValueChanged 回调函数的引用，不然收不到数据
                    CharacteristicNotify.ValueChanged += CharacteristicNotify_ValueChanged;
                    bool enabled = await EnableNotification(CharacteristicNotify);
                    if (enabled)
                    {
                        Log.Info("订阅通知成功！" + CharacteristicNotify.Uuid);
                    }
                }
                ConnectionStateChanged?.Invoke(this, true);
            }
        }

        private void CharacteristicNotify_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = args.CharacteristicValue.ToBytes();
            Log.Info($"收到数据 <<< {data.ToHex()}");
            CharacteristicValueChanged?.Invoke(sender, data);
        }

        private async Task<bool> EnableNotification(GattCharacteristic characteristic)
        {
            GattCommunicationStatus result = await characteristic
                .WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify
                ).GetResultAsync();
            return result == GattCommunicationStatus.Success;
        }

        public async Task<bool> Write(byte[] data)
        {
            if (CharacteristicWrite == null)
            {
                return false;
            }
            DataWriter writer = new DataWriter();
            writer.WriteBytes(data);
            GattCommunicationStatus result = await CharacteristicWrite
                .WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse)
                .GetResultAsync();
            return result == GattCommunicationStatus.Success;
        }

        public async Task<byte[]> Read(string serviceUuid, string characteristicUuid)
        {
            GattCharacteristic characteristic = GattCharacteristics[serviceUuid]?[characteristicUuid];
            if (characteristic != null)
            {
                GattReadResult result = await characteristic.ReadValueAsync().GetResultAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    byte[] data = result.Value.ToBytes();
                    Log.Info($"读取数据 <<< {data.ToHex()} / {Encoding.UTF8.GetString(data)}");
                    return data;
                }
            }
            else
            {
                Log.Info($"未发现该特征: {serviceUuid} / {characteristicUuid}");
            }
            return null;
        }


        public void Disconnect()
        {
            Log.Info("Disconnect()");

            //必须释放服务才能断开连接
            foreach (GattDeviceService service in GattServices)
            {
                service.Dispose();
            }

            _device?.Dispose();
            //_device = null;//赋值为 null 会导致收不到断线的通知
            //ConnectionStateChanged?.Invoke(this, false);
        }

        public override string ToString()
        {
            return Name + "\r\n" + Mac;
        }
    }
}
