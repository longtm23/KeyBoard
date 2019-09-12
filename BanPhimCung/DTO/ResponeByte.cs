using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.DTO
{
    public class ResponeByte
    {
        private int deviceId;

        private int addressKey;

        private int command;

        private int dataLength;

        private byte[] data;

        private int numServicesOrCounter;

        public int NumServicesOrCounter
        {
            get { return numServicesOrCounter; }
            set { numServicesOrCounter = value; }
        }

        private int checkSum;

        private int end;

        private string cnum;

        public int CheckSum
        {
            get { return checkSum; }
            set { checkSum = value; }
        }
        public int DataLength
        {
            get { return dataLength; }
            set { dataLength = value; }
        }
        public int Command
        {
            get { return command; }
            set { command = value; }
        }
        public int AddressKey
        {
            get { return addressKey; }
            set { addressKey = value; }
        }

        public int DeviceId
        {
            get { return deviceId; }
            set { deviceId = value; }
        }

        public int End
        {
            get { return end; }
            set { end = value; }
        }

        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }
        public string Cnum
        {
            get { return cnum; }
            set { cnum = value; }
        }
    }
}
