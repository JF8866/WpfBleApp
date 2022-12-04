using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfBleApp.Ble;

namespace WpfBleApp.Model
{
    class CommPageViewModel : ObservableObject
    {
        private BleDevice _bleDevice;
        private string _txDataHex = "AABBCC112233";

        public BleDevice BleDevice
        {
            get => _bleDevice;
            set => Set(ref _bleDevice, value);
        }

        public string DeviceName
        {
            get => _bleDevice?.Name;
        }

        public string DeviceAddress
        {
            get => _bleDevice?.Mac;
        }

        public string TxDataHex
        {
            get => _txDataHex;
            set => Set(ref _txDataHex, value);
        }
    }
}
