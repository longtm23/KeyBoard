using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;
using BanPhimCung.DTO;
using System.IO;

namespace BanPhimCung.Ultility
{
    public class MRW_Common
    {
        public static byte[] ConvertStringToByte(string str, bool insertLength = false)
        {
            var _char = str.ToCharArray().Select(c => c.ToString()).ToArray();

            var length = str.Length * 4;

            byte[] _d;

            var index = 0;

            if (insertLength)
            {
                length += 1;
                index += 1;
                _d = new byte[length];
                _d[0] = (byte)str.Length;
            }
            else
            {
                _d = new byte[length];
            }

            for (int i = 0; i < _char.Length; i++)
            {
                var _t = Encoding.UTF8.GetBytes(_char[i]);
                Buffer.BlockCopy(_t, 0, _d, index + i * 4, _t.Length);
            }

            return _d;
        }

        #region CONVERT OBJECT TO JSON
        public static string ConvertObjectToJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
        #endregion
        #region CONVERT String TO Object
        public static object ConvertStringToObject(string value)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(value);
        }
        #endregion
        #region CONVERT String TO Object
        public static string RandomString(int length)
        {
            Random rd = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, length).Select(x => pool[rd.Next(0, pool.Length)]);
            return new string(chars.ToArray());
        }
        #endregion
        public static string MathTimeIntToString(int timeTick)
        {
            TimeSpan time = TimeSpan.FromSeconds(timeTick);

            //here backslash is must to tell that colon is
            //not the part of format, it just a character that we want in output
            return time.ToString(@"hh\:mm\:ss");
        }

        public static DateTime ConvertFromUnixTimestamp(int timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }
        public static string ConvertFromUnixTimestampToString(int timestamp)
        {
            DateTime a = ConvertFromUnixTimestamp(timestamp);
            return ConvertFromUnixTimestamp(timestamp).ToLocalTime().ToString(@"hh\:mm\:ss");
        }

        public static int ConverSubTimeToHour(int mtime)
        {
            return (int)(DateTime.UtcNow.Subtract(ConvertFromUnixTimestamp(mtime))).TotalSeconds;
        }
        public static Int32 GetTimeUnix()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static ConfigKeyBoard GetConfigByFileConfig()
        {
            try
            {
                Serializer ser = new Serializer();
                string path = string.Empty;
                string xmlInputData = string.Empty;
                string xmlOutputData = string.Empty;

                path = Directory.GetCurrentDirectory() + @"\ConfigKeyBoard.xml";
                xmlInputData = File.ReadAllText(path);

                ConfigKeyBoard customer2 = ser.Deserialize<ConfigKeyBoard>(xmlInputData);
                xmlOutputData = ser.Serialize<ConfigKeyBoard>(customer2);
                if (customer2.IsHttps)
                {
                    customer2.https = "https://" + customer2.HttpCetm;
                }
                else
                {
                    customer2.https = "http://" + customer2.HttpCetm;
                }
                return customer2;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }








    }
}
