using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfBleApp.Ble;

namespace WpfBleApp.Model
{
    public class ScanPageViewModel : ObservableObject
    {
        public ObservableCollection<BleDevice> Devices { get; } = new ObservableCollection<BleDevice>();

        public void AddDevice(BleDevice device)
        {
            if (!Devices.Any(d => d.Mac == device.Mac))
            {
                Devices.Add(device);
            }
            else
            {
                Devices.First(d => d.Mac == device.Mac).Update(device);
            }
        }
    }
}
