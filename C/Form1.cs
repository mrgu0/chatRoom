using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _233聊天室
{
    public partial class Form1 : Form
    {
        static string host;
        static string name;
        static IPAddress ip;
        static int port  = 8841;
        static int port2 = 8842;
        static int port3 = 8843;
        static Form1 mainFrm;

        static string sendString;               //聊天字符串  


        static Socket socket;          //主要连接
        public Form1(string IP,string name1)
        {
            name = name1;
            host = IP;
            mainFrm = this;
            InitializeComponent();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
                System.Environment.Exit(0);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            richTextBox1.AppendText("启动中...\r\n");
            ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipe);
                richTextBox1.AppendText("服务器连接成功...\r\n");

                Thread th = new Thread(Re);                //接收线程
                th.IsBackground = true;

                byte[] flag = { 0x22, 0x22 };
                socket.Send(flag);
                Thread.Sleep(500);
                socket.Send(Encoding.Unicode.GetBytes(name));       //发送名字
                Thread.Sleep(500);
                th.Start();

                Thread.Sleep(1000);
                Thread th1 = new Thread(xintiao);            //心跳包
                th1.IsBackground = true;
                th1.Start();             
            }
            catch (Exception)
            {
                mainFrm.richTextBox1.AppendText("连接失败...\r\n");
            }
        }

        static void xintiao()
        {
            byte[] send = { 0x01, 0x01 };
            while (true)
            {
                try
                {
                    socket.Send(send);
                    Thread.Sleep(3000);
                }
                catch (Exception)
                {
                    mainFrm.richTextBox1.AppendText("心跳包发送失败\r\n");
                }
            }

        }
        private void button1_Click(object sender, EventArgs e)              //发送信息按钮
        {
            sendString = name + ": " + richTextBox2.Text;
            if (richTextBox2.Text!="")
            {
                richTextBox2.Text = "";
                Send();
            }
        }

        static void Send()
        {
            try
            {
                byte[] sendByte = Encoding.Unicode.GetBytes(sendString);
                socket.Send(sendByte);
            }
            catch (Exception )
            {
                mainFrm.richTextBox1.AppendText("发送失败...\r\n");
            }

        }
        static void Re()
        {
            try
            {
                byte[] hello = Encoding.Unicode.GetBytes("\r\n              " + name + " 加入聊天室");
                socket.Send(hello);

                int bytes;
                string recStr;
                byte[] recByte = new byte[4096];
                while (true)
                {
                        bytes = socket.Receive(recByte, recByte.Length, 0);
                        recStr = Encoding.Unicode.GetString(recByte, 0, bytes);
                    if(recByte[0]==0x02&&recByte[1]==0x02)
                    {
                        mainFrm.button2.Enabled = false;
                        IPEndPoint ipe = new IPEndPoint(ip, port3);
                        Socket reFilesocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        
                        reFilesocket.Connect(ipe);
                        Thread th = new Thread(new ParameterizedThreadStart(downFile));
                        th.IsBackground = true;
                        th.Start(reFilesocket);
                    }
                     else if (recStr.Substring(0, 2) == ":9")           //打印在线用户
                        {
                            recStr = recStr.Remove(0, 2);
                            mainFrm.richTextBox3.Text = recStr;
                        }
                        else
                            mainFrm.richTextBox1.AppendText(recStr + "\r\n");
                }
            }
            catch (Exception )
            {
                mainFrm.richTextBox1.AppendText("好友服务器已断开...\r\n");
            }
        }
        static private void downFile(object s)
        {
            BinaryWriter Bwfilw;
            int bytes;
            byte[] recByte = new byte[4096];
            Socket serverSocket = (Socket)s;

            bytes = serverSocket.Receive(recByte, recByte.Length, 0);
            string name = Encoding.Unicode.GetString(recByte, 0, bytes);
            DateTime d = DateTime.Now;
            name = d.ToString("ss-ffff-") +name;
            serverSocket.Send(recByte, bytes,0);
            try
            {
                Bwfilw = new BinaryWriter(new FileStream(name, FileMode.Create));
            }
            catch (Exception )
            {
                MessageBox.Show("接收文件，创建时失败", "提示");
                return;
            }
            mainFrm. progressBar1.Value = 0;         //清空进度条
            try
            {
                bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                serverSocket.Send(recByte, bytes, 0);
                int maxFile = IsNumeric(Encoding.Unicode.GetString(recByte, 0, bytes));
                int count = 0;              //进度计算器
                int start = 0;              //速度计算器
                MessageBox.Show("准备接收文件,"+ maxFile/1024/1024+" Mb", "提示");
                DateTime startTime = DateTime.Now;
                do
                {
                    bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                    count += bytes;
                    start += bytes;
                    Bwfilw.Write(recByte, 0, bytes);
                    float x = (float)count / (float)maxFile;

                    if ( (int)(x * 100) > 100)
                        mainFrm.progressBar1.Value = 100;
                    else
                        mainFrm.progressBar1.Value = (int)(x * 100);
                    if((DateTime.Now - startTime).Seconds>=1)
                    {
                        float a = (float)start  /1024/ 1024;
                        //mainFrm.richTextBox1.AppendText(a.ToString() +" 以传输"+(count/1024/1024) + "\r\n");
                        mainFrm.label2.Text = String.Format("{0:F3}", a)+" Mb/s";
                        start = 0;
                        startTime = DateTime.Now;
                    }
                }
                while (bytes > 0);
                mainFrm.label2.Text = " ";
                mainFrm.progressBar1.Value = 0;         //清空进度条
            }
            catch (Exception ee)
            {
                mainFrm.richTextBox1.AppendText(ee.ToString());
            }
            Bwfilw.Close();             //服务器关闭socket
            MessageBox.Show("接收文件结束", "提示");
            mainFrm.button2.Enabled = true;
        }
        private void button2_Click(object sender, EventArgs e)          //发送文件
        {
            MessageBox.Show("发送后，只有在线用户可以接收文件", "提示");
            string file = "";           //文件名
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.*)|*.*"; //设置要选择的文件的类型
            fileDialog.Multiselect = false; //是否可以多选
            try
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    file = fileDialog.FileName;//返回文件的完整路径
                    sendFile(file);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("文件打开失败", "提示");
            }
        }
        private void sendFile(string file)
        {
            Socket ftpsocket;
            byte[] zipdata;             //文件数组
            byte[] recByte = new byte[4096];

            
            try
            {
                IPEndPoint ipe = new IPEndPoint(ip, port2);
                ftpsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ftpsocket.Connect(ipe);
            }
            catch (Exception)
            {
                MessageBox.Show("文件服务器连接失败", "提示");
                return;
            }

           
            zipdata = File.ReadAllBytes(file);
          
            string name = System.IO.Path.GetFileName(file);
            ftpsocket.Send(Encoding.Unicode.GetBytes(name));               //发送名字
            int bytes = ftpsocket.Receive(recByte, recByte.Length, 0);
            if (Encoding.Unicode.GetString(recByte, 0, bytes) != name)
            {
                MessageBox.Show("网络异常或文件名错误", "提示");
                return;
            }
            try
            {
                MessageBox.Show("开始发送文件", "提示");
                ftpsocket.Send(zipdata);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString() + "无法发送文件", "提示");
            }
            MessageBox.Show("服务器接收文件成功", "提示");
            ftpsocket.Close();
        }
        static public int IsNumeric(string str)
        {
            int i;
            if (str != null && System.Text.RegularExpressions.Regex.IsMatch(str, @"^-?\d+$"))
                i = int.Parse(str);
            else
                i = -1;
            return i;
        }
    }
}
