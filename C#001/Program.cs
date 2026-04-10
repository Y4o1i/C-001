//类 = 模板、图纸、种类
//对象 = 用模板造出来的真实东西
//方法 = 带小括号 () 的动作、功能



//using System; = 基础打字弹窗
//using System.IO.Ports; = 连硬件串口
//using System.Threading; = 让程序休息
//using Modbus.Device; = 设备语言翻译官
using System;
using System.IO.Ports;
using System.Threading;
using Modbus.Device;

//创建一个类名为program（也不一定叫program），所有代码都必须在类里
class Program
{
    //静态（不用创建任何东西即可使用） 无返回值  程序入口
    static void Main()
    {
   //自动关串口语法 串口类 串口对象    创建 串口类
        using (SerialPort serialPort = new SerialPort("COM1"))
        {
          //串口对象 的 波特率
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Parity = Parity.None;

            //尝试运行代码，报错不闪退而是前往catch
            try
            {
                serialPort.Open();

                //读、写、通信这种 “动作”，必须绑定一条具体线路！
                //必须用对象！类做不到！
                //这个类通过RTU连接串口，创建了一个叫 master 的对象，后面我就可以用这个master使用这个类的所有方法了
                var master = ModbusSerialMaster.CreateRtu(serialPort);
            //  控制台类（自带static）  打印动作（方法）
                Console.WriteLine("串口连接成功！\n");

                //创建一个能存 3 个数字的小格子数组，名字叫 newValues。
                ushort[] newValues = new ushort[3];

                //造一个 “对 / 错” 开关，名字叫 isInputValid，一开始默认是 false（不对、无效）。
                //让用户一直输入，直到输对为止
                bool isInputValid = false;

                //当开关是假的，就一直让用户输入
                while (!isInputValid)
                {
                    Console.WriteLine("请输入要写入 10、11、12 号寄存器的 3 个数字，用逗号或空格隔开：");
                    string userInput = Console.ReadLine();
                    //把用户输入的字符串，按照逗号、空格、中文逗号分成几个小字符串，放到 strParts 数组里,并且去掉空的部分即多打了的逗号或空格
                    string[] strParts = userInput.Split(new char[] { ',', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);

                    if (strParts.Length == 3)
                    {
                        try
                        {
                            //把分好的字符串，转换成数字，放到 newValues 这个数字数组里
                            newValues[0] = ushort.Parse(strParts[0]);
                            newValues[1] = ushort.Parse(strParts[1]);
                            newValues[2] = ushort.Parse(strParts[2]);
                            isInputValid = true;
                        }
                        //不需要（）和ex是因为我们不关心具体是什么错误，只要知道错了就行了
                        catch
                        {
                            Console.WriteLine("输入包含非数字或超出范围，请重新输入！\n");
                        }
                    }
                    else
                    {
                        //$这个符号让我们可以在字符串里直接放变量，{strParts.Length} 就会被替换成实际的数字
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

                //开始写入线圈状态
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
                            //结果 = (内容 是不是 1？)
                            //newCoilValues[] = 结果
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
                    //writecoilEx.Message 这个属性会告诉我们具体的错误信息，比如“串口未打开”或者“设备无响应”等等
                    //是 底层驱动 / Modbus 库 知道错哪
                    //它把错误写进.Message
                    //你只是 拿过来显示给用户看
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
                        //我创建了一个名为 master 的对象，它属于 ModbusSerialMaster 这个类，所以才能用这个类里的方法。
                        ushort[] values = master.ReadHoldingRegisters(1, 10, 3);
                        Console.WriteLine($"[寄存器] 10:{values[0]}, 11:{values[1]}, 12:{values[2]}");

                        bool[] coilValues = master.ReadCoils(1, 0, 2);
                        Console.WriteLine($"[线圈]状态     {coilValues[0]}, {coilValues[1]}");
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
//这段程序以工业测控场景的稳定通信需求为核心，采用分层结构化设计：
//先配置并建立物理串口连接，再通过 Modbus 类的静态方法创建绑定了串口的通信对象（master）作为与 PLC 交互的唯一载体，
//接着通过循环校验对用户输入做规范化处理，
//避免非法数据导致程序异常，随后使用通信对象安全执行寄存器与线圈的读写操作，
//全程通过 try-catch 捕获硬件、通信及输入错误以保证程序不崩溃，
//最后进入循环实时监控数据，
//整体遵循先建连接→再造通信对象→再校验数据→再执行指令→最后稳定运行的工业软件逻辑，
//既满足 Modbus 协议必须绑定具体连接的特性，也兼顾了现场使用的可靠性与容错性。