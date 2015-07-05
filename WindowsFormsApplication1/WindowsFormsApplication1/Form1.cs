using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

//还需加入某些text框的键盘回车功能

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        //serial通用变量
        private SerialPort comm = new SerialPort();
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。  
        private bool Listening = false;//是否没有执行完invoke相关操作
        private bool Closing_new = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke
        private long received_count = 0;//接收计数  
        private long send_count = 0;//发送计数 
        private int mode = 0;

        //serial接收数据相关
        byte[] print_data = new byte[257];
        int[] tmp = new int[50];
        int drawDataCn = 0;

        //画图需要的变量
        public bool progRunFlag = false;
        int zGraph_numLimits = 500;
        private int zGraph_timerDrawI = 0;
        int zGraph_current;
        int drawState = 0;
        public Color[] color = { Color.Red, Color.Blue, Color.FromArgb(0, 128, 192), Color.Yellow, Color.Cyan, Color.Green, Color.MediumVioletRed, Color.Orange };
        public List<float>[] zGraph_x = new List<float>[8];//zGraph的x轴
        public List<float>[] zGraph0_y = new List<float>[8];//zGraph0的y轴
        public List<float>[] zGraph1_y = new List<float>[8];//zGraph1的y轴
        public List<float>[] zGraph2_y = new List<float>[8];//zGraph2的y轴

        //ccd数据
        Bitmap bmap0 = new Bitmap(806, 295);
        Bitmap bmap1 = new Bitmap(806, 295);
        PointF[] cpt_Large = new PointF[4] { new PointF(0, 0), new PointF(0, 294), new PointF(805, 0), new PointF(805, 294) };//外面的框
        PointF[] cpt_Small = new PointF[4] { new PointF(20, 20), new PointF(20, 275), new PointF(785, 20), new PointF(785, 275) };//里面的框
        byte[] ccd_data0 = new byte[128],ccd_data1 = new byte[128];
        int ccd0_count = 0,ccd1_count = 0;
        int ccdRecState = 0, ccdNum=0,ccdDataCn=0;
        int[] ccd_X = new int[128];
        Graphics gph0,gph1;
        Pen pen1 = new Pen(Color.Black);
        string[] lines自定义发送_string = new string[15];

        //摄像头数据
        Bitmap bmap_ov7620 = new Bitmap(700, 300);
        Graphics gph_ov7620;
        int count_x, receive_X,count_y, receive_Y;
        int camera_receive_count = 0;
        int cameraState = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)//窗口初始化，各种变量
        {
            //使用双缓冲
            this.DoubleBuffered = true;
            //最大化
            this.WindowState = FormWindowState.Maximized;
            //波特率初始化
            cbBaudRateList.SelectedItem = cbBaudRateList.Items[6];
            //说明
            comboBox2.SelectedItem = comboBox2.Items[0];
            comboBox3.SelectedItem = comboBox3.Items[0];
            string[] lines = File.ReadAllLines("readme/read00.c");
            richTextBox使用说明.Text = "";
            for (int i = 0; i < lines.Length; i++)
                richTextBox使用说明.Text += lines[i] + "\n";
            //显示串口
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            cbComList.Items.AddRange(ports);
            cbComList.SelectedIndex = cbComList.Items.Count > 0 ? 0 : -1;
            //初始化SerialPort对象  
            comm.NewLine = "/r/n";
            comm.RtsEnable = false;//reset功能
            cbComList.Enabled = true;
            cbBaudRateList.Enabled = true;
            comm.DataReceived += comm_DataReceived;//添加事件注册
            //蓝牙调试初始化
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            textBox4.Enabled = false;
            //ccd link：www.cnblogs.com/nine425/archive/2007/06/28/799473.html
            pen1.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            for (byte i = 0; i < 128; i++) ccd_X[i] = i * 6;
            //ccd0数据初始化赋值
            for (byte i = 0; i < 128; i++) { ccd_data0[i] = ccd_data1[i] = i; }
            ccd_data0[64] = ccd_data1[64] = 0;
            //ccd画图
            gph0 = Graphics.FromImage(bmap0);
            gph1 = Graphics.FromImage(bmap1);
            ccd0_print();
            ccd1_print();
            //ccd0数据存储
            StreamWriter sw = File.AppendText("ccddata/data0.txt");
            sw.Write("\r\n" + DateTime.Now.ToString() + "\r\n");
            sw.Close();
            //camera
            pictureBox9.Image = bmap_ov7620;
            gph_ov7620 = Graphics.FromImage(pictureBox9.Image);
            gph_ov7620.Clear(Color.Blue);//用于清空  
        }


        //第一个ccd
        private void ccd0_print()
        {
            //清图，画2个边框,内框和外框
            gph0.Clear(Color.White);
            gph0.DrawLine(Pens.Black, cpt_Large[0].X, cpt_Large[0].Y, cpt_Large[1].X, cpt_Large[1].Y);
            gph0.DrawLine(Pens.Black, cpt_Large[1].X, cpt_Large[1].Y, cpt_Large[3].X, cpt_Large[3].Y);
            gph0.DrawLine(Pens.Black, cpt_Large[0].X, cpt_Large[0].Y, cpt_Large[2].X, cpt_Large[2].Y);
            gph0.DrawLine(Pens.Black, cpt_Large[2].X, cpt_Large[2].Y, cpt_Large[3].X, cpt_Large[3].Y);
            gph0.DrawLine(Pens.Black, cpt_Small[0].X, cpt_Small[0].Y, cpt_Small[1].X, cpt_Small[1].Y);
            gph0.DrawLine(Pens.Black, cpt_Small[1].X, cpt_Small[1].Y, cpt_Small[3].X, cpt_Small[3].Y);
            gph0.DrawLine(Pens.Black, cpt_Small[0].X, cpt_Small[0].Y, cpt_Small[2].X, cpt_Small[2].Y);
            gph0.DrawLine(Pens.Black, cpt_Small[2].X, cpt_Small[2].Y, cpt_Small[3].X, cpt_Small[3].Y);
            //内部的虚线
            if (checkBox5.Checked)
            {
                gph0.DrawString("(4V,204)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 35));//图表标题
                gph0.DrawString("(3V,153)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 86));//图表标题
                gph0.DrawString("(2V,102)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 137));//图表标题
                gph0.DrawString("(1V, 51)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 188));//图表标题
                gph0.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 51, cpt_Small[2].X, cpt_Small[0].Y + 51);
                gph0.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 102, cpt_Small[2].X, cpt_Small[0].Y + 102);
                gph0.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 153, cpt_Small[2].X, cpt_Small[0].Y + 153);
                gph0.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 204, cpt_Small[2].X, cpt_Small[0].Y + 204);
            }
            //流写txt
            StreamWriter sw = File.AppendText("ccddata/data0.txt");
            //画点
            for (byte i = 0; i < 128; i++)
            {
                //画折线
                if (i < 127)
                    gph0.DrawLine(Pens.Red, cpt_Small[0].X + ccd_X[i], cpt_Small[1].Y - ccd_data0[i], cpt_Small[0].X + ccd_X[i + 1], cpt_Small[1].Y - ccd_data0[i + 1]);
                //流写txt
                if (checkBox9.Checked)
                    sw.Write(ccd_data0[i].ToString() + " ");
                //画圆圈 
                if (checkBox6.Checked)
                {
                    gph0.DrawEllipse(Pens.Red, cpt_Small[0].X + ccd_X[i] - 1, cpt_Small[1].Y - ccd_data0[i] - 2, 3, 3);
                    gph0.FillEllipse(new SolidBrush(Color.White), cpt_Small[0].X + ccd_X[i] - 1, cpt_Small[1].Y - ccd_data0[i] - 2, 3, 3);
                }
            }
            sw.Write("\r\n");
            sw.Close();//清空缓冲区、关闭流
            pictureBox7.Image = bmap0;
        }

        private void ccd1_print()
        {
            //清图，画2个边框
            gph1.Clear(Color.White);
            gph1.DrawLine(Pens.Black, cpt_Large[0].X, cpt_Large[0].Y, cpt_Large[1].X, cpt_Large[1].Y);
            gph1.DrawLine(Pens.Black, cpt_Large[1].X, cpt_Large[1].Y, cpt_Large[3].X, cpt_Large[3].Y);
            gph1.DrawLine(Pens.Black, cpt_Large[0].X, cpt_Large[0].Y, cpt_Large[2].X, cpt_Large[2].Y);
            gph1.DrawLine(Pens.Black, cpt_Large[2].X, cpt_Large[2].Y, cpt_Large[3].X, cpt_Large[3].Y);
            gph1.DrawLine(Pens.Black, cpt_Small[0].X, cpt_Small[0].Y, cpt_Small[1].X, cpt_Small[1].Y);
            gph1.DrawLine(Pens.Black, cpt_Small[1].X, cpt_Small[1].Y, cpt_Small[3].X, cpt_Small[3].Y);
            gph1.DrawLine(Pens.Black, cpt_Small[0].X, cpt_Small[0].Y, cpt_Small[2].X, cpt_Small[2].Y);
            gph1.DrawLine(Pens.Black, cpt_Small[2].X, cpt_Small[2].Y, cpt_Small[3].X, cpt_Small[3].Y);
            //内部的虚线
            if (checkBox8.Checked)
            {
                gph1.DrawString("(4V,204)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 35));//图表标题
                gph1.DrawString("(3V,153)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 86));//图表标题
                gph1.DrawString("(2V,102)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 137));//图表标题
                gph1.DrawString("(1V, 51)", new Font("Courier New", 10), Brushes.Black, new PointF(cpt_Small[0].X, cpt_Small[0].Y + 188));//图表标题
                gph1.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 51, cpt_Small[2].X, cpt_Small[0].Y + 51);
                gph1.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 102, cpt_Small[2].X, cpt_Small[0].Y + 102);
                gph1.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 153, cpt_Small[2].X, cpt_Small[0].Y + 153);
                gph1.DrawLine(pen1, cpt_Small[0].X, cpt_Small[0].Y + 204, cpt_Small[2].X, cpt_Small[0].Y + 204);
            }
            //画图
            for (byte i = 0; i < 128; i++)
            {
                //画折线
                if (i < 127)
                    gph1.DrawLine(Pens.Red, cpt_Small[0].X + ccd_X[i], cpt_Small[1].Y - ccd_data1[i], cpt_Small[0].X + ccd_X[i + 1], cpt_Small[1].Y - ccd_data1[i + 1]);
                //画点 
                if (checkBox7.Checked)
                {
                    gph1.DrawEllipse(Pens.Red, cpt_Small[0].X + ccd_X[i] - 1, cpt_Small[1].Y - ccd_data1[i] - 2, 3, 3);
                    gph1.FillEllipse(new SolidBrush(Color.White), cpt_Small[0].X + ccd_X[i] - 1, cpt_Small[1].Y - ccd_data1[i] - 2, 3, 3);
                }
            }
            pictureBox8.Image = bmap1;
        }



        /******************************** serial 接收函数*********************************/
        void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Closing_new == true)
                return;
            try
            {
                Listening = true;////设置标记，说明我已经开始处理数据，   一会儿要使用系统UI的。
                int n = comm.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致   
                byte[] Received_bytes = new byte[n];//声明一个临时数组存储当前来的串口数据
                if (mode == 0)
                {
                    comm.Read(Received_bytes, 0, n);//读取缓冲数据 
                    received_count += n;//增加接收计数  
                    builder.Clear();//清除字符串构造器的内容  
                    this.Invoke((EventHandler)(delegate//因为要访问ui资源，所以需要使用invoke方式同步ui。  
                    {
                        if (checkBox1.Checked)//判断是否是显示为16禁止  
                        {
                            foreach (byte b in Received_bytes) //依次的拼接出16进制字符串  
                                builder.Append(b.ToString("X2") + " ");
                        }
                        else
                        {
                            if (checkBox4.Checked == true)
                            {
                                string str = Encoding.ASCII.GetString(Received_bytes);//蓝牙AT时'\r'要剔除
                                builder.Append(str.Replace("\r", ""));//直接按ASCII规则转换成字符串
                            }
                            else
                                builder.Append(Encoding.GetEncoding("GB2312").GetString(Received_bytes));//已经可以支持中文
                        }
                        this.richTextBox1.AppendText(builder.ToString());//追加的形式添加到文本框末端，并滚动到最后。  
                        label3.Text = "已接收:" + received_count.ToString();//修改接收计数  
                    }));
                }
                else if (mode == 1)//画图mode
                {
                    comm.Read(Received_bytes, 0, n);//读取缓冲数据到 buf_mode1，n位要读的字节数
                    for (int i = 0; i < n; i++)
                    {
                        if (drawState == 0 && Received_bytes[i] == 'S')
                            drawState = 1;
                        else if (drawState == 1)
                        {
                            if (Received_bytes[i] == 'T')
                            {
                                drawState = 2;
                                drawDataCn = 0;
                                for (int j = 0; j < 48; j++)
                                    print_data[j] = 0;
                            }
                            else
                                drawState = 0;
                        }
                        else if (drawState == 2)
                        {
                            if (drawDataCn >= 48)
                            {
                                drawState = 0;
                                for (int j = 0; j < 24; j++)
                                {
                                    if (print_data[j * 2 + 1] <= 128)
                                        tmp[j] = (print_data[j * 2 + 1] << 8) + print_data[j * 2];
                                    else
                                        tmp[j] = (print_data[j * 2 + 1] << 8) + print_data[j * 2] - 65536;
                                }
                                //给zGraph的y赋值  
                                for (int loop = 0; loop < 8; loop++)
                                {
                                    zGraph0_y[loop].Add(tmp[loop]);
                                    zGraph1_y[loop].Add(tmp[loop + 8]);
                                    zGraph2_y[loop].Add(tmp[loop + 8 + 8]);
                                }
                                //x轴
                                if (zGraph_timerDrawI < zGraph_numLimits)
                                    zGraph_xAdd();
                                else
                                    zGraph_yRemoveAt();
                            }
                            else
                                print_data[drawDataCn++] = Received_bytes[i];
                        }
                    }
                }
                else if (mode == 2)//**************ccd功能**************
                {
                    comm.Read(Received_bytes, 0, n);//读取缓冲数据到 buf_mode1，n位要读的字节数
                    for (int i = 0; i < n; i++)
                    {
                        if (ccdRecState == 3)
                        {
                            if (ccdDataCn == 128 * ccdNum)
                            {
                                ccdRecState = 0;
                                ccd0_print();
                                ccd0_count++;
                                this.Invoke((EventHandler)(delegate { label32.Text = "帧计数：" + ccd0_count.ToString(); }));
                                if (ccdNum == 2)
                                {
                                    ccd1_print();
                                    ccd1_count++;
                                    this.Invoke((EventHandler)(delegate { label33.Text = "帧计数：" + ccd1_count.ToString(); }));
                                }
                            }
                            else
                            {
                                if (ccdDataCn < 128)
                                    ccd_data0[ccdDataCn++] = Received_bytes[i];
                                else
                                    ccd_data1[ccdDataCn++ - 128] = Received_bytes[i];
                            }
                        }
                        else if (ccdRecState == 0 && Received_bytes[i] == 255)
                            ccdRecState = 1;
                        else if (ccdRecState == 1)
                        {
                            if (Received_bytes[i] == 255)
                                ccdRecState = 2;
                            else
                                ccdRecState = 0;
                        }
                        else if (ccdRecState == 2)
                        {
                            if (Received_bytes[i] == 1 || Received_bytes[i] == 2)
                            {
                                ccdRecState = 3;
                                ccdNum = Received_bytes[i];
                                ccdDataCn = 0;
                            }
                            else
                                ccdRecState = 0;
                        }
                    }
                }
                else if (mode == 3)//**************摄像头显示功能**************
                {
                    comm.Read(Received_bytes, 0, n);//读取缓冲数据到 buf_mode1，n位要读的字节数
                    for (int i = 0; i < n; i++)
                    {
                        if (cameraState == 10)
                        {
                            if (checkBox10.Checked)
                            {
                                for (int j = 0; j < 8; j++)
                                {
                                    count_x++;
                                    if (((Received_bytes[i] << j) & 0x80) == 0)
                                        gph_ov7620.DrawEllipse(new Pen(Color.FromArgb(255, 255, 255)), count_x, count_y, 1, 1);
                                    else
                                        gph_ov7620.DrawEllipse(new Pen(Color.FromArgb(0, 0, 0)), count_x, count_y, 1, 1);
                                }
                            }
                            else
                            {
                                count_x++;
                                gph_ov7620.DrawEllipse(new Pen(Color.FromArgb(Received_bytes[i], Received_bytes[i], Received_bytes[i])), count_x, count_y, 1, 1);
                            }
                            if (count_x >= receive_X)
                            {
                                count_x = 0;
                                count_y++;
                                if (count_x % 20 == 0)
                                    this.Invoke((EventHandler)(delegate { label38.Text = count_y.ToString(); pictureBox9.Refresh(); }));
                                if (count_y >= receive_Y)
                                {
                                    cameraState = 0;
                                    camera_receive_count++;
                                    this.Invoke((EventHandler)(delegate
                                    {
                                        label39.Text = camera_receive_count.ToString();
                                        pictureBox9.Refresh();
                                        if (checkBox12.Checked == true)
                                            pictureBox9.Image.Save("camera_Image/camera_Image" + camera_receive_count.ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); ;
                                    }));
                                }

                            }
                        }
                        else if (cameraState == 0 && Received_bytes[i] == 'C') cameraState = 1;
                        else if (cameraState == 1) { if (Received_bytes[i] == 'A')cameraState = 2; else cameraState = 0; }
                        else if (cameraState == 2) { if (Received_bytes[i] == 'M')cameraState = 3; else cameraState = 0; }
                        else if (cameraState == 3) { if (Received_bytes[i] == 'E')cameraState = 4; else cameraState = 0; }
                        else if (cameraState == 4) { if (Received_bytes[i] == 'R')cameraState = 5; else cameraState = 0; }
                        else if (cameraState == 5) { if (Received_bytes[i] == 'A')cameraState = 6; else cameraState = 0; }
                        else if (cameraState == 6) { cameraState = 7; receive_Y = Received_bytes[i]; }
                        else if (cameraState == 7) { cameraState = 8; receive_Y = receive_Y * 256 + Received_bytes[i]; }
                        else if (cameraState == 8) { cameraState = 9; receive_X = Received_bytes[i]; }
                        else if (cameraState == 9)
                        {
                            cameraState = 10;
                            receive_X = receive_X * 256 + Received_bytes[i];
                            count_x = count_y = 0;
                            this.Invoke((EventHandler)(delegate { gph_ov7620.Clear(Color.White); label37.Text = "正在采集..."; }));//用于清空
                        }
                    }
                }
            }
            finally
            {
                Listening = false;//我用完了，ui可以关闭串口了。
            }
        }

        private void zGraph_xAdd()
        {
            for (int loop = 0; loop < 8; loop++)
                zGraph_x[loop].Add(zGraph_timerDrawI);
            zGraph_timerDrawI++;
        }

        private void zGraph_yRemoveAt()
        {
            //画图框
            for (int loop = 0; loop < 8; loop++)
            {
                zGraph0_y[loop].RemoveAt(0);
                zGraph1_y[loop].RemoveAt(0);
                zGraph2_y[loop].RemoveAt(0);
            }
        }

        //重要的打开和关闭串口操作
        private void btnComOpen_Click(object sender, EventArgs e)
        {
            //根据当前串口对象，来判断操作  
            if (comm.IsOpen)
            {
                Closing_new = true;
                while (Listening) Application.DoEvents();
                //模式1，串口示波器功能
                if (mode == 1)
                {
                    button数据显示模拟7.Enabled = true;
                    textBox数值.ReadOnly = false;
                    this.Focus();
                    f_timerDrawStop();
                    groupBox数据显示模拟.Visible = true;
                }
                else if (mode == 3)
                {
                    label38.Text = "0";
                    label37.Text = "欢迎使用！！！";
                    button91.Enabled = true;
                }
                comm.Close();//打开时点击，则关闭串口  
            }
            else
            {
                //没有串口的情况下
                if (cbComList.Text == "")
                {
                    MessageBox.Show("大哥，看看有没有串口啊!!!");
                    return;
                }
                if (mode == 1)//模式1，串口示波器功能
                {
                    button数据显示模拟7.Enabled = false;
                    textBox数值.ReadOnly = true;
                    Graph_init();//初始化3个画图框
                    f_timerDrawStart();//开始TIMER
                    groupBox数据显示模拟.Visible = false;
                }
                else if (mode == 2)//模式2，ccd
                {
                    ccd0_count = 0;
                    ccd1_count = 0;
                }
                else if (mode == 3)//模式3，camera初始化图形
                {
                    gph_ov7620.Clear(Color.White);//用于清空
                    button91.Enabled = false;
                    camera_receive_count = 0;
                    label37.Text = "正在搜索字头...";
                    label39.Text = "0";
                }
                //打开串口
                Closing_new = false;
                comm.PortName = cbComList.Text;
                comm.BaudRate = int.Parse(cbBaudRateList.Text);
                try
                {
                    comm.Open();
                }
                catch (Exception ex)
                {
                    comm = new SerialPort();//捕获到异常信息，创建一个新的comm对象，之前的不能用了。 
                    MessageBox.Show(ex.Message);//现实异常信息给客户。  
                }
            }
            btnComOpen.Text = comm.IsOpen ? "关闭端口" : "打开端口"; //设置按钮的状态  
            cbComList.Enabled = !comm.IsOpen;
            cbBaudRateList.Enabled = !comm.IsOpen;
        }

        private void Graph_init()
        {
            //读入zGraph0_current
            if (int.TryParse(textBox附加参数.Text.ToString(), out zGraph_current))
            {
                if (zGraph_current > 25 && zGraph_current < 300)
                    timerDraw.Interval = zGraph_current;
                else
                    textBox附加参数.Text = "50";
            }
            else
                textBox附加参数.Text = "50";
            //读入zGraph0_numLimits
            if (int.TryParse(textBox数值.Text.ToString(), out zGraph_numLimits))
            {
                if (zGraph_numLimits < 50 || zGraph_numLimits > 100000)
                {
                    zGraph_numLimits = 500;
                    textBox数值.Text = zGraph_numLimits.ToString();
                }
            }
            else
            {
                zGraph_numLimits = 500;
                textBox数值.Text = zGraph_numLimits.ToString();
            }
            //清除x轴
            for (int loop = 0; loop < 8; loop++)
            {
                zGraph_x[loop] = new List<float>();
                zGraph_x[loop].Clear();
            }
            //初始化zGraph0
            for (int loop = 0; loop < 8; loop++)
            {
                zGraph0_y[loop] = new List<float>();
                zGraph0_y[loop].Clear();
            }
            zGraph0.f_ClearAllPix();
            zGraph0.f_reXY();
            zGraph0.f_LoadOnePix(ref zGraph_x[0], ref zGraph0_y[0], color[0], 2);
            for (int loop = 1; loop < 8; loop++)
                zGraph0.f_AddPix(ref zGraph_x[loop], ref zGraph0_y[loop], color[loop], 2);
            //初始化zGraph1
            for (int loop = 0; loop < 8; loop++)
            {
                zGraph1_y[loop] = new List<float>();
                zGraph1_y[loop].Clear();
            }
            zGraph1.f_ClearAllPix();
            zGraph1.f_reXY();

            zGraph1.f_LoadOnePix(ref zGraph_x[0], ref zGraph1_y[0], color[0], 2);
            for (int loop = 1; loop < 8; loop++)
                zGraph1.f_AddPix(ref zGraph_x[loop], ref zGraph1_y[loop], color[loop], 2);
            //初始化zGraph2
            for (int loop = 0; loop < 8; loop++)
            {
                zGraph2_y[loop] = new List<float>();
                zGraph2_y[loop].Clear();
            }
            zGraph2.f_ClearAllPix();
            zGraph2.f_reXY();
            zGraph2.f_LoadOnePix(ref zGraph_x[0], ref zGraph2_y[0], color[0], 2);
            for (int loop = 1; loop < 8; loop++)
                zGraph2.f_AddPix(ref zGraph_x[loop], ref zGraph2_y[loop], color[loop], 2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            received_count = 0;
            richTextBox1.Text = "清空啦啦啦啦→_→\r\n";
            label3.Text = "已接收:0";
        }

        //发送的按键
        private void button2_Click(object sender, EventArgs e)
        {
            //定义一个变量，记录发送了几个字节 
            if (!comm.IsOpen)
                return;
            int n = 0;
            //16进制发送  
            if (checkBox2.Checked)
            {
                //我们不管规则了。如果写错了一些，我们允许的，只用正则得到有效的十六进制数  
                MatchCollection mc = Regex.Matches(richTextBox2.Text, @"(?i)[/da-f]{2}");
                List<byte> buf = new List<byte>();//填充到这个临时列表中  
                //依次添加到列表中  
                foreach (Match m in mc)
                {
                    buf.Add(byte.Parse(m.Value));
                }
                //转换列表为数组后发送  
                comm.Write(buf.ToArray(), 0, buf.Count);
                //记录发送的字节数  
                n = buf.Count;
            }
            else//ascii编码直接发送  
            {
                //包含换行符  
                if (checkBox3.Checked)
                {
                    comm.Write(richTextBox2.Text + System.Environment.NewLine);
                    n = richTextBox2.Text.Length + 2;
                }
                else//不包含换行符  
                {
                    string a = richTextBox2.Text;
                    string s = richTextBox2.Text;
                    n = richTextBox2.Text.Length;
                    if (a[n - 1] == '\n')
                        s = a.Substring(0, n - 1) + "\r\n";
                    n = n + 1;
                    comm.Write(s);
                }
            }
            send_count += n;//累加发送字节数  
            label9.Text = "已发送:" + send_count.ToString();//更新界面  
        }

        private void button3_Click(object sender, EventArgs e)
        {
            send_count = 0;
            label9.Text = "已发送:0";
            richTextBox2.Text = "";
        }

        //tab变化
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            mode = tabControl1.SelectedIndex;
            if (mode == 1)
                ResizezGraph();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write("AT+BAUD" + textBox1.Text + "\r\n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write("AT\r\n");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write("AT+NAME" + textBox2.Text + "\r\n");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write("AT+PIN" + textBox3.Text + "\r\n");
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
            {
                button4.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                button7.Enabled = true;
                button8.Enabled = true;
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                textBox4.Enabled = true;
            }
            else
            {
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
                button8.Enabled = false;
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;
                textBox4.Enabled = false;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write("AT+ROLE" + textBox4.Text + "\r\n");
        }

        /******************* timerDraw *******************/
        private void timer1_Tick(object sender, EventArgs e)
        {
            progRunFlag = true;
            zGraph0.f_Refresh();
            zGraph1.f_Refresh();
            zGraph2.f_Refresh();
        }
        private void f_timerDrawStart()
        {
            zGraph_timerDrawI = 0;
            progRunFlag = true;
            timerDraw.Start();
            textBox附加参数.ReadOnly = true;
            textBox数值.ReadOnly = true;
        }

        private void f_timerDrawStop()
        {
            progRunFlag = false;
            timerDraw.Stop();
            textBox附加参数.ReadOnly = false;
            textBox数值.ReadOnly = false;
        }

        private void button数据显示模拟7_Click(object sender, EventArgs e)
        {
            timerDraw.Stop();
            zGraph0.f_ClearAllPix();
            zGraph1.f_ClearAllPix();
            zGraph2.f_ClearAllPix();
        }

        private void a_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void b_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void c_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button19_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button26_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button29_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button34_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button33_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);

        }

        private void button32_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button38_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button37_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button36_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button35_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button42_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button41_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button40_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void button39_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            Button senderButton = (Button)sender;//根据sender引用控件。
            comm.Write(senderButton.Text);
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (comm.IsOpen == true)
                e.Cancel = true;
            else
                e.Cancel = false;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            ccd0_print();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            ccd0_print();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            ccd1_print();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            ccd1_print();
        }

        private void button80_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[0]);
        }

        private void button81_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[1]);
        }

        private void button82_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[2]);
        }

        private void button83_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[3]);
        }

        private void button84_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[4]);
        }

        private void button85_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[5]);
        }

        private void button86_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[6]);
        }

        private void button87_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[7]);
        }

        private void button88_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[8]);
        }

        private void button89_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[9]);
        }

        private void button90_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write(lines自定义发送_string[10]);
        }

        private void button91_Click(object sender, EventArgs e)
        {
            gph_ov7620.Clear(Color.White);//用于清空 
            pictureBox9.Image = bmap_ov7620;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            ResizezGraph();
        }

        void ResizezGraph()
        {
            zGraph0.Location = new Point(0,0);
            zGraph0.Height = (int)(tabPage3.Height);
            zGraph0.Width = (int)(tabPage3.Width * 0.5);

            zGraph1.Location = new Point((int)(tabPage3.Width * 0.5), 0);
            zGraph1.Height = (int)(tabPage3.Height * 0.5);
            zGraph1.Width = (int)(tabPage3.Width * 0.5);

            zGraph2.Location = new Point((int)(tabPage3.Width * 0.5), (int)(tabPage3.Height * 0.5));
            zGraph2.Height = (int)(tabPage3.Height * 0.5);
            zGraph2.Width = (int)(tabPage3.Width * 0.5);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/wangdongxuking61/C-sharp_PC2MCU_Draw");
        }

        int MCU = 0,  readmeMODE= 0;
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            readmeMODE = comboBox3.SelectedIndex;
            richTextBox使用说明.Text = "";
            string[] lines = File.ReadAllLines("readme/read"+MCU+readmeMODE+".c");
            for (int i = 0; i < lines.Length; i++)
                richTextBox使用说明.Text += lines[i] + "\n";
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            MCU = comboBox2.SelectedIndex;
            richTextBox使用说明.Text = "";
            string[] lines = File.ReadAllLines("readme/read" + MCU + readmeMODE + ".c");
            for (int i = 0; i < lines.Length; i++)
                richTextBox使用说明.Text += lines[i] + "\n";
        }
    }
}