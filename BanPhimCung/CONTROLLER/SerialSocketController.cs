using BanPhimCung.Command;
using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BanPhimCung.Controller
{
    public sealed class SerialSocketController
    {
        private MRW_ModBus modBus = null;
        private SerialReciveController serialRecive = null;
        private MRW_SerialPort serialPort = null;
        private int DEVICE_KEYBOARD = (int)CheckByteSend.DEVICE_ID.DEVICE_KEYBOARD;
        public delegate void ReceivedEventHandler(EventProgram ev);

        public static readonly SerialSocketController Instance = new SerialSocketController();
        private SerialSocketController() { }

        public event ReceivedEventHandler DataReceived;
        public void InitSerialSocketController(string comName)
        {
            modBus = new MRW_ModBus();
            serialPort = new MRW_SerialPort(comName);
            serialRecive = new SerialReciveController();
            //event recive
            serialPort.DataReceived += serialRecive.Recive;
            serialRecive.DataReceived += dataRecive;
        }

        public void SetDataRecive(Dictionary<string, Service> dicSer, Dictionary<string, Counter> dicCou, Dictionary<int, KeyBoardCounter> dicCouKeyBoard)
        {
            serialRecive.SetData(dicCouKeyBoard, dicCou, dicSer);
        }
        public void SerialPortPing(Dictionary<int, KeyBoardCounter> dicCounterKeyboard)
        {
            if (serialPort.openPort())
            {
                byte[] byteRes = modBus.BuildText(DEVICE_KEYBOARD, dicCounterKeyboard.Values.ToArray()[0].AddressKeyboard, (int)CheckByteSend.BYTE_COMMAND.STATUS_COMMAND, "", 1);
                serialPort.SendData(byteRes);
            }
        }
        private void sendPingLED(Dictionary<int, KeyBoardCounter> dicCounterKeyboard, int command)
        {
            foreach (var add in dicCounterKeyboard.Keys)
            {
                sendDataToLED(add, "", command);
                Thread.Sleep(100);
            }
        }
        private void sendDataToLED(int addCounter, string cnum, int command)
        {
            var byteRes = modBus.BuildTextSendLed((int)CheckByteSend.DEVICE_ID.DEVICE_LED, addCounter, command, cnum);
            serialPort.SendData(byteRes);
        }
        public void SendLED(string action, int addCounter, string data)
        {
            int command = 0;
            switch (action)
            {
                case ActionTicket.INITIAL:
                    command = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                    break;
                //case ActionTicket.ACTION_CREATE: // không làm gì
                //    sendDataToLED(addCounter, data, command);
                //    break;
                case ActionTicket.ACTION_CALL:
                    command = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                    break;
                case ActionTicket.ACTION_RECALL:
                    command = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                    break;
                case ActionTicket.ACTION_RESTORE:
                    command = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                    break;
                case ActionTicket.ACTION_MOVE:
                    command = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                    data = "STOP";
                    break;
                case ActionTicket.ACTION_CANCEL:
                    command = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                    data = "STOP";
                    break;
                case ActionTicket.ACTION_FINISH:
                    command = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                    data = "STOP";
                    break;
                case ActionTicket.ACTION_PING:
                    data = "";
                    command = (int)CheckByteSend.COMMAND_LED.STATUS_COMMAND;
                    break;
                case ActionTicket.MESSAGE_ERROR:
                    data = "STOP";
                    command = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                    break;
                default:
                    //bug
                    break;
            }
            sendDataToLED(addCounter, data, command);
        }
        private void dataRecive(EventProgram ev)
        {
            switch (ev.Action)
            {
                case ActionTicket.MESSAGE_ERROR:
                    SendKeyBoard(ev);
                    break;
                default:
                    DataReceived(ev); break;
            }
        }
        private void serialPortError(string data, int address)
        {
            byte[] byteRes = null;
            var command = (int)CheckByteSend.BYTE_COMMAND.ERROR_COMMAND;
            switch (data)
            {
                case MessageError.ERROR_00:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                case MessageError.ERROR_06:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, 0, command, data, 1);
                    break;
                case MessageError.ERROR_07:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, 0, command, data, 1);
                    break;
                case MessageError.ERROR_13:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                case MessageError.ERROR_12:

                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                case MessageError.ERROR_11:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                case MessageError.ERROR_14:
                     byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                case MessageError.ERROR_09:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                case MessageError.ERROR_01:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 1);
                    break;
                default:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, 13);
                    break;
            }
            serialPort.SendData(byteRes);
        }
        public void SendKeyBoard(EventProgram eventData)
        {
            string action = eventData.Action;
            int command = 0;
            switch (action)
            {
                case ActionTicket.INITIAL_SER:
                    // nạp service
                    var byteService = modBus.SetService(0, eventData.LstServices);
                    serialPort.SendData(byteService);
                    return;
                case ActionTicket.INITIAL_COU:
                    // nạp counter
                    //var byteCounter = modBus.SetCounter(0, "Quầy/Counter", eventData.LstCounter, 0);
                   var byteCounter = modBus.SetCounter(0, "Quầy/ Counter", eventData.LstAllCounter, eventData.DicAddKey);
                    serialPort.SendData(byteCounter);
                    break;
                case ActionTicket.ACTION_RESET:
                    //reset
                    var byteset = modBus.BuildText(DEVICE_KEYBOARD, 0, (int)CheckByteSend.BYTE_COMMAND.RESET_COUNTER, "", -1);
                    serialPort.SendData(byteset);
                    return;
                case "call_hst":
                    command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND;
                    break;
                case ActionTicket.ACTION_ALL_RESTORE:
                    command = (int)CheckByteSend.BYTE_COMMAND.RESTORE_COMMAND;
                    var byteRestore = modBus.BuildTextAllRetore(DEVICE_KEYBOARD, eventData.Address, command, eventData.LstTicket, eventData.DicService);
                    serialPort.SendData(byteRestore);
                    return;
                case ActionTicket.CALL_LIST_WATTING:
                    command = (int)CheckByteSend.BYTE_COMMAND.CALL_LIST_WATTING;
                    var byteCall = modBus.BuildTextAllRetore(DEVICE_KEYBOARD, eventData.Address, command, eventData.LstTicket, eventData.DicService);
                    serialPort.SendData(byteCall);
                    return;
                case ActionTicket.ACTION_CALL:
                    command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND;
                    break;
                case ActionTicket.ACTION_RECALL:
                    command = (int)CheckByteSend.BYTE_COMMAND.RECALL_COMMAND;
                    break;
                case ActionTicket.ACTION_CANCEL:
                    command = (int)CheckByteSend.BYTE_COMMAND.DELETE_COMMAND;
                    break;
                case ActionTicket.ACTION_CONNECT_COUNTER: command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND; break;
                case ActionTicket.ACTION_CREATE: break;
                case ActionTicket.ACTION_FEEDBACK: break;
                case ActionTicket.ACTION_FINISH: command = (int)CheckByteSend.BYTE_COMMAND.FINISH_COMMAND; break;
                case ActionTicket.ACTION_MOVE_COUNTER:
                    command = (int)CheckByteSend.BYTE_COMMAND.FORWARD_COMMAND_COUNTER;
                    break;
                case ActionTicket.ACTION_MOVE_SERVICE:
                    command = (int)CheckByteSend.BYTE_COMMAND.FORWARD_COMMAND_SERVICE;
                    break;
                case ActionTicket.ACTION_RATING: break;

                case ActionTicket.ACTION_RESET_COUNTER: command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND; break;
                case ActionTicket.ACTION_RESTORE:
                    command = (int)CheckByteSend.BYTE_COMMAND.CALLSTORE_COMMAND;
                    break;

                case ActionTicket.MESSAGE_ERROR:
                    serialPortError(eventData.CNum, eventData.Address); return;
                case ActionTicket.CALL_PRIORITY: break;

                case ActionTicket.CONNECT:
                    serialPortError(MessageError.ERROR_06, eventData.Address);
                    return;
                case ActionTicket.DISCONNECT:
                    serialPortError(MessageError.ERROR_07, eventData.Address);
                    return;
                case ActionTicket.ERROR_SERVER: break;
                case ActionTicket.OPEN_SERVER: break;
                case ActionTicket.RATING_ONCE: break;
                //case ActionTicket.RELOAD: break;
                default: break;
            }
            var byteRst = modBus.BuildText(DEVICE_KEYBOARD, eventData.Address, command, eventData.CNum, eventData.IndexService);
            serialPort.SendData(byteRst);
        }

    }
}
