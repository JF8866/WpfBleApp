using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using WpfBleApp.Utils;

namespace WpfBleApp.Ble
{
    public static class BleExtensions
    {

        /// <summary>
        /// 通过 DeviceInformation 获取 MAC 地址
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static string Mac(this DeviceInformation info)
        {
            return info.Properties["System.Devices.Aep.DeviceAddress"]?.ToString()?.ToUpper();
        }


        public static string Mac(this BluetoothLEAdvertisementReceivedEventArgs args)
        {
            ulong address = args.BluetoothAddress;
            int len = 6;
            byte[] macArr = new byte[len];
            for (int i = 0; i < len; i++)
            {
                macArr[i] = (byte)((address >> ((len - 1 - i) * 8)) & 0xff);
            }
            //macArr = $"{args.BluetoothAddress:X12}".ToData();
            return string.Join(":", macArr.Select(b => $"{b:X2}"));
        }

        public static string LocalName(this BluetoothLEAdvertisementReceivedEventArgs args)
        {
            return args.Advertisement.LocalName;
        }

        public static byte[] MfrData(this BluetoothLEAdvertisementReceivedEventArgs args)
        {
            foreach (BluetoothLEAdvertisementDataSection section in args.Advertisement.DataSections)
            {
                if (section.DataType == 0xff)
                {
                    return section.Data.ToBytes();
                }
            }
            return null;
        }

        //
    }
}
