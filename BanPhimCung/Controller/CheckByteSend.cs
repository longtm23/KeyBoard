using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanPhimCung.DTO;
using BanPhimCung.Ultility;

namespace BanPhimCung.Controller
{
    public class CheckByteSend
    {
        public enum BYTE_COMMAND : int
        {
            START_COMMAND = (int)0x3A,
            STATUS_COMMAND = 2 * (int)0x3A,
            CONNECT_COMMAND = (int)0xF1,
            START_OF_COUNTER_COMMAND = (int)0x30,
            NEXT_COMMAND = START_OF_COUNTER_COMMAND + (int)0x00,
            RECALL_COMMAND = START_OF_COUNTER_COMMAND + (int)0x01,
            DELETE_COMMAND = START_OF_COUNTER_COMMAND + (int)0x02,
            FINISH_COMMAND = START_OF_COUNTER_COMMAND + (int)0x03,
            FORWARD_COMMAND_SERVICE = START_OF_COUNTER_COMMAND + (int)0x04,
            FORWARD_COMMAND_COUNTER = START_OF_COUNTER_COMMAND + (int)0x07,
            CALLSTORE_COMMAND = START_OF_COUNTER_COMMAND + (int)0x05,
            RESTORE_COMMAND = START_OF_COUNTER_COMMAND + (int)0x06,
            RESET_COUNTER = (int)0x0A + START_OF_COUNTER_COMMAND,
            DISPLAY_COMMAND = (int)0x02,
            ERROR_COMMAND = (int)0x39,
            END_COMMAND = (int)0x1310,
            CALL_LIST_WATTING = (int)0x3B,
            CALL_PRIORITY_COMMAND = (int)0x3C,
            OK_COMMAND = 0
        }
        public enum DEVICE_ID : int
        {
            DEVICE_LED = 1,
            DEVICE_KEYBOARD = 2,
            DEVICE_FEED_BACK = 3
        }

        public enum COMMAND_FEED_BACK : int
        {
            COMMAND_GOOD = 2,
            COMMAND_RATHER = 4,
            COMMAND_MEDIUM = 1,
            COMMAND_WEAK = 8
        }

        public enum COMMAND_LED : int
        {
            START_OF_LED_COMMAND = (int)0x00,
            NULL_COMMAND = START_OF_LED_COMMAND + (int)0x00,
            STATUS_COMMAND = START_OF_LED_COMMAND + (int)0x01,
            DISPLAY_COMMAND = START_OF_LED_COMMAND + (int)0x02,
            TURN_OFF_COMMAND = START_OF_LED_COMMAND + (int)0x03,
            TURN_ON_COMMAND = START_OF_LED_COMMAND + (int)0x04,
            FILLOUT_LED_COMMAND = START_OF_LED_COMMAND + (int)0x05,
            SHOW_ADDR_COMMAND = START_OF_LED_COMMAND + (int)0x06,
            SLIDE_DATA_COMMAND = START_OF_LED_COMMAND + (int)0x07,
            STOP_COMMAND = START_OF_LED_COMMAND + (int)0x08
        }
        public static List<ResponeByte> getByteSend(byte[] resByte)
        {
            List<ResponeByte> lstRes = new List<ResponeByte>();
            ResponeByte resByteClass = null;
            var count = resByte.Length;
            if (count > 10)
            {
                resByteClass = new ResponeByte();
                var dataLength = resByte[5] + resByte[6] * 256;
                var end = resByte[count - 2] + resByte[count - 1] * 256;
                int startNum = (int)BYTE_COMMAND.START_COMMAND;
                int endNum = (int)BYTE_COMMAND.END_COMMAND;
                if (startNum == resByte[0] && (count - 10) - 1 == dataLength && endNum == end)
                {
                    resByteClass.DeviceId = (int)resByte[1];
                    resByteClass.AddressKey = (int)resByte[2];
                    resByteClass.Command = (int)(resByte[3] + resByte[4] * 256);
                    resByteClass.NumServicesOrCounter = (int)(resByte[11] + resByte[12]);
                    lstRes.Add(resByteClass);
                }
            }
            return lstRes;
        }

        public static List<ResponeByte> getByteSendList(byte[] resByte, Dictionary<int, KeyBoardCounter> dicCounterKeyboard)
        {
            List<ResponeByte> lstRes = null;
            ResponeByte resByteClass = null;

            var count = resByte.Length;
            var countMath = 0;
            if (count > 10)
            {
                lstRes = new List<ResponeByte>();
                resByteClass = new ResponeByte();
                int startNum = (int)BYTE_COMMAND.START_COMMAND;
                int endNum = (int)BYTE_COMMAND.END_COMMAND;
                do
                {
                    ConvertByte<byte> arrayItemDevice = new ConvertByte<byte>(resByte, countMath, 15);
                    if (CheckFrame(arrayItemDevice, startNum, dicCounterKeyboard))
                    {
                        try
                        {
                            ConvertByte<byte> arrayItemSendSocket = new ConvertByte<byte>(resByte, countMath, 17);
                            resByteClass = new ResponeByte();
                            var dataLength = arrayItemDevice[5] + arrayItemSendSocket[6] * 256;
                            var end = 0;
                            var lengArr = dataLength + 11;
                            if (lengArr == 15)
                            {
                                countMath += 15;
                            }
                            else if (lengArr <= count)
                            {
                                arrayItemSendSocket = new ConvertByte<byte>(resByte, countMath, lengArr);
                                byte[] byteArr = arrayItemSendSocket.Cast<byte>().ToArray();
                                var data = new ConvertByte<byte>(byteArr, 11, dataLength - 4);
                                resByteClass.Data = data.Cast<byte>().ToArray();
                                var lengData = data.Length;
                                byte[] dataByte = null;
                                if (lengData % 4 == 0)
                                {
                                    dataByte = new byte[lengData / 4];
                                    var j = 0;
                                    for (int i = 1; i < lengData; i = j * 4)
                                    {
                                        if (i == 1)
                                            dataByte[j] = data[i - 1];
                                        else { dataByte[j] = data[i]; }
                                        j++;
                                    }
                                    resByteClass.Cnum = Encoding.UTF8.GetString(dataByte, 0, dataByte.Length);
                                }

                                end = arrayItemSendSocket[lengArr - 2] + arrayItemSendSocket[lengArr - 1] * 256;
                                if (endNum == end)
                                {
                                    resByteClass.DeviceId = (int)arrayItemSendSocket[1];
                                    resByteClass.AddressKey = (int)arrayItemSendSocket[2];
                                    resByteClass.Command = (int)(arrayItemSendSocket[3] + arrayItemSendSocket[4] * 256);
                                    resByteClass.NumServicesOrCounter = (int)(arrayItemSendSocket[11] + arrayItemSendSocket[12]);
                                    lstRes.Add(resByteClass);
                                }
                                countMath += lengArr;
                            }
                            else
                            {
                                countMath = ForSearchStart(countMath, resByte, count);
                            }
                        }
                        catch
                        {
                            countMath = ForSearchStart(countMath, resByte, count);
                        }
                    }
                    else
                    {
                        countMath = ForSearchStart(countMath, resByte, count);
                    }
                } while (countMath < count);
            }
            return lstRes;
        }

        private static int ForSearchStart(int countMath, byte[] resByte, int countRes)
        {
            for (countMath = countMath + 1; countMath < countRes; countMath++)
            {
                if (resByte[countMath] == (int)BYTE_COMMAND.START_COMMAND)
                {
                    return countMath;
                }
            }
            return countMath;
        }

        /* Check đúng khung truyền*/
        public static bool CheckFrame(ConvertByte<byte> byteRes, int startNum, Dictionary<int, KeyBoardCounter> dicCounterKeyboard)
        {
            bool isCheck = false;
            try
            {
                var deviceID = byteRes[1];
                var command = ((int)byteRes[3] + (int)byteRes[4] * 256);
                var address = byteRes[2];

                switch (deviceID)
                {
                    case (int)DEVICE_ID.DEVICE_KEYBOARD:
                        if ((startNum == byteRes[0] && dicCounterKeyboard.ContainsKey(address)) && (command == (int)BYTE_COMMAND.CALLSTORE_COMMAND
                                || command == (int)BYTE_COMMAND.DELETE_COMMAND
                                || command == (int)BYTE_COMMAND.DISPLAY_COMMAND
                                || command == (int)BYTE_COMMAND.END_COMMAND
                                || command == (int)BYTE_COMMAND.ERROR_COMMAND
                                || command == (int)BYTE_COMMAND.FINISH_COMMAND
                                || command == (int)BYTE_COMMAND.FORWARD_COMMAND_COUNTER
                                || command == (int)BYTE_COMMAND.FORWARD_COMMAND_SERVICE
                                || command == (int)BYTE_COMMAND.NEXT_COMMAND
                                || command == (int)BYTE_COMMAND.RECALL_COMMAND
                                || command == (int)BYTE_COMMAND.RESTORE_COMMAND
                                || command == (int)BYTE_COMMAND.START_COMMAND
                                || command == (int)BYTE_COMMAND.START_OF_COUNTER_COMMAND
                                || command == (int)BYTE_COMMAND.OK_COMMAND
                                || command == (int)BYTE_COMMAND.RESET_COUNTER
                                || command == (int)BYTE_COMMAND.CALL_LIST_WATTING
                                || command == (int)BYTE_COMMAND.CALL_PRIORITY_COMMAND
                                || command == (int)BYTE_COMMAND.STATUS_COMMAND
                                || command == (int)BYTE_COMMAND.CONNECT_COMMAND))
                        {
                            string msg = "";
                            switch (command)
                            {
                                case (int)BYTE_COMMAND.CALLSTORE_COMMAND: msg = "Call restore"; break;
                                case (int)BYTE_COMMAND.DELETE_COMMAND: msg = "DELETE"; break;
                                case (int)BYTE_COMMAND.DISPLAY_COMMAND: msg = "DISPLAY_COMMAND"; break;
                                case (int)BYTE_COMMAND.END_COMMAND: msg = "END_COMMAND"; break;
                                case (int)BYTE_COMMAND.ERROR_COMMAND: msg = "ERROR_COMMAND"; break;
                                case (int)BYTE_COMMAND.FINISH_COMMAND: msg = "FINISH_COMMAND"; break;
                                case (int)BYTE_COMMAND.FORWARD_COMMAND_COUNTER: msg = "FORWARD_COMMAND_COUNTER"; break;
                                case (int)BYTE_COMMAND.FORWARD_COMMAND_SERVICE: msg = "FORWARD_COMMAND_SERVICE"; break;
                                case (int)BYTE_COMMAND.NEXT_COMMAND: msg = "NEXT_COMMAND"; break;
                                case (int)BYTE_COMMAND.RECALL_COMMAND: msg = "RECALL_COMMAND"; break;
                                case (int)BYTE_COMMAND.RESTORE_COMMAND: msg = "RESTORE_COMMAND"; break;
                                case (int)BYTE_COMMAND.START_COMMAND: msg = "START_COMMAND"; break;
                                case (int)BYTE_COMMAND.OK_COMMAND: msg = "OK_COMMAND"; break;
                                case (int)BYTE_COMMAND.CALL_LIST_WATTING: msg = "CALL_LIST_WATTING"; break;
                                case (int)BYTE_COMMAND.CALL_PRIORITY_COMMAND: msg = "CALL_PRIORITY_COMMAND"; break;
                                case (int)BYTE_COMMAND.STATUS_COMMAND: msg = "STATUS_COMMAND"; break;
                                case (int)BYTE_COMMAND.CONNECT_COMMAND: msg = "CONNECT_COMMAND"; break;
                            }
                            Console.WriteLine(msg);
                            isCheck = true;
                        }
                        else
                        {
                            Console.WriteLine("Sai khung truyen: ");
                            foreach (var a in byteRes)
                            {
                                Console.WriteLine(a + ", ");
                            }
                        }
                        break;
                    case (int)DEVICE_ID.DEVICE_FEED_BACK:
                        if (startNum == byteRes[0] && dicCounterKeyboard.Values.FirstOrDefault(m => m.AddressFeedBack == address) != null)
                        {
                            isCheck = true;
                        }
                        break;
                }
            }
            catch
            {
                return isCheck;
            }
            return isCheck;
        }

        public static string[] GetActionByCommand(int command,string cNumPriority, int numServicesOrCounter, string counterOld, Dictionary<string, Service> dicService, Dictionary<string, Counter> dicCounter)
        {
            var arrActionAndIdCounterService = new string[4];
            string action = null;
            string counterID = null;
            string serviceID = null;
            switch (command)
            {
                case (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND:
                    action = ActionTicket.ACTION_CALL; break;
                case (int)CheckByteSend.BYTE_COMMAND.RECALL_COMMAND:
                    action = ActionTicket.ACTION_RECALL; break;
                case (int)CheckByteSend.BYTE_COMMAND.DELETE_COMMAND:
                    action = ActionTicket.ACTION_CANCEL; break;
                case (int)CheckByteSend.BYTE_COMMAND.FINISH_COMMAND:
                    action = ActionTicket.ACTION_FINISH; break;
                case (int)CheckByteSend.BYTE_COMMAND.FORWARD_COMMAND_SERVICE:
                    //var serviceId = dicService.ElementAt(numServicesOrCounter).Key;
                    //serviceID = CheckServiceOfCounter(serviceId, dicCounter, counterOld);
                    serviceID = dicService.ElementAt(numServicesOrCounter).Key;
                    if (!string.IsNullOrWhiteSpace(counterOld))
                    {
                        action = ActionTicket.ACTION_MOVE_SERVICE;
                    }
                    break;
                case (int)CheckByteSend.BYTE_COMMAND.FORWARD_COMMAND_COUNTER:
                    action = ActionTicket.ACTION_MOVE_COUNTER;
                    counterID = dicCounter.ElementAt(numServicesOrCounter).Key;
                    //case chuyển counter phải sang socket kiểm tra
                    break;
                case (int)CheckByteSend.BYTE_COMMAND.CALLSTORE_COMMAND:
                    action = ActionTicket.ACTION_RESTORE;
                    break;
                case (int)CheckByteSend.BYTE_COMMAND.RESTORE_COMMAND:
                    action = ActionTicket.ACTION_ALL_RESTORE;
                    break;
                case (int)BYTE_COMMAND.RESET_COUNTER:
                    action = ActionTicket.ACTION_RESET_COUNTER;
                    break;
                case (int)BYTE_COMMAND.CALL_LIST_WATTING:
                    action = ActionTicket.CALL_LIST_WATTING;
                    break;
                case (int)BYTE_COMMAND.CALL_PRIORITY_COMMAND:
                    action = ActionTicket.CALL_PRIORITY;
                    break;
                case (int)BYTE_COMMAND.CONNECT_COMMAND:
                    action = ActionTicket.ACTION_CONNECT_COUNTER;
                    break;
                default:
                    break;
            }
            arrActionAndIdCounterService[0] = action;
            arrActionAndIdCounterService[1] = counterID;
            arrActionAndIdCounterService[2] = serviceID;
            arrActionAndIdCounterService[3] = cNumPriority;
            return arrActionAndIdCounterService;
        }

        public static int GetRatingByCommand(int command)
        {
            var rating = 0;
            switch (command)
            {
                case (int)COMMAND_FEED_BACK.COMMAND_GOOD: rating = 10; break;
                case (int)COMMAND_FEED_BACK.COMMAND_RATHER: rating = 8; break;
                case (int)COMMAND_FEED_BACK.COMMAND_MEDIUM: rating = 6; break;
                case (int)COMMAND_FEED_BACK.COMMAND_WEAK: rating = 4; break;
            }
            return rating;
        }

        private static string CheckServiceOfCounter(string serviceID, Dictionary<string, Counter> dicCounter, string counterOld)
        {
            return dicCounter[counterOld].Services.FirstOrDefault(m => m == serviceID);
        }

    }
}
