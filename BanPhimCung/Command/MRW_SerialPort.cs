using BanPhimCung.Controller;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BanPhimCung.Command
{
    public class MRW_SerialPort
    {
        WriteLog log = new WriteLog();
        private string comName = "";
        private const string DEVICENAME = "USB-SERIAL CH340";
        private bool isSended = false;
        private const int bauRate = 19200;
        public enum Status
        {
            Opened,
            Closed,
            Opening,
            Closing,
            CloseError,
            OpenError,
            SendError,
            ReceivedEror
        }

        //private void setTimerSend()
        //{
        //    // Create a timer with a two second interval.

        //        var aTimePing = new System.Timers.Timer(200);
        //        // Hook up the Elapsed event for the timer. 
        //        aTimePing.Elapsed += onSendData;
        //        aTimePing.AutoReset = true;
        //        aTimePing.Enabled = true;
        //}
        //private void onSendData(Object obj, System.Timers.ElapsedEventArgs e)
        //{
        //    Console.WriteLine("WAIT TIME BUSY");
        //    if (!comSendWorker.IsBusy)
        //    {
        //        Console.WriteLine("CANCEL WAIT TIME BUSY");
        //        var a = (System.Timers.Timer)obj;
        //        a.Dispose();
        //        comSendWorker.RunWorkerAsync();
                
        //    }
        //}

        private SerialPort serialPort = null;

        public delegate void ReceivedEventHandler(byte[] data);

        public event ReceivedEventHandler DataReceived;

        private Queue<byte[]> sendQueue = null;
        private readonly BackgroundWorker comSendWorker = new BackgroundWorker();
        private readonly Mutex mQueue = new Mutex();
        private readonly Mutex mIsend = new Mutex();
        public MRW_SerialPort()
        {
            InitWorker();
        }

        public MRW_SerialPort(string comName)
        {

            sendQueue = new Queue<byte[]>();
            InitWorker();
            this.serialPort = new SerialPort(comName, bauRate);
            serialPort.DataReceived += serialPortReceived;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            //setTimerSend();
            //serialPort.ReadTimeout = 2000;
            //serialPort.WriteTimeout = 1000;
            try
            {
                openPort();
            }
            catch { }

        }
        void initPort(string comName)
        {
            this.serialPort = new System.IO.Ports.SerialPort(comName.ToUpper(), bauRate);
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            this.serialPort.DataReceived += serialPortReceived;


            
            //serialPort.ReadTimeout = 2000;
            //serialPort.WriteTimeout = 1000;
            try
            {
                openPort();
            }
            catch { }
        }
        public void InitWorker()
        {
            //comSendWorker.WorkerSupportsCancellation = true;
            comSendWorker.DoWork += comSendWorker_DoWork;
            comSendWorker.RunWorkerCompleted += readRunWorkerCompleted;
            comSendWorker.RunWorkerAsync();

        }
        private void readRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Thread.Sleep(100);
            if (!comSendWorker.IsBusy)
            {
                comSendWorker.RunWorkerAsync();
            }
        }

        public void serialPortReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Console.WriteLine("Vao nhan data break");
            var byteRes = ReadByteInPort(true);
           
            if (byteRes != null)
            {
                var a = "";
                foreach (var b in byteRes)
                {
                    a += b + " ";
                }
                Console.WriteLine("DATA RECIVE: " + a);
            }
            
            DataReceived(byteRes);
        }
        private void setIsend(bool val)
        {
            mIsend.WaitOne();
            isSended = val;
            mIsend.ReleaseMutex();
        }
        private bool checkSend()
        {
            var isCheck = false;
            mIsend.WaitOne();
            isCheck = isSended;
            mIsend.ReleaseMutex();
            return isCheck;
        }
        private byte[] ReadByteInPort(bool isReturnSend)
        {
            byte[] r = null;
            try
            {
                var bytes = serialPort.BytesToRead;
                r = new byte[bytes];
                serialPort.Read(r, 0, bytes);
            }
            catch (TimeoutException ex)
            {
                log.sendLog("Exception read byte in port: " + ex.ToString());
            }
            catch (Exception ex)
            {
                log.sendLog("Exception read byte in port: " + ex.ToString());
            }
            finally
            {
                if (isReturnSend && r.Length > 0)
                {
                    Console.WriteLine("VAO SET FALSE");
                    setIsend(false);
                }

               // if (serialPort != null && serialPort.IsOpen && serialPort.BytesToRead != 0) serialPort.DiscardInBuffer();
            }

            return r;
        }

        private void waitForResult(int device)
        {
            if (device != (int)CheckByteSend.DEVICE_ID.DEVICE_LED)
            {
                double WaitTimeout = 500 + DateTime.Now.TimeOfDay.TotalMilliseconds;
                while (!(DateTime.Now.TimeOfDay.TotalMilliseconds >= WaitTimeout) && isSended)
                {
                    //int BytesToRead = 0;
                    //try
                    //{
                    //    BytesToRead = serialPort.BytesToRead;
                    //}
                    //catch { }
                    //if (BytesToRead > 0)
                    //{
                    //    isSended = false;
                    //    return;
                    //}
                    Thread.Sleep(28);
                }

            }
            else
            {
                double WaitTimeout = 200 + DateTime.Now.TimeOfDay.TotalMilliseconds;
                while (!(DateTime.Now.TimeOfDay.TotalMilliseconds >= WaitTimeout) && isSended)
                {
                    Thread.Sleep(100);
                }
            }
            setIsend(false);
            return;
        }

        public string[] PortName { get { return SerialPort.GetPortNames(); } }
        public bool openPort()
        {
            if (!serialPort.IsOpen)
            {
                try
                {
                    log.sendLog("opening Port " + comName);
                    serialPort.Open();
                    log.sendLog("opened Port" + comName);
                    return true;
                }
                catch (Exception e)
                {
                    comName = getPortCommonWindows();
                    if (comName.Length > 0)
                    {
                        initPort(comName);
                    }
                    log.sendLog("Open port: " + e);
                    return true;
                }
            }
            return true;
        }

        public void SendData(byte[] data)
        {

            mQueue.WaitOne();
            sendQueue.Enqueue(data);
            mQueue.ReleaseMutex();
            //Console.WriteLine("VAO DAY", data);
            try
            {
                comSendWorker.RunWorkerAsync();
                Console.WriteLine("VAO RANH");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //setTimerSend();
            }
        }

        private bool checkMQueue()
        {
            bool isCheck = false;
            mQueue.WaitOne();
            if (sendQueue != null && sendQueue.Count() > 0)
            {
                isCheck = true;
            }
            mQueue.ReleaseMutex();
            return isCheck;

        }

        private void runSendData()
        {
            if (checkMQueue() && !checkSend())
            {
                mQueue.WaitOne();
                var data = sendQueue.Dequeue();
                mQueue.ReleaseMutex();
                Console.WriteLine("BYTE SEND: ");
                foreach (var a in data)
                {
                    Console.Write(a + ", ");
                }
                Console.WriteLine("");

                SendDataToKeyBoard(data);
                if (data.Length > 1)
                {
                    var device = data[1];
                    waitForResult(device);
                }
            }
        }
        private void comSendWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Cancel)
            {
                InitWorker();
                return;
            }
            while (!comSendWorker.CancellationPending && checkMQueue())
            {
                if (!isSended)
                {
                    mQueue.WaitOne();
                    var data = sendQueue.Dequeue();
                    mQueue.ReleaseMutex();
                    Console.WriteLine("BYTE SEND: ");
                    foreach (var a in data)
                    {
                        Console.Write(a + ", ");
                    }
                    Console.WriteLine("");
                    SendDataToKeyBoard(data);
                    if (data.Length > 1)
                    {
                        var device = data[1];
                        waitForResult(device);
                        Thread.Sleep(100);
                    }
                }
            }
        }
        private string getPortCommonWindows()
        {
            var coms = SerialPort.GetPortNames();
            var lsCom = new List<string>();
            ManagementObjectCollection collection = null;
            using (var searcher = new ManagementObjectSearcher("Select * From Win32_PnPEntity"))
                collection = searcher.Get();
            foreach (var device in collection)
            {
                var des = (string)device.GetPropertyValue("Description");
                var caption = (string)device.GetPropertyValue("Caption");
                if (!string.IsNullOrWhiteSpace(des) && !string.IsNullOrWhiteSpace(caption))
                {
                    foreach (var com in coms)
                    {
                        if (caption.Contains(com) && des.Contains(DEVICENAME))
                        {
                            return com;
                        }
                    }
                }
            }
            return "";
        }
        private void SendDataToKeyBoard(byte[] data)
        {
            if (openPort())
            {
                try
                {
                    isSended = true;
                    serialPort.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    isSended = false;
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
