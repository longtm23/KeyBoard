using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.Command
{
    public class MRW_ModBus
    {

        int messageId;

        public MRW_ModBus(int messageId = 1)
        {
            MessageId = messageId;
        }

        private int MessageId
        {
            get { return messageId; }
            set { messageId = value; }
        }

        public byte[] BuildText(int Device, int Address, int Command, string Data, int service)
        {
            if (string.IsNullOrWhiteSpace(Data))
            {
                Data = "";
            }
            var temp = Data.ToCharArray().Select(c => c.ToString()).ToArray();

            var data = new byte[temp.Length * 4 + 1];//1byte service

            for (int i = 0; i < temp.Length; i++)
            {
                var t = Encoding.UTF8.GetBytes(temp[i]);
                Buffer.BlockCopy(t, 0, data, i * 4, t.Length);
            }
            if (service > 0)
            {
                data[data.Length - 1] = Convert.ToByte("" + service);
            }
            return Build(Device, Address, Command, data);

        }

        public byte[] BuildTextSendLed(int Device, int Address, int Command, string Data)
        {
            var temp = Data.ToCharArray().Select(c => c.ToString()).ToArray();

            var data = new byte[temp.Length * 4];

            for (int i = 0; i < temp.Length; i++)
            {
                var t = Encoding.UTF8.GetBytes(temp[i]);
                Buffer.BlockCopy(t, 0, data, i * 4, t.Length);
            }
            return Build(Device, Address, Command, data);
        }

        public byte[] BuildTextError(int Device, int Address, int Command, string Data)
        {
            var temp = Data.ToCharArray().Select(c => c.ToString()).ToArray();

            var data = new byte[temp.Length * 4 ];//1byte service

            for (int i = 0; i < temp.Length; i++)
            {
                var t = Encoding.UTF8.GetBytes(temp[i]);
                Buffer.BlockCopy(t, 0, data, i * 4, t.Length);
            }
            return Build(Device, Address, Command, data);
        }

        public byte[] BuildTextAllRetore(int Device, int Address, int Command, List<Ticket> lstNum, Dictionary<string, Service> dicServices)
        {
            int lengData = 0;
            foreach (var num in lstNum)
            {
                lengData += num.CNum.Length*4 + 2;
            }
            byte[] newData = new byte[lengData + 1];
            newData[0] = Convert.ToByte("" + lstNum.Count());
            lengData = 1;
            foreach (var num in lstNum)
            {
                string serviceID = num.Service_Id;
                if (string.IsNullOrWhiteSpace(serviceID))
                {
                    serviceID = num.Services[0];
                }
                var temp = num.CNum.ToCharArray().Select(c => c.ToString()).ToArray();
                var lengTem = temp.Length * 4 + 2;
                var dataNum = new byte[lengTem];//1byte do dai so
                dataNum[0] = Convert.ToByte("" + num.CNum.Length);
                for (int i = 0; i < temp.Length; i++)
                {
                    var t = Encoding.UTF8.GetBytes(temp[i]);
                    Buffer.BlockCopy(t, 0, dataNum, i * 4 + 1, t.Length);
                }
                dataNum[lengTem - 1] = Convert.ToByte("" + getIndexService(serviceID, dicServices));
                Buffer.BlockCopy(dataNum, 0, newData, lengData, lengTem);
               // newData[lengTem + 1] = Convert.ToByte("" + getIndexService(num.service_id, dicServices));
                //lengData += lengTem + 1;
                lengData += lengTem;
            }
            return Build(Device, Address, Command, newData);

        }

        private static int getIndexService(string serviceID, Dictionary<string, Service> dicServices)
        {
            var index = Array.FindIndex(dicServices.Values.ToArray(), m => m.Id.Contains(serviceID));
            return index;
        }

        public static int GetMessageId(byte[] data)
        {
            return (data[10] << 24) + (data[9] << 16) + (data[8] << 8) + data[7];
        }
        public static byte BYTE_START = 0x3A;
        public byte[] Build(int Device, int Address, int Command, byte[] Data)
        {
            var temp = new byte[15 + Data.Length];

            var crc = CRCCalculate(Data);
            temp[0] = BYTE_START; temp[1] = (byte)Device; temp[2] = (byte)Address; temp[3] = (byte)(Command % 256);
            temp[4] = (byte)(Command / 256);
            temp[5] = (byte)((Data.Length + 4) % 256);
            temp[6] = (byte)((Data.Length + 4) / 256);
            temp[7] = (byte)MessageId;
            temp[8] = (byte)(MessageId >> 8);
            temp[9] = (byte)(MessageId >> 16);
            temp[10] = (byte)(MessageId >> 24);
            for (int i = 11; i < Data.Length + 11; i++)
            {
                temp[i] = Data[i - 11];
            }
            temp[Data.Length + 11] = (byte)(crc % 256);
            temp[Data.Length + 12] = (byte)(crc / 256);
            temp[Data.Length + 13] = 0x10;
            temp[Data.Length + 14] = 0x13;

            MessageId = MessageId + 1;

            return temp;
        }

        public enum SendCommand : int
        {
            Next = 0x30,
            Recall = 0x31,
            Delete = 0x32,
            Finish = 0x33,
            Forward = 0x34,
            Restore = 0x36,
            LoadService = 0x102,
            LoadServiceDone = 0x103,
            LoadCounter = 0x104,
            LoadCounterDone = 0x105,
        }

        public enum RecivedCommand : int
        {
            SetService = 0x102,
            SetCounter = 0x104,
        }
        private int deviceID = 2;
        public byte[] SetService(int address, List<string> services)
        {
            int _serviceCount = services.Count();

            byte[] _data = new byte[] { (byte)_serviceCount };

            byte[] _temp;

            foreach (var s in services)
            {
                _temp = new byte[_data.Length + s.Length * 4 + 1];

                Array.Copy(_data, _temp, _data.Length);

                var _d = MRW_Common.ConvertStringToByte(s, true);

                Buffer.BlockCopy(_d, 0, _temp, _data.Length, _d.Length);

                _data = _temp;
            }

            return Build(deviceID, address, (int)RecivedCommand.SetService, _data);

        }

        public byte[] SetCounter(int address, string specialName, List<Counter> nameCounters, Dictionary<int, KeyBoardCounter> dicKeyAdd)
        {
            int _counterCount = nameCounters.Count();

            int addressCouOther = 100;
            byte[] _data = new byte[] { (byte)_counterCount };

            byte[] _temp;

            foreach (var s in nameCounters)
            {
                var lengData = _data.Length;
                _temp = new byte[lengData + s.Name.ToString().Length * 4 + 2];

                Array.Copy(_data, _temp, lengData);
                var add = dicKeyAdd.FirstOrDefault(m => m.Value.CounterID == s.Id).Key;
                byte byteAdd;
                if (add == 0)
                {
                    add = addressCouOther;
                    addressCouOther++;
                }

                byteAdd = Convert.ToByte("" + add);
                _temp[lengData] = byteAdd;

                var _d = MRW_Common.ConvertStringToByte(s.Name.ToString(), true);

                Buffer.BlockCopy(_d, 0, _temp, lengData + 1, _d.Length);

                _data = _temp;
            }

            _temp = new byte[_data.Length + specialName.Length * 4 + 1];

            var _dSpecial = MRW_Common.ConvertStringToByte(specialName, true);

            Buffer.BlockCopy(_data, 0, _temp, _dSpecial.Length, _data.Length);
            Buffer.BlockCopy(_dSpecial, 0, _temp, 0, _dSpecial.Length);

            return Build(deviceID, address, (int)RecivedCommand.SetCounter, _temp);

        }

        public byte[] SetCounter(int address, string specialName, List<string> nameCounters, int add)
        {
            int _counterCount = nameCounters.Count();

            int addressCouOther = 100;
            byte[] _data = new byte[] { (byte)_counterCount };

            byte[] _temp;

            foreach (var s in nameCounters)
            {
                var lengData = _data.Length;
                _temp = new byte[lengData + s.Length * 4 + 2];

                Array.Copy(_data, _temp, lengData);
                //var add = dicKeyAdd.FirstOrDefault(m => m.Value.CounterID == s.Id).Key;
                byte byteAdd;
                if (add == 0)
                {
                    add = addressCouOther;
                    addressCouOther++;
                }

                byteAdd = Convert.ToByte("" + add);
                _temp[lengData] = byteAdd;

                var _d = MRW_Common.ConvertStringToByte(s, true);

                Buffer.BlockCopy(_d, 0, _temp, lengData + 1, _d.Length);

                _data = _temp;
            }

            _temp = new byte[_data.Length + specialName.Length * 4 + 1];

            var _dSpecial = MRW_Common.ConvertStringToByte(specialName, true);

            Buffer.BlockCopy(_data, 0, _temp, _dSpecial.Length, _data.Length);
            Buffer.BlockCopy(_dSpecial, 0, _temp, 0, _dSpecial.Length);

            return Build(deviceID, address, (int)RecivedCommand.SetCounter, _temp);

        }
        private int CRCCalculate(byte[] Data)
        {
            byte uchCRCHi = 0xFF;

            byte uchCRCLo = 0xFF;

            int temp = 0;

            int uIndex;

            for (int i = 0; i < Data.Length; i++)
            {
                uIndex = uchCRCLo ^ Data[i];
                uchCRCLo = (byte)(uchCRCHi ^ auchCRCHi[uIndex]);
                uchCRCHi = auchCRCLo[uIndex];
            }

            temp = uchCRCHi;
            temp = (temp << 8) | uchCRCLo;

            return temp;
        }


        static readonly byte[] auchCRCHi = new byte[] {0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                        0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01,
                        0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                        0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                        0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00,
                        0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40};

        static readonly byte[] auchCRCLo = new byte[] {0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9,
                        0x09, 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3, 0x11, 0xD1,
                        0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38,
                        0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26, 0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60,
                        0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9,
                        0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5, 0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91, 0x51, 0x93,
                        0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A,
                        0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C, 0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80, 0x40};

    }

}
