using System;
using System.IO.Ports;
using System.Threading;
using Modbus.Device;

class Program
{
    static void Main()
    {
        using (SerialPort serialPort = new SerialPort("COM1"))
        {
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Parity = Parity.None;

            try
            {
                serialPort.Open();
                var master = ModbusSerialMaster.CreateRtu(serialPort);
                Console.WriteLine("串口连接成功！\n");

                ushort[] newValues = new ushort[3];
                bool isInputValid = false;

                while (!isInputValid)
                {
                    Console.WriteLine("请输入要写入 10、11、12 号寄存器的 3 个数字，用逗号或空格隔开：");
                    string userInput = Console.ReadLine();
                    string[] strParts = userInput.Split(new char[] { ',', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);

                    if (strParts.Length == 3)
                    {
                        try
                        {
                            newValues[0] = ushort.Parse(strParts[0]);
                            newValues[1] = ushort.Parse(strParts[1]);
                            newValues[2] = ushort.Parse(strParts[2]);
                            isInputValid = true;
                        }
                        catch
                        {
                            Console.WriteLine("输入包含非数字或超出范围，请重新输入！\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"必须输入 3 个数字！你输入了 {strParts.Length} 个。\n");
                    }
                }

                try
                {
                    Console.WriteLine("\n正在执行批量写入寄存器...");
                    master.WriteMultipleRegisters(1, 10, newValues);
                    Console.WriteLine($"写入成功：10~12号地址已改为 {newValues[0]}, {newValues[1]}, {newValues[2]}！");
                }
                catch (Exception writeEx)
                {
                    Console.WriteLine("写数据时发生错误：" + writeEx.Message);
                }

                bool[] newCoilValues = new bool[2];
                bool isCoilInputValid = false;

                while (!isCoilInputValid)
                {
                    Console.WriteLine("\n请输入 0号、1号线圈状态（1=开，0=关，用空格隔开）：");
                    string coilInput = Console.ReadLine();
                    string[] coilParts = coilInput.Split(new char[] { ',', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);

                    if (coilParts.Length == 2)
                    {
                        if ((coilParts[0] == "1" || coilParts[0] == "0") && (coilParts[1] == "1" || coilParts[1] == "0"))
                        {
                            newCoilValues[0] = coilParts[0] == "1";
                            newCoilValues[1] = coilParts[1] == "1";
                            isCoilInputValid = true;
                        }
                        else
                        {
                            Console.WriteLine("只能输入 1 或 0，请重新输入！");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"必须输入 2 个状态！你输入了 {coilParts.Length} 个。");
                    }
                }

                try
                {
                    Console.WriteLine("\n正在执行批量写入线圈...");
                    master.WriteMultipleCoils(1, 0, newCoilValues);

                    string state1 = newCoilValues[0] ? "开(True)" : "关(False)";
                    string state2 = newCoilValues[1] ? "开(True)" : "关(False)";
                    Console.WriteLine($"线圈写入成功：0号={state1}，1号={state2}！");
                }
                catch (Exception writeCoilEx)
                {
                    Console.WriteLine("写线圈数据时发生错误：" + writeCoilEx.Message);
                }

                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine("开始连续监控...");
                Console.WriteLine("按任意键安全退出！");
                Console.WriteLine("----------------------------------------\n");

                while (!Console.KeyAvailable)
                {
                    try
                    {
                        ushort[] values = master.ReadHoldingRegisters(1, 10, 3);
                        Console.WriteLine($"[寄存器] 10:{values[0]}, 11:{values[1]}, 12:{values[2]}");

                        bool[] coilValues = master.ReadCoils(1, 0, 2);
                        Console.WriteLine($"[线圈]     0:{coilValues[0]}, 1:{coilValues[1]}");
                        Console.WriteLine("");
                    }
                    catch (Exception readEx)
                    {
                        Console.WriteLine("读取错误: " + readEx.Message);
                    }

                    Thread.Sleep(1000);
                }

                serialPort.Close();
                Console.WriteLine("\n串口已安全关闭！按回车退出。");
                Console.ReadLine();
                Console.ReadLine();
            }
            catch (Exception openEx)
            {
                Console.WriteLine("串口打开失败: " + openEx.Message);
                Console.ReadLine();
            }
        }
    }
}