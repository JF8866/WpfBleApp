using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfBleApp.Ble
{
    public class Uuids
    {
        public const string SERVICE = "00001000-0000-1000-8000-00805f9b34fb";
        public const string WRITE = "00001001-0000-1000-8000-00805f9b34fb";
        public const string NOTIFY = "00001002-0000-1000-8000-00805f9b34fb";

        public const string GenericAccess = "00001800-0000-1000-8000-00805f9b34fb";
        public const string DeviceName = "00002a00-0000-1000-8000-00805f9b34fb";
    }
}
