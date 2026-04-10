// 引入系统基础功能，比如读写控制台屏幕（那个黑窗口）的内容
using System;
// 引入操作硬件串口的功能，相当于拿到了控制“物理电话线”的权限
using System.IO.Ports;
// 引入多线程功能，主要是为了后面能用 Thread.Sleep() 让程序“睡”一会儿（延时喘口气）
using System.Threading;
// 引入别人写好的 Modbus 翻译工具包，让咱们的代码能听懂工业“行业黑话”
using Modbus.Device;

// 定义一个叫做 Program 的类，这是咱们软件的“外壳图纸”
class Program
{
    // Main() 是整个软件的“正大门”，程序一启动，就从这里走进去开始一步步干活
    static void Main()
    {
        // 雇一个“保洁阿姨”（using），让她盯着名叫 "COM1" 的电话亭。
        // 只要大括号里的事情办完，阿姨会自动帮你挂断电话并清理干净，绝不占线。
        using (SerialPort serialPort = new SerialPort("COM1"))
        {
            // 配置电话的语速（波特率），9600 是工业界最常用的默认速度，双方必须对齐
            serialPort.BaudRate = 9600;
            // 配置每次传递信息的数据包长度为 8 位
            serialPort.DataBits = 8;
            // 配置停止位为 1，相当于每说完一句话加一个句号，表示说完啦
            serialPort.StopBits = StopBits.One;
            // 配置校验位为“无”，意思是不去刻意检查对方有没有口误
            serialPort.Parity = Parity.None;

            // 给下面这部分代码穿上一层“防弹衣”（try），万一报错了，软件不会直接闪退崩溃
            try
            {
                // 正式拨通电话！嘟...嘟...接通了
                serialPort.Open();
                // 告诉程序：我们要找一个翻译官（master），用 Modbus RTU 这种“行话”通过刚才拨通的电话进行交流
                var master = ModbusSerialMaster.CreateRtu(serialPort);
                // 在黑窗口上打印一句话，提示我们跟硬件连接成功了
                Console.WriteLine("串口连接成功！\n");

                // ==========================================
                // 【阶段一：让用户自己输入修改的值（寄存器）】
                // ==========================================

                // 准备一个空的小本本（数组），总共有 3 页，用来装一会儿用户输入的 3 个数字
                ushort[] newValues = new ushort[3];
                // 准备一个标签，记录用户是不是胡乱敲了键盘。默认是 false（表示还没输对）
                bool isInputValid = false;

                // 开启一个死循环：只要用户没输对（isInputValid 是 false），就一直让他重新输，休想往下走
                while (!isInputValid)
                {
                    // 屏幕上显示提示语，告诉用户现在该干嘛
                    Console.WriteLine(" 请输入要写入 10、11、12 号寄存器的 3 个数字，用逗号或空格隔开（例如：150 250 350）：");

                    // 让屏幕卡住，光标闪烁，等待用户敲键盘。按下回车后，把用户敲的一长串字存进 userInput 里
                    string userInput = Console.ReadLine();

                    // 【切西瓜法则】：“把 userInput 里的文字给我切开！只要看到英文逗号、空格、或者中文逗号，就给我切一刀。
                    // 切完之后，把那些因为连敲空格而产生出来的空壳垃圾全都给我扔掉（RemoveEmptyEntries），
                    // 最后把剩下的纯肉（纯数据）整整齐齐地摆在 strParts 这个盘子（数组）里交给我！”
                    string[] strParts = userInput.Split(new char[] { ',', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);

                    // 检查一下，是不是刚好切出来 3 块（即用户确实输入了 3 个独立的东西）
                    if (strParts.Length == 3)
                    {
                        // 再次穿上“防弹衣”，因为用户虽然输入了 3 个东西，但可能是 "A, B, C"，根本不是数字
                        try
                        {
                            // 强行把第 1 块文字翻译成无符号整数（数字），写到小本本的第 1 页（位置 0）
                            newValues[0] = ushort.Parse(strParts[0]);
                            // 强行把第 2 块文字翻译成数字，写到小本本的第 2 页（位置 1）
                            newValues[1] = ushort.Parse(strParts[1]);
                            // 强行把第 3 块文字翻译成数字，写到小本本的第 3 页（位置 2）
                            newValues[2] = ushort.Parse(strParts[2]);

                            // 运行到这里说明三个数字都完美翻译成功了！把标签改为 true，准备跳出这个死循环！
                            isInputValid = true;
                        }
                        // 如果刚才强行翻译时遇到了乱七八糟的字母，就会掉进这里
                        catch
                        {
                            // 温柔地提示用户别乱敲键盘
                            Console.WriteLine("输入包含了非数字内容，或者数字太大了，请重新输入！\n");
                        }
                    }
                    // 如果切出来的不是 3 块（数量不对）
                    else
                    {
                        // 运用 $ 符号的魔法（魔法填空），把用户实际输入的数量填入句子里打印出来
                        Console.WriteLine($"必须输入刚好 3 个数字！你输入了 {strParts.Length} 个。\n");
                    }
                }

                // ==========================================
                // 【阶段二：把用户输入的数字发送给硬件（寄存器）】
                // ==========================================

                // 给发送过程穿上“防弹衣”，防止发送时网线突然断开
                try
                {
                    // 打印提示，告诉用户老板要下令了
                    Console.WriteLine("\n正在执行批量写入寄存器...");
                    // 🌟核心写命令：呼叫 1 号管理员，从 10 号货架（地址）开始，把刚才小本本(newValues)里记的 3 个数字按顺序塞进去！
                    master.WriteMultipleRegisters(1, 10, newValues);
                    // 运用 $ 魔法填空，汇报刚才成功写进去了哪三个值
                    Console.WriteLine($"写入成功：10~12号地址已改为 {newValues[0]}, {newValues[1]}, {newValues[2]}！");
                }
                // 如果下达命令失败（比如没连上线），掉进这里
                catch (Exception writeEx)
                {
                    // 打印失败的具体原因（writeEx.Message 里存着报错详情）
                    Console.WriteLine(" 写数据时发生错误：" + writeEx.Message);
                }

                // ==========================================
                // 【阶段三：让用户输入并修改线圈状态】
                // ==========================================

                // 准备第二个小本本，这次是只能装 true（开）或 false（关）的布尔类型数组，准备装 2 个状态
                bool[] newCoilValues = new bool[2];
                // 准备第二个标签，用来监控线圈输入对不对
                bool isCoilInputValid = false;

                // 再次开启死循环，逼着用户输入正确的线圈状态
                while (!isCoilInputValid)
                {
                    // 提示用户输入 0 和 1 号开关的状态
                    Console.WriteLine("\n👉 请输入要写入 0号 和 1号 线圈的状态（1代表开，0代表关，用空格隔开，例如：1 0）：");
                    // 接收用户敲下的开关字符
                    string coilInput = Console.ReadLine();
                    // 再次使用“切西瓜法则”，按照逗号或空格把字符切成独立的块
                    string[] coilParts = coilInput.Split(new char[] { ',', ' ', '，' }, StringSplitOptions.RemoveEmptyEntries);

                    // 检查是否刚好切出了 2 块
                    if (coilParts.Length == 2)
                    {
                        // 严格检查：第一块必须是 "1" 或 "0"，并且 第二块也必须是 "1" 或 "0"
                        if ((coilParts[0] == "1" || coilParts[0] == "0") && (coilParts[1] == "1" || coilParts[1] == "0"))
                        {
                            // 翻译判断：如果输入的是 "1"，就在本子上记下 true，否则记下 false
                            newCoilValues[0] = coilParts[0] == "1";
                            // 翻译第 2 个状态
                            newCoilValues[1] = coilParts[1] == "1";
                            // 输入完美！放行！
                            isCoilInputValid = true;
                        }
                        // 如果输入的虽然是两块，但敲了 "2" 或者 "A"
                        else
                        {
                            // 拦截并提示
                            Console.WriteLine("❌ 只能输入数字 1 或 0 呀！请重新输入。");
                        }
                    }
                    // 如果数量不对
                    else
                    {
                        // 运用 $ 魔法填空报错
                        Console.WriteLine($"❌ 必须输入刚好 2 个状态！你输入了 {coilParts.Length} 个。");
                    }
                }

                // 穿上防弹衣准备下达线圈命令
                try
                {
                    // 提示用户准备推电闸了
                    Console.WriteLine("\n正在执行批量写入线圈...");
                    // 🌟核心控制命令：呼叫 1 号设备，从 0 号线圈开始，按照 newCoilValues 里的状态咔咔推电闸！
                    master.WriteMultipleCoils(1, 0, newCoilValues);

                    // 高级三元运算符（压缩版if-else）：如果是 true 就取 "开(True)"，否则取 "关(False)"
                    string state1 = newCoilValues[0] ? "开(True)" : "关(False)";
                    string state2 = newCoilValues[1] ? "开(True)" : "关(False)";
                    // 运用 $ 魔法汇报电闸推完的结果
                    Console.WriteLine($"👉 线圈写入成功：0号变为 {state1}，1号变为 {state2}！");
                }
                // 控制失败的话掉进这里
                catch (Exception writeCoilEx)
                {
                    // 打印控制线圈失败的原因
                    Console.WriteLine("❌ 写线圈数据时发生错误：" + writeCoilEx.Message);
                }

                // ==========================================
                // 【阶段四：进入连续“读”的监控模式】
                // ==========================================

                // 打印华丽的分割线，提示用户马上要进入循环播报模式了
                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine("开始进入连续监控模式，验证写入结果...");
                // 告诉用户防呆退出的秘籍
                Console.WriteLine("🌟 请按键盘上的【任意键】安全退出程序！");
                Console.WriteLine("----------------------------------------\n");

                // 死循环：只要用户没碰键盘（!Console.KeyAvailable），老板就一直在下面无限循环要数据
                while (!Console.KeyAvailable)
                {
                    // 穿防弹衣读数据，防止读着读着线掉了
                    try
                    {
                        // 🌟老板提问 1：呼叫 1 号管理员，从 10 号货架起查 3 个寄存器数据
                        ushort[] values = master.ReadHoldingRegisters(1, 10, 3);
                        // 把拿到的 3 个数据打印到屏幕上
                        Console.WriteLine($"[实时寄存器] - 10号: {values[0]}, 11号: {values[1]}, 12号: {values[2]}");

                        // 🌟老板提问 2：呼叫 1 号管理员，从 0 号线圈起查 2 个继电器的通断状态
                        bool[] coilValues = master.ReadCoils(1, 0, 2);
                        // 把查到的开/关状态打印到屏幕上
                        Console.WriteLine($"[实时线圈]   - 0号: {coilValues[0]}, 1号: {coilValues[1]}");

                        // 打印一个空行，让这一秒和下一秒的数据隔开，看起来不至于头晕
                        Console.WriteLine("");
                    }
                    // 万一这一秒读取失败了（没信号了）
                    catch (Exception readEx)
                    {
                        // 稍微抱怨一下出错原因，但程序不会死，下一秒还会接着试
                        Console.WriteLine("⚠️ 读取出错: " + readEx.Message);
                    }

                    // 🌟极其重要：让程序睡大觉 1000 毫秒（1秒）。不加这句硬件会被问到冒烟死机！
                    Thread.Sleep(1000);
                }

                // 离开上面的死循环，说明用户按了键盘，准备安全下班了！

                // 主动挂断电话，把串口关闭
                serialPort.Close();
                // 打印挥手再见
                Console.WriteLine("\n👋 串口已安全关闭！按回车键结束程序。");

                // 这个 ReadLine 是为了“吃掉”刚刚你按下的那个触发退出的键，免得变成乱码印在屏幕上
                Console.ReadLine();
                // 彻底卡住屏幕，等你按下真正的回车键后，黑窗口才会彻底关闭
                Console.ReadLine();

            }
            // 如果程序一开始连第一步拨通电话（serialPort.Open）都没成功（没开模拟器或串口被占）
            catch (Exception openEx)
            {
                // 遗憾地告诉你电话没打通的原因
                Console.WriteLine("❌ 串口打开失败: " + openEx.Message);
                // 卡住屏幕让你看清死因，不然窗口一闪就没了
                Console.ReadLine();
            }
        } // 离开这里，保洁阿姨（using）最后扫视一圈，确认电话彻底清干净了，下班！
    }
}