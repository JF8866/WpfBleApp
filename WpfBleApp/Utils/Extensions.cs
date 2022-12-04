using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WpfBleApp.Utils
{
    public static class Extensions
    {
        public static async Task<T> GetResultAsync<T>(this IAsyncOperation<T> asyncOperation){
            return await Task.Run(() =>
            {
                while (asyncOperation.Status == AsyncStatus.Started)
                {
                }
                return asyncOperation.GetResults();//这里会异常
            });
        }

        public static string ToHex(this byte[] data)
        {
            return string.Join(" ", data.Select(b => $"{b:X2}"));
        }

        public static byte[] ToData(this string hex)
        {
            hex = hex.Replace(" ", "").Trim();
            if (hex.Length % 2 == 1)
            {
                hex = $"0{hex}";
            }
            int len = hex.Length / 2;
            byte[] data = new byte[len];
            for (int i = 0; i < len; i++)
            {
                data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return data;
        }

        public static byte[] ToBytes(this IBuffer data)
        {
            byte[] bytes = new byte[data.Length];
            DataReader.FromBuffer(data).ReadBytes(bytes);
            return bytes;
        }

        public static string ToHex(this IBuffer data)
        {
            return data.ToBytes().ToHex();
        }

        //
    }
}
