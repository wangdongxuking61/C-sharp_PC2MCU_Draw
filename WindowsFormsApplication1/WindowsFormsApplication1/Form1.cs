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
        //通用变量
        private SerialPort comm = new SerialPort();
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。  
        private bool Listening = false;//是否没有执行完invoke相关操作
        private bool Closing_new = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke
        private long received_count = 0;//接收计数  
        private long send_count = 0;//发送计数 
        private int mode = 0;
        
        //接收数据相关
        byte[] print_data = new byte[257];
        int print_data_count = 0;
        int[] tmp = new int[50];
        int time_count = 0;

        //单片机用到的变量
        private double OFFSET_angle = 0;
        private double Tg = 0;
        
        //画图需要的变量
        private Color[] m_colors;
        private float m_fstyle;
        private int[] m_istyle;
        public bool progRunFlag = false;
        int zGraph_numLimits = 500;
        private int zGraph_timerDrawI = 0;
        int zGraph_current;

        //3幅图像每个曲线个数
        int zGraph0_num = 3;
        int zGraph1_num = 0;
        int zGraph2_num = 0;
        int max_zGraph_num;


        //ccd数据
        Bitmap bmap0 = new Bitmap(806, 295);
        Bitmap bmap1 = new Bitmap(806, 295);
        PointF[] cpt_Large = new PointF[4] { new PointF(0, 0), new PointF(0, 294), new PointF(805, 0), new PointF(805, 294) };//外面的框
        PointF[] cpt_Small = new PointF[4] { new PointF(20, 20), new PointF(20, 275), new PointF(785, 20), new PointF(785, 275) };//里面的框
        byte[] ccd_data0 = new byte[128];
        byte[] ccd_data1 = new byte[128];
        int ccd0_count = 0;
        int ccd1_count = 0;
        int[] ccd_X = new int[128];
        Graphics gph0;
        Graphics gph1;
        Pen pen1 = new Pen(Color.Black);
        int ccddata_num = 1;
        string[] lines自定义发送_string = new string[15];

        //摄像头数据
        Bitmap bmap_ov7620 = new Bitmap(700, 300);
        Graphics gph_ov7620;
        bool camera_start_flag = false;
        int count_x, receive_X;
        int count_y, receive_Y;
        int camera_receive_count = 0;
            

        // **测试数据**
        #region **测试数据**
        public List<float> zGraph_x1 = new List<float>();
        public List<float> zGraph_x2 = new List<float>();
        public List<float> zGraph_x3 = new List<float>();
        public List<float> zGraph_x4 = new List<float>();
        public List<float> zGraph_x5 = new List<float>();
        public List<float> zGraph_x6 = new List<float>();
        public List<float> zGraph_x7 = new List<float>();
        public List<float> zGraph_x8 = new List<float>();

        //zGraph0的y轴
        public List<float> zGraph0_y1 = new List<float>();
        public List<float> zGraph0_y2 = new List<float>();
        public List<float> zGraph0_y3 = new List<float>(); 
        public List<float> zGraph0_y4 = new List<float>();
        public List<float> zGraph0_y5 = new List<float>();
        public List<float> zGraph0_y6 = new List<float>();
        public List<float> zGraph0_y7 = new List<float>();
        public List<float> zGraph0_y8 = new List<float>();
        //zGraph1的y轴
        public List<float> zGraph1_y1 = new List<float>();
        public List<float> zGraph1_y2 = new List<float>();
        public List<float> zGraph1_y3 = new List<float>();
        public List<float> zGraph1_y4 = new List<float>();
        public List<float> zGraph1_y5 = new List<float>();
        public List<float> zGraph1_y6 = new List<float>();
        public List<float> zGraph1_y7 = new List<float>();
        public List<float> zGraph1_y8 = new List<float>();
        //zGraph2的y轴
        public List<float> zGraph2_y1 = new List<float>();
        public List<float> zGraph2_y2 = new List<float>();
        public List<float> zGraph2_y3 = new List<float>();
        public List<float> zGraph2_y4 = new List<float>();
        public List<float> zGraph2_y5 = new List<float>();
        public List<float> zGraph2_y6 = new List<float>();
        public List<float> zGraph2_y7 = new List<float>();
        public List<float> zGraph2_y8 = new List<float>();
        #endregion
        Random rand = new Random();


        //Form1的加载
        public Form1()
        {
            InitializeComponent();
            f_saveReadFirst(false);
            f_reStyle();//各种控件初始化
        }


        // 获取初始的波形显示控件的样式或设置为初始样式
        private void f_saveReadFirst(bool isRead)
        {
            if (!isRead)
            {
                m_colors = new Color[18];
                m_istyle = new int[2];
                m_istyle[0] = zGraph0.m_titleSize;
                m_fstyle = zGraph0.m_titlePosition;
                m_colors[0] = zGraph0.m_titleColor;
                m_colors[1] = zGraph0.m_titleBorderColor;
                m_colors[2] = zGraph0.m_backColorL;
                m_colors[3] = zGraph0.m_backColorH;
                m_colors[4] = zGraph0.m_coordinateLineColor;
                m_colors[5] = zGraph0.m_coordinateStringColor;
                m_colors[6] = zGraph0.m_coordinateStringTitleColor;
                m_istyle[1] = zGraph0.m_iLineShowColorAlpha;
                m_colors[7] = zGraph0.m_iLineShowColor;
                m_colors[8] = zGraph0.m_GraphBackColor;
                m_colors[9] = zGraph0.m_ControlItemBackColor;
                m_colors[10] = zGraph0.m_ControlButtonBackColor;
                m_colors[11] = zGraph0.m_ControlButtonForeColorL;
                m_colors[12] = zGraph0.m_ControlButtonForeColorH;
                m_colors[13] = zGraph0.m_DirectionBackColor;
                m_colors[14] = zGraph0.m_DirectionForeColor;
                m_colors[15] = zGraph0.m_BigXYBackColor;
                m_colors[16] = zGraph0.m_BigXYButtonBackColor;
                m_colors[17] = zGraph0.m_BigXYButtonForeColor;
            }
            else
            {
                textBox标题位置.Text = m_fstyle.ToString();
                textBox网络线的透明度.Text = m_istyle[1].ToString();
                textBox标题字体大小.Text = m_istyle[0].ToString();
                //波形图0样式
                zGraph0.m_titleSize = m_istyle[0];
                zGraph0.m_titlePosition = m_fstyle;
                zGraph0.m_titleColor = button标题颜色.BackColor = m_colors[0];
                zGraph0.m_titleBorderColor = button标题描边颜色.BackColor = m_colors[1];
                zGraph0.m_backColorL = button背景色渐进起始颜色.BackColor = m_colors[2];
                zGraph0.m_backColorH = button背景色渐进终止颜色.BackColor = m_colors[3];
                zGraph0.m_coordinateLineColor = button坐标线颜色.BackColor = m_colors[4];
                zGraph0.m_coordinateStringColor = button坐标值颜色.BackColor = m_colors[5];
                zGraph0.m_coordinateStringTitleColor = button坐标标题颜色.BackColor = m_colors[6];
                zGraph0.m_iLineShowColorAlpha = m_istyle[1];
                zGraph0.m_iLineShowColor = button网络线的颜色.BackColor = m_colors[7];
                zGraph0.m_GraphBackColor = button波形显示区域背景色.BackColor = m_colors[8];
                zGraph0.m_ControlItemBackColor = button工具栏背景色.BackColor = m_colors[9];
                zGraph0.m_ControlButtonBackColor = button工具栏按钮背景色.BackColor = m_colors[10];
                zGraph0.m_ControlButtonForeColorL = button工具栏按钮前景选中颜色.BackColor = m_colors[11];
                zGraph0.m_ControlButtonForeColorH = button工具栏按钮前景未选中颜色.BackColor = m_colors[12];
                zGraph0.m_DirectionBackColor = button标签说明框背景颜色.BackColor = m_colors[13];
                zGraph0.m_DirectionForeColor = button标签说明框文字颜色.BackColor = m_colors[14];
                zGraph0.m_BigXYBackColor = button放大选取框背景颜色.BackColor = m_colors[15];
                zGraph0.m_BigXYButtonBackColor = button放大选取框按钮背景颜色.BackColor = m_colors[16];
                zGraph0.m_BigXYButtonForeColor = button放大选取框按钮文字颜色.BackColor = m_colors[17];
                //波形图1样式
                zGraph1.m_titleSize = m_istyle[0];
                zGraph1.m_titlePosition = m_fstyle;
                zGraph1.m_titleColor = button标题颜色.BackColor = m_colors[0];
                zGraph1.m_titleBorderColor = button标题描边颜色.BackColor = m_colors[1];
                zGraph1.m_backColorL = button背景色渐进起始颜色.BackColor = m_colors[2];
                zGraph1.m_backColorH = button背景色渐进终止颜色.BackColor = m_colors[3];
                zGraph1.m_coordinateLineColor = button坐标线颜色.BackColor = m_colors[4];
                zGraph1.m_coordinateStringColor = button坐标值颜色.BackColor = m_colors[5];
                zGraph1.m_coordinateStringTitleColor = button坐标标题颜色.BackColor = m_colors[6];
                zGraph1.m_iLineShowColorAlpha = m_istyle[1];
                zGraph1.m_iLineShowColor = button网络线的颜色.BackColor = m_colors[7];
                zGraph1.m_GraphBackColor = button波形显示区域背景色.BackColor = m_colors[8];
                zGraph1.m_ControlItemBackColor = button工具栏背景色.BackColor = m_colors[9];
                zGraph1.m_ControlButtonBackColor = button工具栏按钮背景色.BackColor = m_colors[10];
                zGraph1.m_ControlButtonForeColorL = button工具栏按钮前景选中颜色.BackColor = m_colors[11];
                zGraph1.m_ControlButtonForeColorH = button工具栏按钮前景未选中颜色.BackColor = m_colors[12];
                zGraph1.m_DirectionBackColor = button标签说明框背景颜色.BackColor = m_colors[13];
                zGraph1.m_DirectionForeColor = button标签说明框文字颜色.BackColor = m_colors[14];
                zGraph1.m_BigXYBackColor = button放大选取框背景颜色.BackColor = m_colors[15];
                zGraph1.m_BigXYButtonBackColor = button放大选取框按钮背景颜色.BackColor = m_colors[16];
                zGraph1.m_BigXYButtonForeColor = button放大选取框按钮文字颜色.BackColor = m_colors[17];
                //波形图2样式
                zGraph2.m_titleSize = m_istyle[0];
                zGraph2.m_titlePosition = m_fstyle;
                zGraph2.m_titleColor = button标题颜色.BackColor = m_colors[0];
                zGraph2.m_titleBorderColor = button标题描边颜色.BackColor = m_colors[1];
                zGraph2.m_backColorL = button背景色渐进起始颜色.BackColor = m_colors[2];
                zGraph2.m_backColorH = button背景色渐进终止颜色.BackColor = m_colors[3];
                zGraph2.m_coordinateLineColor = button坐标线颜色.BackColor = m_colors[4];
                zGraph2.m_coordinateStringColor = button坐标值颜色.BackColor = m_colors[5];
                zGraph2.m_coordinateStringTitleColor = button坐标标题颜色.BackColor = m_colors[6];
                zGraph2.m_iLineShowColorAlpha = m_istyle[1];
                zGraph2.m_iLineShowColor = button网络线的颜色.BackColor = m_colors[7];
                zGraph2.m_GraphBackColor = button波形显示区域背景色.BackColor = m_colors[8];
                zGraph2.m_ControlItemBackColor = button工具栏背景色.BackColor = m_colors[9];
                zGraph2.m_ControlButtonBackColor = button工具栏按钮背景色.BackColor = m_colors[10];
                zGraph2.m_ControlButtonForeColorL = button工具栏按钮前景选中颜色.BackColor = m_colors[11];
                zGraph2.m_ControlButtonForeColorH = button工具栏按钮前景未选中颜色.BackColor = m_colors[12];
                zGraph2.m_DirectionBackColor = button标签说明框背景颜色.BackColor = m_colors[13];
                zGraph2.m_DirectionForeColor = button标签说明框文字颜色.BackColor = m_colors[14];
                zGraph2.m_BigXYBackColor = button放大选取框背景颜色.BackColor = m_colors[15];
                zGraph2.m_BigXYButtonBackColor = button放大选取框按钮背景颜色.BackColor = m_colors[16];
                zGraph2.m_BigXYButtonForeColor = button放大选取框按钮文字颜色.BackColor = m_colors[17];
            }
        }


        
        // 获取波形显示控件基本属性和样式，并更新到该程序界面
        private void f_reStyle()
        {
            //样式
            textBox标题字体大小.Text = zGraph0.m_titleSize.ToString();
            textBox标题位置.Text = zGraph0.m_titlePosition.ToString();
            button标题颜色.BackColor = zGraph0.m_titleColor;
            button标题描边颜色.BackColor = zGraph0.m_titleBorderColor;
            button背景色渐进起始颜色.BackColor = zGraph0.m_backColorL;
            button背景色渐进终止颜色.BackColor = zGraph0.m_backColorH;
            button坐标线颜色.BackColor = zGraph0.m_coordinateLineColor;
            button坐标值颜色.BackColor = zGraph0.m_coordinateStringColor;
            button坐标标题颜色.BackColor = zGraph0.m_coordinateStringTitleColor;
            textBox网络线的透明度.Text = zGraph0.m_iLineShowColorAlpha.ToString();
            button网络线的颜色.BackColor = zGraph0.m_iLineShowColor;
            button波形显示区域背景色.BackColor = zGraph0.m_GraphBackColor;
            button工具栏背景色.BackColor = zGraph0.m_ControlItemBackColor;
            button工具栏按钮背景色.BackColor = zGraph0.m_ControlButtonBackColor;
            button工具栏按钮前景选中颜色.BackColor = zGraph0.m_ControlButtonForeColorL;
            button工具栏按钮前景未选中颜色.BackColor = zGraph0.m_ControlButtonForeColorH;
            button标签说明框背景颜色.BackColor = zGraph0.m_DirectionBackColor;
            button标签说明框文字颜色.BackColor = zGraph0.m_DirectionForeColor;
            button放大选取框背景颜色.BackColor = zGraph0.m_BigXYBackColor;
            button放大选取框按钮背景颜色.BackColor = zGraph0.m_BigXYButtonBackColor;
            button放大选取框按钮文字颜色.BackColor = zGraph0.m_BigXYButtonForeColor;
        }


        //窗口初始化，各种变量
        private void Form1_Load(object sender, EventArgs e)
        {
            //MessageBox.Show("凌立印象!!!");
            cbBaudRateList.SelectedItem = cbBaudRateList.Items[6];//波特率初始化
            comboBox图片0曲线数目.SelectedItem = comboBox图片0曲线数目.Items[3];
            comboBox图片1曲线数目.SelectedItem = comboBox图片1曲线数目.Items[0];
            comboBox图片2曲线数目.SelectedItem = comboBox图片2曲线数目.Items[0];
            string[] lines = File.ReadAllLines("说明.txt");
            for (int i=0; i < 142;i++ )
                richTextBox使用说明.Text += lines[i]+"\n";
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            cbComList.Items.AddRange(ports);
            cbComList.SelectedIndex = cbComList.Items.Count > 0 ? 0 : -1;
            //初始化SerialPort对象  
            comm.NewLine = "/r/n";
            comm.RtsEnable = true;//根据实际情况吧。 
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
            

            //camera
            pictureBox9.Image = bmap_ov7620;
            gph_ov7620 = Graphics.FromImage(pictureBox9.Image);
            gph_ov7620.Clear(Color.Blue);//用于清空  
            
            /*bmap_ov7620.Save("gph_ov7620.bmp", ImageFormat.Bmp);
            this.pictureBox9.Image.Save("gph_ov7620ff.bmp",System.Drawing.Imaging.ImageFormat.Bmp);*/


            //ccd 
            //http:是//www.cnblogs.com/nine425/archive/2007/06/28/799473.html
            comboBox1.SelectedItem = comboBox1.Items[1];
            pen1.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            for (byte i = 0; i < 128; i++)
                ccd_X[i] = i * 6;
            //ccd0数据初始化赋值
            for (byte i = 0; i < 128; i++)//ccd0
                ccd_data0[i] = i;
            ccd_data0[64] = 0;
            //ccd0画图
            gph0 = Graphics.FromImage(bmap0);
            ccd0_print();
            //ccd0数据存储
            StreamWriter sw = File.AppendText("ccddata/data0.txt");
            sw.Write("\r\n" + DateTime.Now.ToString() + "\r\n");
            sw.Close();
            //ccd1数据出事
            for (byte i = 0; i < 128; i++)  //ccd1
                ccd_data1[i] = i;
            ccd_data1[64] = 0;
            //ccd0画图
            gph1 = Graphics.FromImage(bmap1);
            ccd1_print();
            
            

            //ccd自定义按键初始化
            string[] lines自定义发送 = File.ReadAllLines("自定义发送.txt");
            int lines自定义发送_num = int.Parse(lines自定义发送[0]);
            if (lines自定义发送_num > 0)
            {
                button80.Text = lines自定义发送[2];
                lines自定义发送_string[0] = lines自定义发送[3];
            }
            else
                button80.Enabled = false;
            if (lines自定义发送_num > 1)
            {
                button81.Text = lines自定义发送[4];
                lines自定义发送_string[1] = lines自定义发送[5];
            }
            else
                button81.Enabled = false;
            if (lines自定义发送_num > 2)
            {
                button82.Text = lines自定义发送[6];
                lines自定义发送_string[2] = lines自定义发送[7];
            }
            else
                button82.Enabled = false;
            if (lines自定义发送_num > 3)
            {
                button83.Text = lines自定义发送[8];
                lines自定义发送_string[3] = lines自定义发送[9];
            }
            else
                button83.Enabled = false;
            if (lines自定义发送_num > 4)
            {
                button84.Text = lines自定义发送[10];
                lines自定义发送_string[4] = lines自定义发送[11];
            }
            else
                button84.Enabled = false;
            if (lines自定义发送_num > 5)
            {
                button85.Text = lines自定义发送[12];
                lines自定义发送_string[5] = lines自定义发送[13];
            }
            else
                button85.Enabled = false;
            if (lines自定义发送_num > 6)
            {
                button86.Text = lines自定义发送[14];
                lines自定义发送_string[6] = lines自定义发送[15];
            }
            else
                button86.Enabled = false;
            if (lines自定义发送_num > 7)
            {
                button87.Text = lines自定义发送[16];
                lines自定义发送_string[7] = lines自定义发送[17];
            }
            else
                button87.Enabled = false;
            if (lines自定义发送_num > 8)
            {
                button88.Text = lines自定义发送[18];
                lines自定义发送_string[8] = lines自定义发送[19];
            }
            else
                button88.Enabled = false;
            if (lines自定义发送_num > 9)
            {
                button89.Text = lines自定义发送[20];
                lines自定义发送_string[9] = lines自定义发送[21];
            }
            else
                button89.Enabled = false;
            if (lines自定义发送_num > 10)
            {
                button90.Text = lines自定义发送[22];
                lines自定义发送_string[10] = lines自定义发送[23];
            }
            else
                button90.Enabled = false;
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



        /********************************接收函数*********************************/
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
                    //因为要访问ui资源，所以需要使用invoke方式同步ui。  
                    this.Invoke((EventHandler)(delegate
                    {
                        //判断是否是显示为16禁止  
                        if (checkBox1.Checked)
                        {
                            //依次的拼接出16进制字符串  
                            foreach (byte b in Received_bytes)
                                builder.Append(b.ToString("X2") + " ");
                        }
                        else
                        {
                            if (checkBox4.Checked == true)
                            {
                                string str = Encoding.ASCII.GetString(Received_bytes);//蓝牙AT时'\r'要剔除
                                builder.Append(str.Replace("\r",""));//直接按ASCII规则转换成字符串
                            }
                            else
                                builder.Append(Encoding.GetEncoding("GB2312").GetString(Received_bytes));//已经可以支持中文
                        }
                        this.richTextBox1.AppendText(builder.ToString());//追加的形式添加到文本框末端，并滚动到最后。  
                        label3.Text = "已接收:" + received_count.ToString();//修改接收计数  
                    }));
                }
                else if (mode == 1)
                {
                    time_count++;
                    if (time_count > 3 )  //刚上电不显示。因为电路建立连接不稳定
                    {
                        comm.Read(Received_bytes, 0, n);//读取缓冲数据到 buf_mode1，n位要读的字节数
                        for (int i = 0; i < n; i++)
                        {
                            if (Received_bytes[i] != 'T')//不是'T'写入print_data
                            {
                                if (print_data_count > (zGraph0_num + zGraph1_num + zGraph2_num) * 2 + 1)
                                    print_data_count = 0;
                                print_data[print_data_count] = Received_bytes[i];
                                print_data_count++;
                            }
                            else
                            {
                                if ((print_data_count == (zGraph0_num + zGraph1_num + zGraph2_num) * 2 + 1) && print_data[print_data_count - 1] == 'S')//print_data_count =7同时上一个为'S',此时判断为是一个周期结束
                                {
                                    print_data_count = 0;//计数器清零
                                    try
                                    {
                                        int j = 0;
                                        for (j = 0; j < (zGraph0_num + zGraph1_num + zGraph2_num); j++)
                                        {
                                            int temp = j * 2;
                                            if (print_data[temp + 1] <= 128)
                                                tmp[j] = (print_data[temp + 1] << 8) + print_data[temp];
                                            else
                                                tmp[j] = (print_data[temp + 1] << 8) + print_data[temp] - 65536;
                                        }
                                        //给zGraph0的y赋值
                                        if (zGraph0_num != 0)
                                        {
                                            if (zGraph0_num == 1)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                            }
                                            else if (zGraph0_num == 2)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                            }
                                            else if (zGraph0_num == 3)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                                zGraph0_y3.Add(tmp[2]);
                                            }
                                            else if (zGraph0_num == 4)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                                zGraph0_y3.Add(tmp[2]);
                                                zGraph0_y4.Add(tmp[3]);
                                            }
                                            else if (zGraph0_num == 5)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                                zGraph0_y3.Add(tmp[2]);
                                                zGraph0_y4.Add(tmp[3]);
                                                zGraph0_y5.Add(tmp[4]);
                                            }
                                            else if (zGraph0_num == 6)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                                zGraph0_y3.Add(tmp[2]);
                                                zGraph0_y4.Add(tmp[3]);
                                                zGraph0_y5.Add(tmp[4]);
                                                zGraph0_y6.Add(tmp[5]);
                                            }
                                            else if (zGraph0_num == 7)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                                zGraph0_y3.Add(tmp[2]);
                                                zGraph0_y4.Add(tmp[3]);
                                                zGraph0_y5.Add(tmp[4]);
                                                zGraph0_y6.Add(tmp[5]);
                                                zGraph0_y7.Add(tmp[6]);
                                            }
                                            else if (zGraph0_num == 8)
                                            {
                                                zGraph0_y1.Add(tmp[0]);
                                                zGraph0_y2.Add(tmp[1]);
                                                zGraph0_y3.Add(tmp[2]);
                                                zGraph0_y4.Add(tmp[3]);
                                                zGraph0_y5.Add(tmp[4]);
                                                zGraph0_y6.Add(tmp[5]);
                                                zGraph0_y7.Add(tmp[6]);
                                                zGraph0_y8.Add(tmp[7]);
                                            }
                                        }
                                       //给zGraph1的y赋值
                                       if (zGraph1_num != 0)
                                       {
                                           if (zGraph1_num == 1)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                           }
                                           else if (zGraph1_num == 2)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                           }
                                           else if (zGraph1_num == 3)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                               zGraph1_y3.Add(tmp[zGraph0_num + 2]); ;
                                           }
                                           else if (zGraph1_num == 4)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                               zGraph1_y3.Add(tmp[zGraph0_num + 2]);
                                               zGraph1_y4.Add(tmp[zGraph0_num + 3]);
                                           }
                                           else if (zGraph1_num == 5)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                               zGraph1_y3.Add(tmp[zGraph0_num + 2]);
                                               zGraph1_y4.Add(tmp[zGraph0_num + 3]);
                                               zGraph1_y5.Add(tmp[zGraph0_num + 4]);
                                           }
                                           else if (zGraph1_num == 6)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                               zGraph1_y3.Add(tmp[zGraph0_num + 2]);
                                               zGraph1_y4.Add(tmp[zGraph0_num + 3]);
                                               zGraph1_y5.Add(tmp[zGraph0_num + 4]);
                                               zGraph1_y6.Add(tmp[zGraph0_num + 5]);
                                           }
                                           else if (zGraph1_num == 7)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                               zGraph1_y3.Add(tmp[zGraph0_num + 2]);
                                               zGraph1_y4.Add(tmp[zGraph0_num + 3]);
                                               zGraph1_y5.Add(tmp[zGraph0_num + 4]);
                                               zGraph1_y6.Add(tmp[zGraph0_num + 5]);
                                               zGraph1_y7.Add(tmp[zGraph0_num + 6]);
                                           }
                                           else if (zGraph1_num == 8)
                                           {
                                               zGraph1_y1.Add(tmp[zGraph0_num + 0]);
                                               zGraph1_y2.Add(tmp[zGraph0_num + 1]);
                                               zGraph1_y3.Add(tmp[zGraph0_num + 2]);
                                               zGraph1_y4.Add(tmp[zGraph0_num + 3]);
                                               zGraph1_y5.Add(tmp[zGraph0_num + 4]);
                                               zGraph1_y6.Add(tmp[zGraph0_num + 5]);
                                               zGraph1_y7.Add(tmp[zGraph0_num + 6]);
                                               zGraph1_y8.Add(tmp[zGraph0_num + 7]);
                                           }
                                       }
                                       //给zGraph2的y赋值
                                       if (zGraph2_num != 0)
                                       {
                                           if (zGraph2_num == 1)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                           }
                                           else if (zGraph2_num == 2)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                           }
                                           else if (zGraph2_num == 3)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                               zGraph2_y3.Add(tmp[zGraph0_num + zGraph1_num + 2]); ;
                                           }
                                           else if (zGraph2_num == 4)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                               zGraph2_y3.Add(tmp[zGraph0_num + zGraph1_num + 2]);
                                               zGraph2_y4.Add(tmp[zGraph0_num + zGraph1_num + 3]);
                                           }
                                           else if (zGraph2_num == 5)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                               zGraph2_y3.Add(tmp[zGraph0_num + zGraph1_num + 2]);
                                               zGraph2_y4.Add(tmp[zGraph0_num + zGraph1_num + 3]);
                                               zGraph2_y5.Add(tmp[zGraph0_num + zGraph1_num + 4]);
                                           }
                                           else if (zGraph2_num == 6)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                               zGraph2_y3.Add(tmp[zGraph0_num + zGraph1_num + 2]);
                                               zGraph2_y4.Add(tmp[zGraph0_num + zGraph1_num + 3]);
                                               zGraph2_y5.Add(tmp[zGraph0_num + zGraph1_num + 4]);
                                               zGraph2_y6.Add(tmp[zGraph0_num + zGraph1_num + 5]);
                                           }
                                           else if (zGraph2_num == 7)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                               zGraph2_y3.Add(tmp[zGraph0_num + zGraph1_num + 2]);
                                               zGraph2_y4.Add(tmp[zGraph0_num + zGraph1_num + 3]);
                                               zGraph2_y5.Add(tmp[zGraph0_num + zGraph1_num + 4]);
                                               zGraph2_y6.Add(tmp[zGraph0_num + zGraph1_num + 5]);
                                               zGraph2_y7.Add(tmp[zGraph0_num + zGraph1_num + 6]);
                                           }
                                           else if (zGraph2_num == 8)
                                           {
                                               zGraph2_y1.Add(tmp[zGraph0_num + zGraph1_num + 0]);
                                               zGraph2_y2.Add(tmp[zGraph0_num + zGraph1_num + 1]);
                                               zGraph2_y3.Add(tmp[zGraph0_num + zGraph1_num + 2]);
                                               zGraph2_y4.Add(tmp[zGraph0_num + zGraph1_num + 3]);
                                               zGraph2_y5.Add(tmp[zGraph0_num + zGraph1_num + 4]);
                                               zGraph2_y6.Add(tmp[zGraph0_num + zGraph1_num + 5]);
                                               zGraph2_y7.Add(tmp[zGraph0_num + zGraph1_num + 6]);
                                               zGraph2_y8.Add(tmp[zGraph0_num + zGraph1_num + 7]);
                                           }
                                       }
                                        //第一幅图片
                                        if (zGraph_timerDrawI < zGraph_numLimits)
                                        {
                                            zGraph_xAdd();
                                        }
                                        else
                                        {
                                            zGraph_yRemoveAt();
                                        }      
                                    }
                                    catch
                                    {
                                        return;
                                    } 
                                }
                                else
                                {
                                    if ((print_data_count>=1 && print_data_count < (zGraph0_num + zGraph1_num + zGraph2_num) * 2 + 1) && print_data[print_data_count - 1] == 'S')
                                        print_data_count = 0;
                                    else if (print_data_count > (zGraph0_num + zGraph1_num + zGraph2_num) * 2 + 1)  //以T打头时候，数据过多，清零
                                        print_data_count = 0;
                                    else
                                    {   //数据中掺杂'T'的时候
                                        print_data[print_data_count] = Received_bytes[i];
                                        print_data_count++;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (mode == 2)//**************ccd功能**************
                {
                    if (ccddata_num != 0)
                    {
                        comm.Read(Received_bytes, 0, n);//读取缓冲数据到 buf_mode1，n位要读的字节数
                        for (int i = 0; i < n; i++)
                        {
                            if (Received_bytes[i] != 255)//不是255写入print_data,相当于‘T’
                            {
                                if (print_data_count > ccddata_num + 1)
                                    print_data_count = 0;
                                print_data[print_data_count] = Received_bytes[i];
                                print_data_count++;
                            }
                            else
                            {
                                if ((print_data_count == ccddata_num + 1) && print_data[print_data_count - 1] == 0)//相当于‘S’
                                {
                                    //得到了
                                    print_data_count = 0;//计数器清零
                                    for (int j = 0; j < 128; j++)
                                        ccd_data0[j] = print_data[j];
                                    ccd0_print();
                                    ccd0_count++;
                                    this.Invoke((EventHandler)(delegate
                                    {
                                        label32.Text = "帧计数：" + ccd0_count.ToString();
                                    }));
                                    if (ccddata_num == 256)
                                    {
                                        for (int j = 0; j < 128; j++)
                                            ccd_data1[j] = print_data[j + 128];
                                        ccd1_print();
                                        ccd1_count++;
                                        this.Invoke((EventHandler)(delegate
                                        {   
                                            label33.Text = "帧计数：" + ccd1_count.ToString();
                                        }));
                                    }

                                }
                                else
                                {
                                    if ((print_data_count >= 1 && print_data_count < ccddata_num + 1) && print_data[print_data_count - 1] == 0)//相当于‘S’
                                        print_data_count = 0;
                                    else if (print_data_count > ccddata_num + 1)  //以T打头时候，数据过多，清零
                                        print_data_count = 0;
                                    else
                                    {   //数据中掺杂'T'的时候
                                        print_data[print_data_count] = Received_bytes[i];
                                        print_data_count++;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (mode == 4)//**************摄像头显示功能**************
                {
                    comm.Read(Received_bytes, 0, n);//读取缓冲数据到 buf_mode1，n位要读的字节数
                    for (int i = 0; i < n; i++)
                    {
                        if (Received_bytes[i] == 255)    
                        {
                            count_x = 0; 
                            count_y = 0;
                            if (camera_start_flag == false)
                            {
                                this.Invoke((EventHandler)(delegate
                                {
                                    gph_ov7620.Clear(Color.White);//用于清空
                                    label37.Text = "正在采集...";
                                }));
                                camera_start_flag = true;
                                camera_receive_count = 0;
                                continue;
                            }
                            else
                            {
                                camera_receive_count++;
                                this.Invoke((EventHandler)(delegate
                                {
                                    label39.Text = camera_receive_count.ToString();
                                    if (checkBox12.Checked == true)
                                        pictureBox9.Image.Save("camera_Image/camera_Image" + camera_receive_count.ToString() + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp); ;

                                }));
                                if (checkBox10.Enabled == true)
                                {
                                    if (camera_receive_count >= 1)
                                    {
                                        this.Invoke((EventHandler)(delegate
                                        {
                                            Closing_new = true;
                                            while (Listening) Application.DoEvents();
                                            label38.Text = "0";
                                            label37.Text = "欢迎使用！！！";
                                            button91.Enabled = true;
                                            comm.Close();
                                        }));
                                    }
                                }
                            }
                        }
                        else if (camera_start_flag == true)
                        {
                            count_x++;
                            gph_ov7620.DrawEllipse(new Pen(Color.FromArgb(Received_bytes[i], Received_bytes[i], Received_bytes[i])), count_x, count_y, 1, 1);
                            if (count_x >= receive_X)
                            {
                                count_x = 0;
                                count_y++;
                                if (count_y >= receive_Y)
                                    count_y = 0;
                                this.Invoke((EventHandler)(delegate
                                {
                                    label38.Text =  count_y.ToString();
                                    pictureBox9.Refresh();
                                }));
                            }
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
            if (max_zGraph_num==1)
                zGraph_x1.Add(zGraph_timerDrawI);
            else if (max_zGraph_num == 2)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
            }
            else if (max_zGraph_num == 3)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
                zGraph_x3.Add(zGraph_timerDrawI);
            }
            else if (max_zGraph_num == 4)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
                zGraph_x3.Add(zGraph_timerDrawI);
                zGraph_x4.Add(zGraph_timerDrawI);
            }
            else if (max_zGraph_num == 5)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
                zGraph_x3.Add(zGraph_timerDrawI);
                zGraph_x4.Add(zGraph_timerDrawI);
                zGraph_x5.Add(zGraph_timerDrawI);
            }
            else if (max_zGraph_num == 6)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
                zGraph_x3.Add(zGraph_timerDrawI);
                zGraph_x4.Add(zGraph_timerDrawI);
                zGraph_x5.Add(zGraph_timerDrawI);
                zGraph_x6.Add(zGraph_timerDrawI);
            }
            else if (max_zGraph_num == 7)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
                zGraph_x3.Add(zGraph_timerDrawI);
                zGraph_x4.Add(zGraph_timerDrawI);
                zGraph_x5.Add(zGraph_timerDrawI);
                zGraph_x6.Add(zGraph_timerDrawI);
                zGraph_x7.Add(zGraph_timerDrawI);
            }
            else if (max_zGraph_num == 8)
            {
                zGraph_x1.Add(zGraph_timerDrawI);
                zGraph_x2.Add(zGraph_timerDrawI);
                zGraph_x3.Add(zGraph_timerDrawI);
                zGraph_x4.Add(zGraph_timerDrawI);
                zGraph_x5.Add(zGraph_timerDrawI);
                zGraph_x6.Add(zGraph_timerDrawI);
                zGraph_x7.Add(zGraph_timerDrawI);
                zGraph_x8.Add(zGraph_timerDrawI);
            }
            zGraph_timerDrawI++;
        }

        private void zGraph_yRemoveAt()
        {
            //画图框1
            if (zGraph0_num == 1)
            {
                zGraph0_y1.RemoveAt(0);
            }
            else if (zGraph0_num == 2)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
            }
            else if (zGraph0_num == 3)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
                zGraph0_y3.RemoveAt(0);
            }
            else if (zGraph0_num == 4)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
                zGraph0_y3.RemoveAt(0);
                zGraph0_y4.RemoveAt(0);
            }
            else if (zGraph0_num == 5)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
                zGraph0_y3.RemoveAt(0);
                zGraph0_y4.RemoveAt(0);
                zGraph0_y5.RemoveAt(0);
            }
            else if (zGraph0_num == 6)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
                zGraph0_y3.RemoveAt(0);
                zGraph0_y4.RemoveAt(0);
                zGraph0_y5.RemoveAt(0);
                zGraph0_y6.RemoveAt(0);
            }
            else if (zGraph0_num == 7)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
                zGraph0_y3.RemoveAt(0);
                zGraph0_y4.RemoveAt(0);
                zGraph0_y5.RemoveAt(0);
                zGraph0_y6.RemoveAt(0);
                zGraph0_y7.RemoveAt(0);
            }
            else if (zGraph0_num == 8)
            {
                zGraph0_y1.RemoveAt(0);
                zGraph0_y2.RemoveAt(0);
                zGraph0_y3.RemoveAt(0);
                zGraph0_y4.RemoveAt(0);
                zGraph0_y5.RemoveAt(0);
                zGraph0_y6.RemoveAt(0);
                zGraph0_y7.RemoveAt(0);
                zGraph0_y8.RemoveAt(0);
            }
            //画图框1
            if (zGraph1_num == 1)
            {
                zGraph1_y1.RemoveAt(0);
            }
            else if (zGraph1_num == 2)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
            }
            else if (zGraph1_num == 3)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
                zGraph1_y3.RemoveAt(0);
            }
            else if (zGraph1_num == 4)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
                zGraph1_y3.RemoveAt(0);
                zGraph1_y4.RemoveAt(0);
            }
            else if (zGraph1_num == 5)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
                zGraph1_y3.RemoveAt(0);
                zGraph1_y4.RemoveAt(0);
                zGraph1_y5.RemoveAt(0);
            }
            else if (zGraph1_num == 6)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
                zGraph1_y3.RemoveAt(0);
                zGraph1_y4.RemoveAt(0);
                zGraph1_y5.RemoveAt(0);
                zGraph1_y6.RemoveAt(0);
            }
            else if (zGraph1_num == 7)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
                zGraph1_y3.RemoveAt(0);
                zGraph1_y4.RemoveAt(0);
                zGraph1_y5.RemoveAt(0);
                zGraph1_y6.RemoveAt(0);
                zGraph1_y7.RemoveAt(0);
            }
            else if (zGraph1_num == 8)
            {
                zGraph1_y1.RemoveAt(0);
                zGraph1_y2.RemoveAt(0);
                zGraph1_y3.RemoveAt(0);
                zGraph1_y4.RemoveAt(0);
                zGraph1_y5.RemoveAt(0);
                zGraph1_y6.RemoveAt(0);
                zGraph1_y7.RemoveAt(0);
                zGraph1_y8.RemoveAt(0);
            }
            //画图框2
            if (zGraph2_num == 1)
            {
                zGraph2_y1.RemoveAt(0);
            }
            else if (zGraph2_num == 2)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
            }
            else if (zGraph2_num == 3)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
                zGraph2_y3.RemoveAt(0);
            }
            else if (zGraph2_num == 4)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
                zGraph2_y3.RemoveAt(0);
                zGraph2_y4.RemoveAt(0);
            }
            else if (zGraph2_num == 5)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
                zGraph2_y3.RemoveAt(0);
                zGraph2_y4.RemoveAt(0);
                zGraph2_y5.RemoveAt(0);
            }
            else if (zGraph2_num == 6)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
                zGraph2_y3.RemoveAt(0);
                zGraph2_y4.RemoveAt(0);
                zGraph2_y5.RemoveAt(0);
                zGraph2_y6.RemoveAt(0);
            }
            else if (zGraph2_num == 7)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
                zGraph2_y3.RemoveAt(0);
                zGraph2_y4.RemoveAt(0);
                zGraph2_y5.RemoveAt(0);
                zGraph2_y6.RemoveAt(0);
                zGraph2_y7.RemoveAt(0);
            }
            else if (zGraph2_num == 8)
            {
                zGraph2_y1.RemoveAt(0);
                zGraph2_y2.RemoveAt(0);
                zGraph2_y3.RemoveAt(0);
                zGraph2_y4.RemoveAt(0);
                zGraph2_y5.RemoveAt(0);
                zGraph2_y6.RemoveAt(0);
                zGraph2_y7.RemoveAt(0);
                zGraph2_y8.RemoveAt(0);
            }
        }


        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void cbComList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbBaudRateList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            
        }
        private void picComState_Paint(object sender, PaintEventArgs e)
        {

        }

        private void picComState_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

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
                }
                else if (mode == 2)
                {
                    comboBox1.Enabled = true;
                }
                else if(mode == 4)
                {
                    label38.Text = "0" ;
                    label37.Text = "欢迎使用！！！";
                    button91.Enabled = true;
                }
                
                //打开时点击，则关闭串口  
                comm.Close();
            }
            else
            {
                //没有串口的情况下
                if (cbComList.Text == "")
                {
                    MessageBox.Show("大哥，看看有没有串口啊!!!");
                    return;
                }
                
                
                //模式1，串口示波器功能
                if (mode == 1)
                {
                    time_count = 0;
                    button数据显示模拟7.Enabled = false;
                    textBox数值.ReadOnly = true;
                    this.Focus();
                    Graph_init();//初始化3个画图框
                    //开始TIMER
                    f_timerDrawStart();
                }
                else if(mode == 2 )
                {
                    comboBox1.Enabled = false;
                    ccddata_num = int.Parse(comboBox1.Text) * 128;
                    ccd0_count = 0;
                    ccd1_count = 0;
                }
                else if (mode == 4)
                {
                    camera_start_flag = false;
                    count_x = 0;
                    count_y = 0;
                    receive_X = int.Parse(textBox8.Text);
                    receive_Y = int.Parse(textBox7.Text);
                    //初始化图形
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
                    //捕获到异常信息，创建一个新的comm对象，之前的不能用了。  
                    comm = new SerialPort();
                    //现实异常信息给客户。  
                    MessageBox.Show(ex.Message);
                }
            }
            //设置按钮的状态  
            btnComOpen.Text = comm.IsOpen ? "关闭端口" : "打开端口";
            cbComList.Enabled = !comm.IsOpen;
            cbBaudRateList.Enabled = !comm.IsOpen;
        }

        private void Graph_init()
        {
            //读入zGraph0_current
            if (int.TryParse(textBox附加参数.Text.ToString(), out zGraph_current))
            {
                if (zGraph_current > 25 && zGraph_current < 300)
                {
                    timerDraw.Interval = zGraph_current;
                }
                else
                {
                    textBox附加参数.Text = "50";
                }
            }
            else
            {
                textBox附加参数.Text = "50";
            }
            //读入zGraph0_numLimits
            if (int.TryParse(textBox数值.Text.ToString(), out zGraph_numLimits))
            {
                if (zGraph_numLimits < 50 || zGraph_numLimits > 10000)
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
            //图片数目的采集
            zGraph0_num = int.Parse(comboBox图片0曲线数目.Text);
            zGraph1_num = int.Parse(comboBox图片1曲线数目.Text);
            zGraph2_num = int.Parse(comboBox图片2曲线数目.Text);
            if (zGraph1_num > zGraph0_num)
                max_zGraph_num = zGraph1_num;
            else
                max_zGraph_num = zGraph0_num;
            if (max_zGraph_num < zGraph2_num)
                max_zGraph_num = zGraph2_num;
            //清除x轴
            zGraph_x1.Clear();
            zGraph_x2.Clear();
            zGraph_x3.Clear();
            zGraph_x4.Clear();
            zGraph_x5.Clear();
            zGraph_x6.Clear();
            zGraph_x7.Clear();
            zGraph_x8.Clear();

            //初始化zGraph0
            zGraph0_y1.Clear();
            zGraph0_y2.Clear();
            zGraph0_y3.Clear();
            zGraph0_y4.Clear();
            zGraph0_y5.Clear();
            zGraph0_y6.Clear();
            zGraph0_y7.Clear();
            zGraph0_y8.Clear();
            zGraph0.f_ClearAllPix();
            zGraph0.f_reXY();
            if (zGraph0_num == 1)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
            }
            else if (zGraph0_num == 2)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
            }
            else if (zGraph0_num == 3)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
                zGraph0.f_AddPix(ref zGraph_x3, ref zGraph0_y3, Color.FromArgb(0, 128, 192), 2);
            }
            else if (zGraph0_num == 4)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
                zGraph0.f_AddPix(ref zGraph_x3, ref zGraph0_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph0.f_AddPix(ref zGraph_x4, ref zGraph0_y4, Color.Yellow, 3);
            }
            else if (zGraph0_num == 5)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
                zGraph0.f_AddPix(ref zGraph_x3, ref zGraph0_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph0.f_AddPix(ref zGraph_x4, ref zGraph0_y4, Color.Yellow, 3);
                zGraph0.f_AddPix(ref zGraph_x5, ref zGraph0_y5, Color.Cyan, 3);
            }
            else if (zGraph0_num == 6)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
                zGraph0.f_AddPix(ref zGraph_x3, ref zGraph0_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph0.f_AddPix(ref zGraph_x4, ref zGraph0_y4, Color.Yellow, 3);
                zGraph0.f_AddPix(ref zGraph_x5, ref zGraph0_y5, Color.Cyan, 3);
                zGraph0.f_AddPix(ref zGraph_x6, ref zGraph0_y6, Color.Green, 3);
            }
            else if (zGraph0_num == 7)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
                zGraph0.f_AddPix(ref zGraph_x3, ref zGraph0_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph0.f_AddPix(ref zGraph_x4, ref zGraph0_y4, Color.Yellow, 3);
                zGraph0.f_AddPix(ref zGraph_x5, ref zGraph0_y5, Color.Cyan, 3);
                zGraph0.f_AddPix(ref zGraph_x6, ref zGraph0_y6, Color.Green, 3);
                zGraph0.f_AddPix(ref zGraph_x7, ref zGraph0_y7, Color.MediumVioletRed, 1);
            }
            else if (zGraph0_num == 8)
            {
                zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2);
                zGraph0.f_AddPix(ref zGraph_x2, ref zGraph0_y2, Color.Blue, 2);
                zGraph0.f_AddPix(ref zGraph_x3, ref zGraph0_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph0.f_AddPix(ref zGraph_x4, ref zGraph0_y4, Color.Yellow, 3);
                zGraph0.f_AddPix(ref zGraph_x5, ref zGraph0_y5, Color.Cyan, 3);
                zGraph0.f_AddPix(ref zGraph_x6, ref zGraph0_y6, Color.Green, 3);
                zGraph0.f_AddPix(ref zGraph_x7, ref zGraph0_y7, Color.MediumVioletRed, 2);
                zGraph0.f_AddPix(ref zGraph_x8, ref zGraph0_y8, Color.Orange, 2);
            }


            //初始化zGraph1
            zGraph1_y1.Clear();
            zGraph1_y2.Clear();
            zGraph1_y3.Clear();
            zGraph1_y4.Clear();
            zGraph1_y5.Clear();
            zGraph1_y6.Clear();
            zGraph1_y7.Clear();
            zGraph1_y8.Clear();
            zGraph1.f_ClearAllPix();
            zGraph1.f_reXY();
            if (zGraph1_num == 1)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
            }
            else if (zGraph1_num == 2)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
            }
            else if (zGraph1_num == 3)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
                zGraph1.f_AddPix(ref zGraph_x3, ref zGraph1_y3, Color.FromArgb(0, 128, 192), 2);
            }
            else if (zGraph1_num == 4)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
                zGraph1.f_AddPix(ref zGraph_x3, ref zGraph1_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph1.f_AddPix(ref zGraph_x4, ref zGraph1_y4, Color.Yellow, 3);
            }
            else if (zGraph1_num == 5)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
                zGraph1.f_AddPix(ref zGraph_x3, ref zGraph1_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph1.f_AddPix(ref zGraph_x4, ref zGraph1_y4, Color.Yellow, 3);
                zGraph1.f_AddPix(ref zGraph_x5, ref zGraph1_y5, Color.Cyan, 3);
            }
            else if (zGraph1_num == 6)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
                zGraph1.f_AddPix(ref zGraph_x3, ref zGraph1_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph1.f_AddPix(ref zGraph_x4, ref zGraph1_y4, Color.Yellow, 3);
                zGraph1.f_AddPix(ref zGraph_x5, ref zGraph1_y5, Color.Cyan, 3);
                zGraph1.f_AddPix(ref zGraph_x6, ref zGraph1_y6, Color.Green, 3);
            }
            else if (zGraph1_num == 7)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
                zGraph1.f_AddPix(ref zGraph_x3, ref zGraph1_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph1.f_AddPix(ref zGraph_x4, ref zGraph1_y4, Color.Yellow, 3);
                zGraph1.f_AddPix(ref zGraph_x5, ref zGraph1_y5, Color.Cyan, 3);
                zGraph1.f_AddPix(ref zGraph_x6, ref zGraph1_y6, Color.Green, 3);
                zGraph1.f_AddPix(ref zGraph_x7, ref zGraph1_y7, Color.MediumVioletRed, 2);
            }
            else if (zGraph1_num == 8)
            {
                zGraph1.f_LoadOnePix(ref zGraph_x1, ref zGraph1_y1, Color.Red, 2);
                zGraph1.f_AddPix(ref zGraph_x2, ref zGraph1_y2, Color.Blue, 2);
                zGraph1.f_AddPix(ref zGraph_x3, ref zGraph1_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph1.f_AddPix(ref zGraph_x4, ref zGraph1_y4, Color.Yellow, 3);
                zGraph1.f_AddPix(ref zGraph_x5, ref zGraph1_y5, Color.Cyan, 3);
                zGraph1.f_AddPix(ref zGraph_x6, ref zGraph1_y6, Color.Green, 3);
                zGraph1.f_AddPix(ref zGraph_x7, ref zGraph1_y7, Color.MediumVioletRed, 2);
                zGraph1.f_AddPix(ref zGraph_x8, ref zGraph1_y8, Color.Orange, 2);
            }
            //初始化zGraph2
            zGraph2_y1.Clear();
            zGraph2_y2.Clear();
            zGraph2_y3.Clear();
            zGraph2_y4.Clear();
            zGraph2_y5.Clear();
            zGraph2_y6.Clear();
            zGraph2_y7.Clear();
            zGraph2_y8.Clear();
            zGraph2.f_ClearAllPix();
            zGraph2.f_reXY();
            if (zGraph2_num == 1)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
            }
            else if (zGraph2_num == 2)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
            }
            else if (zGraph2_num == 3)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
                zGraph2.f_AddPix(ref zGraph_x3, ref zGraph2_y3, Color.FromArgb(0, 128, 192), 2);
            }
            else if (zGraph2_num == 4)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
                zGraph2.f_AddPix(ref zGraph_x3, ref zGraph2_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph2.f_AddPix(ref zGraph_x4, ref zGraph2_y4, Color.Yellow, 3);
            }
            else if (zGraph2_num == 5)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
                zGraph2.f_AddPix(ref zGraph_x3, ref zGraph2_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph2.f_AddPix(ref zGraph_x4, ref zGraph2_y4, Color.Yellow, 3);
                zGraph2.f_AddPix(ref zGraph_x5, ref zGraph2_y5, Color.Cyan, 3);
            }
            else if (zGraph2_num == 6)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
                zGraph2.f_AddPix(ref zGraph_x3, ref zGraph2_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph2.f_AddPix(ref zGraph_x4, ref zGraph2_y4, Color.Yellow, 3);
                zGraph2.f_AddPix(ref zGraph_x5, ref zGraph2_y5, Color.Cyan, 3);
                zGraph2.f_AddPix(ref zGraph_x6, ref zGraph2_y6, Color.Green, 3);
            }
            else if (zGraph2_num == 7)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
                zGraph2.f_AddPix(ref zGraph_x3, ref zGraph2_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph2.f_AddPix(ref zGraph_x4, ref zGraph2_y4, Color.Yellow, 3);
                zGraph2.f_AddPix(ref zGraph_x5, ref zGraph2_y5, Color.Cyan, 3);
                zGraph2.f_AddPix(ref zGraph_x6, ref zGraph2_y6, Color.Green, 3);
                zGraph2.f_AddPix(ref zGraph_x7, ref zGraph2_y7, Color.MediumVioletRed, 2);
            }
            else if (zGraph2_num == 8)
            {
                zGraph2.f_LoadOnePix(ref zGraph_x1, ref zGraph2_y1, Color.Red, 2);
                zGraph2.f_AddPix(ref zGraph_x2, ref zGraph2_y2, Color.Blue, 2);
                zGraph2.f_AddPix(ref zGraph_x3, ref zGraph2_y3, Color.FromArgb(0, 128, 192), 2);
                zGraph2.f_AddPix(ref zGraph_x4, ref zGraph2_y4, Color.Yellow, 3);
                zGraph2.f_AddPix(ref zGraph_x5, ref zGraph2_y5, Color.Cyan, 3);
                zGraph2.f_AddPix(ref zGraph_x6, ref zGraph2_y6, Color.Green, 3);
                zGraph2.f_AddPix(ref zGraph_x7, ref zGraph2_y7, Color.MediumVioletRed, 2);
                zGraph2.f_AddPix(ref zGraph_x8, ref zGraph2_y8, Color.Orange, 2);
            }
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
                        s = a.Substring(0, n-1)+"\r\n";
                    n = n + 1;
                    comm.Write(s);
                }
            }
            send_count += n;//累加发送字节数  
            label9.Text = "已发送:" + send_count.ToString();//更新界面  
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            send_count = 0;
            label9.Text = "已发送:0";
            richTextBox2.Text = "";
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
 

        
        //tab变化
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            mode = tabControl1.SelectedIndex;
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

        private void textBox1_TextChanged_2(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen)
                return;
            comm.Write("AT+NAME" + textBox2.Text + "\r\n");
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            
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

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }




        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void zGraphTest_Load(object sender, EventArgs e)
        {

        }


        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        
        /******************* timerDraw *******************/
        private void timer1_Tick(object sender, EventArgs e)
        {
            ///TIME增加数据
           /*
            x1.Add(timerDrawI);
            y1.Add(timerDrawI % 100);
            x2.Add(timerDrawI);
            y2.Add((float)Math.Sin(timerDrawI / 10f) * 200);
            x3.Add(timerDrawI);
            y3.Add(50);
            x4.Add(timerDrawI);
            y4.Add((float)Math.Sin(timerDrawI / 10) * 200);*/

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


        private void button标题字体大小_Click(object sender, EventArgs e)
        {
            button标题字体大小.Enabled = false;
            this.Focus();
            int current;
            if (int.TryParse(textBox标题字体大小.Text.ToString(), out current))
            {
                if (current >= 9 && current <= 19)
                {
                    zGraph0.m_titleSize = current;
                    zGraph0.Refresh();
                    zGraph1.m_titleSize = current;
                    zGraph1.Refresh();
                    zGraph2.m_titleSize = current;
                    zGraph2.Refresh();
                }
                else
                {
                    textBox标题字体大小.Text = zGraph0.m_titleSize.ToString();
                }
            }
            else
            {
                textBox标题字体大小.Text = zGraph0.m_titleSize.ToString();
            }
            button标题字体大小.Enabled = true;

        }

        private void button标题位置_Click(object sender, EventArgs e)
        {
            button标题位置.Enabled = false;
            this.Focus();
            float current;
            if (float.TryParse(textBox标题位置.Text.ToString(), out current))
            {
                if (current > 0 && current < 1)
                {
                    zGraph0.m_titlePosition = current;
                    zGraph0.Refresh();
                    zGraph1.m_titlePosition = current;
                    zGraph1.Refresh();
                    zGraph2.m_titlePosition = current;
                    zGraph2.Refresh();
                }
                else
                {
                    textBox标题位置.Text = zGraph0.m_titlePosition.ToString();
                }
            }
            else
            {
                textBox标题位置.Text = zGraph0.m_titlePosition.ToString();
            }
            button标题位置.Enabled = true;
        }

        private void button标题颜色_Click(object sender, EventArgs e)
        {
            button标题颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_titleColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_titleColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_titleColor = my.Color;
                zGraph2.Refresh();
                button标题颜色.BackColor = zGraph0.m_titleColor;
            }
            button标题颜色.Enabled = true;
        }

        private void button标题描边颜色_Click(object sender, EventArgs e)
        {
            button标题描边颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_titleBorderColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_titleBorderColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_titleBorderColor = my.Color;
                zGraph2.Refresh();
                button标题描边颜色.BackColor = zGraph0.m_titleBorderColor;
            }
            button标题描边颜色.Enabled = true;
        }

        private void button背景色渐进起始颜色_Click(object sender, EventArgs e)
        {
            button背景色渐进起始颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_backColorL = my.Color;
                zGraph0.Refresh();
                zGraph1.m_backColorL = my.Color;
                zGraph1.Refresh();
                zGraph2.m_backColorL = my.Color;
                zGraph2.Refresh();
                button背景色渐进起始颜色.BackColor = zGraph0.m_backColorL;
            }
            button背景色渐进起始颜色.Enabled = true;
        }

        private void button背景色渐进终止颜色_Click(object sender, EventArgs e)
        {
            button背景色渐进终止颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_backColorH = my.Color;
                zGraph0.Refresh();
                zGraph1.m_backColorH = my.Color;
                zGraph1.Refresh();
                zGraph2.m_backColorH = my.Color;
                zGraph2.Refresh();
                button背景色渐进终止颜色.BackColor = zGraph0.m_backColorH;
            }
            button背景色渐进终止颜色.Enabled = true;
        }

        private void button坐标线颜色_Click(object sender, EventArgs e)
        {
            button坐标线颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_coordinateLineColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_coordinateLineColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_coordinateLineColor = my.Color;
                zGraph2.Refresh();
                button坐标线颜色.BackColor = zGraph0.m_coordinateLineColor;
            }
            button坐标线颜色.Enabled = true;
        }

        private void button坐标值颜色_Click(object sender, EventArgs e)
        {
            button坐标值颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_coordinateStringColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_coordinateStringColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_coordinateStringColor = my.Color;
                zGraph2.Refresh();
                button坐标值颜色.BackColor = zGraph0.m_coordinateStringColor;
            }
            button坐标值颜色.Enabled = true;
        }

        private void button坐标标题颜色_Click(object sender, EventArgs e)
        {
            button坐标标题颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_coordinateStringTitleColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_coordinateStringTitleColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_coordinateStringTitleColor = my.Color;
                zGraph2.Refresh();
                button坐标标题颜色.BackColor = zGraph0.m_coordinateStringTitleColor;
            }
            button坐标标题颜色.Enabled = true;
        }

        private void button网络线的透明度_Click(object sender, EventArgs e)
        {
            button网络线的透明度.Enabled = false;
            this.Focus();
            int current;
            if (int.TryParse(textBox网络线的透明度.Text.ToString(), out current))
            {
                if (current > 10 && current < 256)
                {
                    zGraph0.m_iLineShowColorAlpha = current;
                    zGraph0.Refresh();
                    zGraph1.m_iLineShowColorAlpha = current;
                    zGraph1.Refresh();
                    zGraph2.m_iLineShowColorAlpha = current;
                    zGraph2.Refresh();
                }
                else
                {
                    textBox网络线的透明度.Text = zGraph0.m_iLineShowColorAlpha.ToString();
                }
            }
            else
            {
                textBox网络线的透明度.Text = zGraph0.m_iLineShowColorAlpha.ToString();
            }
            button网络线的透明度.Enabled = true;
        }

        private void button网络线的颜色_Click(object sender, EventArgs e)
        {
            button网络线的颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_iLineShowColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_iLineShowColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_iLineShowColor = my.Color;
                zGraph2.Refresh();
                button网络线的颜色.BackColor = zGraph0.m_iLineShowColor;
            }
            button网络线的颜色.Enabled = true;
        }

        private void button波形显示区域背景色_Click(object sender, EventArgs e)
        {
            button波形显示区域背景色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_GraphBackColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_GraphBackColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_GraphBackColor = my.Color;
                zGraph2.Refresh();
                button波形显示区域背景色.BackColor = zGraph0.m_GraphBackColor;
            }
            button波形显示区域背景色.Enabled = true;
        }

        private void button工具栏背景色_Click(object sender, EventArgs e)
        {
            button工具栏背景色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_ControlItemBackColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_ControlItemBackColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_ControlItemBackColor = my.Color;
                zGraph2.Refresh();
                button工具栏背景色.BackColor = zGraph0.m_ControlItemBackColor;
            }
            button工具栏背景色.Enabled = true;
        }

        private void button工具栏按钮背景色_Click(object sender, EventArgs e)
        {
            button工具栏按钮背景色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_ControlButtonBackColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_ControlButtonBackColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_ControlButtonBackColor = my.Color;
                zGraph2.Refresh();
                button工具栏按钮背景色.BackColor = zGraph0.m_ControlButtonBackColor;
            }
            button工具栏按钮背景色.Enabled = true;
        }

        private void button工具栏按钮前景选中颜色_Click(object sender, EventArgs e)
        {
            button工具栏按钮前景选中颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_ControlButtonForeColorL = my.Color;
                zGraph0.Refresh();
                zGraph1.m_ControlButtonForeColorL = my.Color;
                zGraph1.Refresh();
                zGraph2.m_ControlButtonForeColorL = my.Color;
                zGraph2.Refresh();
                button工具栏按钮前景选中颜色.BackColor = zGraph0.m_ControlButtonForeColorL;
            }
            button工具栏按钮前景选中颜色.Enabled = true;
        }

        private void button工具栏按钮前景未选中颜色_Click(object sender, EventArgs e)
        {
            button工具栏按钮前景未选中颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_ControlButtonForeColorH = my.Color;
                zGraph0.Refresh();
                zGraph1.m_ControlButtonForeColorH = my.Color;
                zGraph1.Refresh();
                zGraph2.m_ControlButtonForeColorH = my.Color;
                zGraph2.Refresh();
                button工具栏按钮前景未选中颜色.BackColor = zGraph0.m_ControlButtonForeColorH;
            }
            button工具栏按钮前景未选中颜色.Enabled = true;
        }

        private void button标签说明框背景颜色_Click(object sender, EventArgs e)
        {
            button标签说明框背景颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_DirectionBackColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_DirectionBackColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_DirectionBackColor = my.Color;
                zGraph2.Refresh();
                button标签说明框背景颜色.BackColor = zGraph0.m_DirectionBackColor;
            }
            button标签说明框背景颜色.Enabled = true;
        }

        private void button标签说明框文字颜色_Click(object sender, EventArgs e)
        {
            button标签说明框文字颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_DirectionForeColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_DirectionForeColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_DirectionForeColor = my.Color;
                zGraph2.Refresh();
                button标签说明框文字颜色.BackColor = zGraph0.m_DirectionForeColor;
            }
            button标签说明框文字颜色.Enabled = true;
        }

        private void button放大选取框背景颜色_Click(object sender, EventArgs e)
        {
            button放大选取框背景颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_BigXYBackColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_BigXYBackColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_BigXYBackColor = my.Color;
                zGraph2.Refresh();
                button放大选取框背景颜色.BackColor = zGraph0.m_BigXYBackColor;
            }
            button放大选取框背景颜色.Enabled = true;
        }

        private void button放大选取框按钮背景颜色_Click(object sender, EventArgs e)
        {
            button放大选取框按钮背景颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_BigXYButtonBackColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_BigXYButtonBackColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_BigXYButtonBackColor = my.Color;
                zGraph2.Refresh();
                button放大选取框按钮背景颜色.BackColor = zGraph0.m_BigXYButtonBackColor;
            }
            button放大选取框按钮背景颜色.Enabled = true;
        }

        private void button放大选取框按钮文字颜色_Click(object sender, EventArgs e)
        {
            button放大选取框按钮文字颜色.Enabled = false;
            this.Focus();
            ColorDialog my = new ColorDialog();
            if (DialogResult.OK == my.ShowDialog())
            {
                zGraph0.m_BigXYButtonForeColor = my.Color;
                zGraph0.Refresh();
                zGraph1.m_BigXYButtonForeColor = my.Color;
                zGraph1.Refresh();
                zGraph2.m_BigXYButtonForeColor = my.Color;
                zGraph2.Refresh();
                button放大选取框按钮文字颜色.BackColor = zGraph0.m_BigXYButtonForeColor;
            }
            button放大选取框按钮文字颜色.Enabled = true;
        }

        private void button默认样式_Click(object sender, EventArgs e)
        {
            button默认样式.Enabled = false;
            this.Focus();
            f_saveReadFirst(true);
            zGraph0.Refresh();
            zGraph1.Refresh();
            zGraph2.Refresh();
            button默认样式.Enabled = true;
        }

        private void button参考样式1_Click(object sender, EventArgs e)
        {
            button参考样式1.Enabled = false;
            this.Focus();
            //第一个图片
            zGraph0.m_titleSize = 15;
            zGraph0.m_titlePosition = 0.4f;
            zGraph0.m_titleColor = Color.FromArgb(255, 255, 255);
            zGraph0.m_titleBorderColor = Color.FromArgb(0, 64, 128);
            zGraph0.m_backColorL = Color.FromArgb(0, 0, 0);
            zGraph0.m_backColorH = Color.FromArgb(0, 64, 128);
            zGraph0.m_coordinateLineColor = Color.FromArgb(0, 128, 255);
            zGraph0.m_coordinateStringColor = Color.FromArgb(128, 255, 255);
            zGraph0.m_coordinateStringTitleColor = Color.FromArgb(68, 189, 255);
            zGraph0.m_iLineShowColorAlpha = 200;
            zGraph0.m_iLineShowColor = Color.FromArgb(0, 128, 128);
            zGraph0.m_GraphBackColor = Color.FromArgb(255, 255, 255);
            zGraph0.m_ControlItemBackColor = Color.FromArgb(0, 64, 128);
            zGraph0.m_ControlButtonBackColor = Color.FromArgb(0, 0, 0);
            zGraph0.m_ControlButtonForeColorL = Color.FromArgb(0, 255, 255);
            zGraph0.m_ControlButtonForeColorH = Color.FromArgb(0, 64, 128);
            zGraph0.m_DirectionBackColor = Color.FromArgb(0, 0, 0);
            zGraph0.m_DirectionForeColor = Color.FromArgb(255, 255, 0);
            zGraph0.m_BigXYBackColor = Color.FromArgb(0, 64, 128);
            zGraph0.m_BigXYButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph0.m_BigXYButtonForeColor = Color.FromArgb(255, 255, 0);
            zGraph0.Refresh();
            //第二个图片
            zGraph1.m_titleSize = 15;
            zGraph1.m_titlePosition = 0.4f;
            zGraph1.m_titleColor = Color.FromArgb(255, 255, 255);
            zGraph1.m_titleBorderColor = Color.FromArgb(0, 64, 128);
            zGraph1.m_backColorL = Color.FromArgb(0, 0, 0);
            zGraph1.m_backColorH = Color.FromArgb(0, 64, 128);
            zGraph1.m_coordinateLineColor = Color.FromArgb(0, 128, 255);
            zGraph1.m_coordinateStringColor = Color.FromArgb(128, 255, 255);
            zGraph1.m_coordinateStringTitleColor = Color.FromArgb(68, 189, 255);
            zGraph1.m_iLineShowColorAlpha = 200;
            zGraph1.m_iLineShowColor = Color.FromArgb(0, 128, 128);
            zGraph1.m_GraphBackColor = Color.FromArgb(255, 255, 255);
            zGraph1.m_ControlItemBackColor = Color.FromArgb(0, 64, 128);
            zGraph1.m_ControlButtonBackColor = Color.FromArgb(0, 0, 0);
            zGraph1.m_ControlButtonForeColorL = Color.FromArgb(0, 255, 255);
            zGraph1.m_ControlButtonForeColorH = Color.FromArgb(0, 64, 128);
            zGraph1.m_DirectionBackColor = Color.FromArgb(0, 0, 0);
            zGraph1.m_DirectionForeColor = Color.FromArgb(255, 255, 0);
            zGraph1.m_BigXYBackColor = Color.FromArgb(0, 64, 128);
            zGraph1.m_BigXYButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph1.m_BigXYButtonForeColor = Color.FromArgb(255, 255, 0);
            zGraph1.Refresh();
            //第三个图片
            zGraph2.m_titleSize = 15;
            zGraph2.m_titlePosition = 0.4f;
            zGraph2.m_titleColor = Color.FromArgb(255, 255, 255);
            zGraph2.m_titleBorderColor = Color.FromArgb(0, 64, 128);
            zGraph2.m_backColorL = Color.FromArgb(0, 0, 0);
            zGraph2.m_backColorH = Color.FromArgb(0, 64, 128);
            zGraph2.m_coordinateLineColor = Color.FromArgb(0, 128, 255);
            zGraph2.m_coordinateStringColor = Color.FromArgb(128, 255, 255);
            zGraph2.m_coordinateStringTitleColor = Color.FromArgb(68, 189, 255);
            zGraph2.m_iLineShowColorAlpha = 200;
            zGraph2.m_iLineShowColor = Color.FromArgb(0, 128, 128);
            zGraph2.m_GraphBackColor = Color.FromArgb(255, 255, 255);
            zGraph2.m_ControlItemBackColor = Color.FromArgb(0, 64, 128);
            zGraph2.m_ControlButtonBackColor = Color.FromArgb(0, 0, 0);
            zGraph2.m_ControlButtonForeColorL = Color.FromArgb(0, 255, 255);
            zGraph2.m_ControlButtonForeColorH = Color.FromArgb(0, 64, 128);
            zGraph2.m_DirectionBackColor = Color.FromArgb(0, 0, 0);
            zGraph2.m_DirectionForeColor = Color.FromArgb(255, 255, 0);
            zGraph2.m_BigXYBackColor = Color.FromArgb(0, 64, 128);
            zGraph2.m_BigXYButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph2.m_BigXYButtonForeColor = Color.FromArgb(255, 255, 0);
            zGraph2.Refresh();
            //记录下来
            f_reStyle();
            button参考样式1.Enabled = true;
        }

        private void button参考样式2_Click(object sender, EventArgs e)
        {
            button参考样式2.Enabled = false;
            this.Focus();
            //图片一
            zGraph0.m_titleSize = 14;
            zGraph0.m_titlePosition = 0.4f;
            zGraph0.m_titleColor = Color.FromArgb(255, 255, 255);
            zGraph0.m_titleBorderColor = Color.FromArgb(255, 128, 64);
            zGraph0.m_backColorL = Color.FromArgb(255, 128, 0);
            zGraph0.m_backColorH = Color.FromArgb(255, 255, 0);
            zGraph0.m_coordinateLineColor = Color.FromArgb(255, 255, 128);
            zGraph0.m_coordinateStringColor = Color.FromArgb(255, 255, 128);
            zGraph0.m_coordinateStringTitleColor = Color.FromArgb(255, 255, 255);
            zGraph0.m_iLineShowColorAlpha = 200;
            zGraph0.m_iLineShowColor = Color.FromArgb(255, 128, 0);
            zGraph0.m_GraphBackColor = Color.FromArgb(255, 255, 128);
            zGraph0.m_ControlItemBackColor = Color.FromArgb(255, 255, 0);
            zGraph0.m_ControlButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph0.m_ControlButtonForeColorL = Color.FromArgb(128, 0, 0);
            zGraph0.m_ControlButtonForeColorH = Color.FromArgb(255, 255, 128);
            zGraph0.m_DirectionBackColor = Color.FromArgb(0, 255, 0);
            zGraph0.m_DirectionForeColor = Color.FromArgb(0, 0, 64);
            zGraph0.m_BigXYBackColor = Color.FromArgb(255, 128, 64);
            zGraph0.m_BigXYButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph0.m_BigXYButtonForeColor = Color.FromArgb(255, 255, 0);
            zGraph0.Refresh();
            //图片二
            zGraph1.m_titleSize = 14;
            zGraph1.m_titlePosition = 0.4f;
            zGraph1.m_titleColor = Color.FromArgb(255, 255, 255);
            zGraph1.m_titleBorderColor = Color.FromArgb(255, 128, 64);
            zGraph1.m_backColorL = Color.FromArgb(255, 128, 0);
            zGraph1.m_backColorH = Color.FromArgb(255, 255, 0);
            zGraph1.m_coordinateLineColor = Color.FromArgb(255, 255, 128);
            zGraph1.m_coordinateStringColor = Color.FromArgb(255, 255, 128);
            zGraph1.m_coordinateStringTitleColor = Color.FromArgb(255, 255, 255);
            zGraph1.m_iLineShowColorAlpha = 200;
            zGraph1.m_iLineShowColor = Color.FromArgb(255, 128, 0);
            zGraph1.m_GraphBackColor = Color.FromArgb(255, 255, 128);
            zGraph1.m_ControlItemBackColor = Color.FromArgb(255, 255, 0);
            zGraph1.m_ControlButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph1.m_ControlButtonForeColorL = Color.FromArgb(128, 0, 0);
            zGraph1.m_ControlButtonForeColorH = Color.FromArgb(255, 255, 128);
            zGraph1.m_DirectionBackColor = Color.FromArgb(0, 255, 0);
            zGraph1.m_DirectionForeColor = Color.FromArgb(0, 0, 64);
            zGraph1.m_BigXYBackColor = Color.FromArgb(255, 128, 64);
            zGraph1.m_BigXYButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph1.m_BigXYButtonForeColor = Color.FromArgb(255, 255, 0);
            zGraph1.Refresh();
            //图片三
            zGraph2.m_titleSize = 14;
            zGraph2.m_titlePosition = 0.4f;
            zGraph2.m_titleColor = Color.FromArgb(255, 255, 255);
            zGraph2.m_titleBorderColor = Color.FromArgb(255, 128, 64);
            zGraph2.m_backColorL = Color.FromArgb(255, 128, 0);
            zGraph2.m_backColorH = Color.FromArgb(255, 255, 0);
            zGraph2.m_coordinateLineColor = Color.FromArgb(255, 255, 128);
            zGraph2.m_coordinateStringColor = Color.FromArgb(255, 255, 128);
            zGraph2.m_coordinateStringTitleColor = Color.FromArgb(255, 255, 255);
            zGraph2.m_iLineShowColorAlpha = 200;
            zGraph2.m_iLineShowColor = Color.FromArgb(255, 128, 0);
            zGraph2.m_GraphBackColor = Color.FromArgb(255, 255, 128);
            zGraph2.m_ControlItemBackColor = Color.FromArgb(255, 255, 0);
            zGraph2.m_ControlButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph2.m_ControlButtonForeColorL = Color.FromArgb(128, 0, 0);
            zGraph2.m_ControlButtonForeColorH = Color.FromArgb(255, 255, 128);
            zGraph2.m_DirectionBackColor = Color.FromArgb(0, 255, 0);
            zGraph2.m_DirectionForeColor = Color.FromArgb(0, 0, 64);
            zGraph2.m_BigXYBackColor = Color.FromArgb(255, 128, 64);
            zGraph2.m_BigXYButtonBackColor = Color.FromArgb(255, 128, 0);
            zGraph2.m_BigXYButtonForeColor = Color.FromArgb(255, 255, 0);
            zGraph2.Refresh();
            //记录下来
            f_reStyle();
            button参考样式2.Enabled = true;
        }



        /*********************** timerRandom_Tick ***************************/
        private void timerRandom_Tick(object sender, EventArgs e)
        {
            zGraph_x1.Add(rand.Next(60));
            zGraph0_y1.Add((float)rand.NextDouble());
            zGraph0.f_Refresh();
            //更新按钮显示，表示为正在采样
        }

        private void f_timerRandomStart()
        {
            timerRandom.Start();
            textBox附加参数.ReadOnly = true;
            textBox数值.ReadOnly = true;
        }

        private void f_timerRandomStop()
        {
            timerRandom.Stop();
            textBox附加参数.ReadOnly = false;
            textBox数值.ReadOnly = false;
        }

        //模拟显示。没用
        //private void button数据显示模拟1_Click(object sender, EventArgs e)
        //{
        //    ///-300~num画四条数据
        //    button数据显示模拟1.Enabled = false;
        //    this.Focus();
        //    int num;
        //    textBox附加参数.Text = "";
        //    if (int.TryParse(textBox数值.Text.ToString(), out num))
        //    {
        //        if (num < -10000 || num > 10000)
        //        {
        //            num = 1580;
        //            textBox数值.Text = num.ToString();
        //        }
        //    }
        //    else
        //    {
        //        num = 1580;
        //        textBox数值.Text = num.ToString();
        //    }
        //    x1.Clear();
        //    y1.Clear();
        //    x2.Clear();
        //    y2.Clear();
        //    x3.Clear();
        //    y3.Clear();
        //    x4.Clear();
        //    y4.Clear();
        //    if (num < -300)
        //    {
        //        for (int i = -300; i > num; i--)
        //        {
        //            x1.Add(i);
        //            y1.Add(i % 1000);
        //            x2.Add(i);
        //            y2.Add((float)Math.Sin(i / 100f) * 200);
        //            x3.Add(i);
        //            y3.Add(0);
        //            x4.Add(i);
        //            y4.Add((float)Math.Sin(i / 100) * 200);
        //        }
        //    }
        //    else
        //    {
        //        for (int i = -300; i < num; i++)
        //        {
        //            x1.Add(i);
        //            y1.Add(i % 1000);
        //            x2.Add(i);
        //            y2.Add((float)Math.Sin(i / 100f) * 200);
        //            x3.Add(i);
        //            y3.Add(0);
        //            x4.Add(i);
        //            y4.Add((float)Math.Sin(i / 100) * 200);
        //        }
        //    }
        //    zGraphTest.f_ClearAllPix();
        //    zGraphTest.f_reXY();
        //    zGraphTest.f_LoadOnePix(ref x1, ref y1, Color.Red, 2);
        //    zGraphTest.f_AddPix(ref x2, ref y2, Color.Blue, 4);
        //    zGraphTest.f_AddPix(ref x3, ref y3, Color.FromArgb(0, 128, 192), 2);
        //    zGraphTest.f_AddPix(ref x4, ref y4, Color.Yellow, 4);
        //    zGraphTest.f_Refresh();
        //    button数据显示模拟1.Enabled = true;
        //}


        //private void button数据显示模拟2_Click(object sender, EventArgs e)
        //{
        //    ///画三条数据[点|线|矩形条]
        //    button数据显示模拟2.Enabled = false;
        //    this.Focus();
        //    textBox数值.Text = "";
        //    textBox附加参数.Text = "";
        //    x1.Clear();
        //    y1.Clear();
        //    x2.Clear();
        //    y2.Clear();
        //    x3.Clear();
        //    y3.Clear();
        //    for (int i = 0; i < 18000; i += 1000)
        //    {
        //        x1.Add(i);
        //        y1.Add(i / 4f);
        //        x2.Add(i);
        //        y2.Add(i / 4f);
        //        x3.Add(i);
        //        y3.Add(i / 8f);
        //    }
        //    zGraphTest.f_ClearAllPix();
        //    zGraphTest.f_reXY();
        //    zGraphTest.f_LoadOnePix(ref x1, ref y1, Color.Red, 3);
        //    zGraphTest.f_AddPix(ref x2, ref y2, Color.Yellow, 5, LineJoin.Round, LineCap.Flat, ZhengJuyin.UI.ZGraph.DrawStyle.dot);
        //    zGraphTest.f_AddPix(ref x3, ref y3, Color.FromArgb(0, 128, 192), 12, LineJoin.MiterClipped, LineCap.NoAnchor, ZhengJuyin.UI.ZGraph.DrawStyle.bar);
        //    zGraphTest.f_Refresh();
        //    button数据显示模拟2.Enabled = true;
        //}

        //private void button数据显示模拟3_Click(object sender, EventArgs e)
        //{
        //    ///模拟串口采样显示[周期k]
        //    button数据显示模拟3.Enabled = false;
        //    this.Focus();
        //    textBox数值.Text = "";
        //    int current;
        //    if (int.TryParse(textBox附加参数.Text.ToString(), out current))
        //    {
        //        if (current > 100 && current < 300)
        //        {
        //            timerDraw.Interval = current;
        //        }
        //        else
        //        {
        //            textBox附加参数.Text = "200";
        //        }
        //    }
        //    else
        //    {
        //        textBox附加参数.Text = "200";
        //    }
        //    x1.Clear();
        //    y1.Clear();
        //    x2.Clear();
        //    y2.Clear();
        //    x3.Clear();
        //    y3.Clear();
        //    x4.Clear();
        //    y4.Clear();
        //    zGraphTest.f_ClearAllPix();
        //    zGraphTest.f_reXY();
        //    zGraphTest.f_LoadOnePix(ref x1, ref y1, Color.Red, 2);
        //    zGraphTest.f_AddPix(ref x2, ref y2, Color.Blue, 3);
        //    zGraphTest.f_AddPix(ref x3, ref y3, Color.FromArgb(0, 128, 192), 2);
        //    zGraphTest.f_AddPix(ref x4, ref y4, Color.Yellow, 3);

        //    f_timerDrawStart(); //开始TIMER
        //    //更新按钮显示，表示为正在采样
        //    button数据显示模拟3.Text += " 正在采样";
        //    button数据显示模拟3.TextAlign = ContentAlignment.MiddleLeft;
        //}

        //private void button数据显示模拟5_Click(object sender, EventArgs e)
        //{
        //    ///随机点的显示[周期k]
        //    button数据显示模拟5.Enabled = false;
        //    this.Focus();
        //    textBox数值.Text = "";
        //    int current;
        //    if (int.TryParse(textBox附加参数.Text.ToString(), out current))
        //    {
        //        if (current > 50 && current < 300)
        //        {
        //            timerRandom.Interval = current;
        //        }
        //        else
        //        {
        //            textBox附加参数.Text = "100";
        //        }
        //    }
        //    else
        //    {
        //        textBox附加参数.Text = "100";
        //    }
        //    x1.Clear();
        //    y1.Clear();
        //    zGraphTest.f_ClearAllPix();
        //    zGraphTest.f_reXY();
        //    zGraphTest.f_LoadOnePix(ref x1, ref y1, Color.Red, 2, LineJoin.Round, LineCap.NoAnchor, ZhengJuyin.UI.ZGraph.DrawStyle.dot);

        //    f_timerRandomStart(); //开始TIMER
        //    //更新按钮显示，表示为正在采样
        //    button数据显示模拟5.Text += " 正在采样";
        //    button数据显示模拟5.TextAlign = ContentAlignment.MiddleLeft;
        //}

        //private void button数据显示模拟4_Click(object sender, EventArgs e)
        //{
        //    ///关闭TIMER
        //    button数据显示模拟4.Enabled = false;
        //    this.Focus();
        //    f_timerDrawStop();
        //    button数据显示模拟4.Enabled = true;
        //}

        //private void button数据显示模拟6_Click(object sender, EventArgs e)
        //{
        //    button数据显示模拟6.Enabled = false;
        //    this.Focus();
        //    f_timerRandomStop();
        //    button数据显示模拟6.Enabled = true;
        //}

        private void textBoxY轴名称_TextChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

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

        private void tabControl1_TabIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        //模拟
        private void button数据显示模拟5_Click(object sender, EventArgs e)
        {
            ///随机点的显示[周期k]
            this.Focus();
            textBox数值.Text = "";
            int current;
            if (int.TryParse(textBox附加参数.Text.ToString(), out current))
            {
                if (current > 50 && current < 300)
                {
                    timerRandom.Interval = current;
                }
                else
                {
                    textBox附加参数.Text = "100";
                }
            }
            else
            {
                textBox附加参数.Text = "100";
            }
            zGraph_x1.Clear();
            zGraph0_y1.Clear();
            zGraph0.f_ClearAllPix();
            zGraph0.f_reXY();
            zGraph0.f_LoadOnePix(ref zGraph_x1, ref zGraph0_y1, Color.Red, 2, LineJoin.Round, LineCap.NoAnchor, ZhengJuyin.UI.ZGraph.DrawStyle.dot);
            f_timerRandomStart(); //开始TIMER
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (comm.IsOpen == true)
                e.Cancel = true;
            else
                e.Cancel = false;
        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox3_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void richTextBox3_TextChanged_2(object sender, EventArgs e)
        {

        }

        private void richTextBox3_TextChanged_3(object sender, EventArgs e)
        {

        }

        private void zGraph1_Load(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {

        }

        private void richTextBox使用说明_TextChanged(object sender, EventArgs e)
        {

        }

        private void label8_Click_1(object sender, EventArgs e)
        {

        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            ccd0_print();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            ccd0_print();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            ccd1_print();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            ccd1_print();
        }

        private void label32_Click(object sender, EventArgs e)
        {

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {

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

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void button91_Click(object sender, EventArgs e)
        {
            gph_ov7620.Clear(Color.White);//用于清空 
            pictureBox9.Image = bmap_ov7620;
        }

        private void pictureBox9_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void pictureBox7_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void pictureBox8_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox11.Checked)
                checkBox10.Enabled = false;
            else
            {
                checkBox11.Enabled = false;
                checkBox10.Enabled = true;
                checkBox10.Checked = true;
            }
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox10.Checked)
                checkBox11.Enabled = false;
            else
            {
                checkBox10.Enabled = false;
                checkBox11.Enabled = true;
                checkBox11.Checked = true;
            }

        }
        
    }
}