using System;
using System.IO.Ports;
using System.Threading;
using Modbus.Device; // 假设使用 NModbus 库

class Program
{
    static void Main()
    {
        // 确保程序结束时串口安全关闭
        using (SerialPort serialPort = new SerialPort("COM1"))
        {
            // 配置通信参数
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Parity = Parity.None;

            try
            {
                serialPort.Open();
                var master = ModbusSerialMaster.CreateRtu(serialPort);
                Console.WriteLine("✅ 串口连接成功！\n");

                // ==========================================
                // 【阶段一：让用户自己输入修改的值（寄存器）】
                // ==========================================
                ushort[] newValues = new ushort[3];
                bool isInputValid = false;

                while (!isInputValid)
                {
                    Console.WriteLine("👉 请输入要写入 10、11、12 号货架的 3 个数字，用逗号或空格隔开（例如：150 250 350）：");
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
                            Console.WriteLine("❌ 输入包含了非数字内容，或者数字太大了，请重新输入！\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ 必须输入刚好 3 个数字！你输入了 {strParts.Length} 个。\n");
                    }
                }

                // ==========================================
                // 【阶段二：把用户输入的数字发送给硬件（寄存器）】
                // ==========================================
                try
                {
                    Console.WriteLine("\n正在执行批量写入寄存器...");
                    master.WriteMultipleRegisters(1, 10, newValues);
                    Console.WriteLine($"👉 写入成功：10~12号地址已改为 {newValues[0]}, {newValues[1]}, {newValues[2]}！");
                }
                catch (Exception writeEx)
                {
                    Console.WriteLine("❌ 写数据时发生错误：" + writeEx.Message);
                }

                // ==========================================
                // 【阶段三：让用户输入并修改线圈状态】
                // ==========================================
                bool[] newCoilValues = new bool[2];
                bool isCoilInputValid = false;

                while (!isCoilInputValid)
                {
                    Console.WriteLine("\n👉 请输入要写入 0号 和 1号 线圈的状态（1代表开，0代表关，用空格隔开，例如：1 0）：");
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
                            Console.WriteLine("❌ 只能输入数字 1 或 0 呀！请重新输入。");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ 必须输入刚好 2 个状态！你输入了 {coilParts.Length} 个。");
                    }
                }

                try
                {
                    Console.WriteLine("\n正在执行批量写入线圈...");
                    master.WriteMultipleCoils(1, 0, newCoilValues);
                    string state1 = newCoilValues[0] ? "开(True)" : "关(False)";
                    string state2 = newCoilValues[1] ? "开(True)" : "关(False)";
                    Console.WriteLine($"👉 线圈写入成功：0号变为 {state1}，1号变为 {state2}！");
                }
                catch (Exception writeCoilEx)
                {
                    Console.WriteLine("❌ 写线圈数据时发生错误：" + writeCoilEx.Message);
                }

                // ==========================================
                // 【阶段四：进入连续“读”的监控模式】
                // ==========================================
                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine("开始进入连续监控模式，验证写入结果...");
                Console.WriteLine("🌟 请按键盘上的【任意键】安全退出程序！");
                Console.WriteLine("----------------------------------------\n");

                while (!Console.KeyAvailable)
                {
                    try
                    {
                        // 读取寄存器
                        ushort[] values = master.ReadHoldingRegisters(1, 10, 3);
                        Console.WriteLine($"[实时寄存器] - 10号: {values[0]}, 11号: {values[1]}, 12号: {values[2]}");

                        // 读取线圈
                        bool[] coilValues = master.ReadCoils(1, 0, 2);
                        Console.WriteLine($"[实时线圈]   - 0号: {coilValues[0]}, 1号: {coilValues[1]}");

                        Console.WriteLine(""); // 打印一个空行，让界面看起来不那么拥挤
                    }
                    catch (Exception readEx)
                    {
                        Console.WriteLine("⚠️ 读取出错: " + readEx.Message);
                    }

                    Thread.Sleep(1000); // 休息1秒
                }

                // 安全退出
                serialPort.Close();
                Console.WriteLine("\n👋 串口已安全关闭！按回车键结束程序。");

                Console.ReadLine(); // 吸收掉按下的任意键
                Console.ReadLine(); // 等待回车退出

            }
            catch (Exception openEx)
            {
                Console.WriteLine("❌ 串口打开失败: " + openEx.Message);
                Console.ReadLine();
            }
        }
    }
}