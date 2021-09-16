using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chatRoom
{
    class Program
    {
        static IPAddress localIp;
        static byte[] zipdata;      //文件数据
        static string nowName;      //文件名
        static int port = 8841;
        static int port2 = 8842;
        static int port3 = 8843;
        static List<Socket> socketList = new List<Socket>();
        static List<string> userName = new List<string>();
        static void Main(string[] args)
        {
           // IPAddress[] ip = Dns.GetHostAddresses(Dns.GetHostName());
           // localIp = ip[ip.Length - 1];// IPAddress.Parse(host);
           // Console.WriteLine("检测到本机可用IP：" + localIp.ToString());
           // Console.WriteLine("输入IP：");
            //localIp = IPAddress.Parse(Console.ReadLine());
            localIp = IPAddress.Parse("10.0.4.2");

            //localIp = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipe = new IPEndPoint(localIp, port);
            Socket sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sSocket.Bind(ipe);
            sSocket.Listen(100);
            Console.WriteLine("聊天监听已经打开，请等待");
            /*----------------------启动File-----------------------*/
            Thread thFile = new Thread(listenFile);             //发送文件服务
            thFile.IsBackground = true;
            thFile.Start();
            thFile = new Thread(FileSever);                     //接收文件服务
            thFile.IsBackground = true;
            thFile.Start();
            /*----------------------启动File-----------------------*/
            Thread th1 = new Thread(SendName);                  //发送在线用户
            th1.IsBackground = true;
            th1.Start();

            byte[] recByte = new byte[128];
            int bytes;
            while (true)
            {
                try
                {
                    Socket serverSocket = sSocket.Accept();
                    IPEndPoint clientipe = (IPEndPoint)serverSocket.RemoteEndPoint;

                    byte[] flag = { 0x22, 0x22 };
                    bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                    if (recByte[0] != flag[0] || recByte[1] != flag[1])
                        continue;
                    bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                    userName.Add(Encoding.Unicode.GetString(recByte, 0, bytes));        //用户集合
                    socketList.Add(serverSocket);                                       //线程池
                    Console.WriteLine(clientipe.Address.ToString()+"  " + Encoding.Unicode.GetString(recByte, 0, bytes)+"  "+ "连接已经建立");
                    Thread th = new Thread(new ParameterizedThreadStart(Re));
                    th.IsBackground = true;
                    th.Start(serverSocket);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString()  + "用户连接失败");
                }
            }
        }
        static void listenFile()
        {
            IPEndPoint ipe = new IPEndPoint(localIp, port3);
            Socket fileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            fileSocket.Bind(ipe);
            fileSocket.Listen(50);
            Console.WriteLine("FileSend监听已经打开，请等待");
            while(true)
            {
                try
                {
                    Socket serverSocket = fileSocket.Accept();
                    Thread th = new Thread(new ParameterizedThreadStart(FileSend));
                    th.IsBackground = true;
                    th.Start(serverSocket);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
        static void FileSend(Object s)
        {
            Socket serverSocket = (Socket)s;
            int bytes;
            byte[] recByte = new byte[1024];
            try
            {
       
                serverSocket.Send(Encoding.Unicode.GetBytes(nowName));
                bytes = serverSocket.Receive(recByte,recByte.Length, 0);
                
                if (Encoding.Unicode.GetString(recByte, 0, bytes)== nowName)
                {
                    serverSocket.Send(Encoding.Unicode.GetBytes(zipdata.Length.ToString()));
                    bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                    serverSocket.Send(zipdata);
                }
                else
                    Console.WriteLine("文件名传输失败");
                serverSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        static void FileSever()
        {
            IPEndPoint ipe = new IPEndPoint(localIp, port2);
            Socket fileSeverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            fileSeverSocket.Bind(ipe);
            fileSeverSocket.Listen(20);
            Console.WriteLine("接收文件服务已经打开!");
                
            byte[] recByte = new byte[4096];
            int bytes;
            BinaryWriter Bwfilw;            //文件指针
            while (true)
            {
                try
                {
                    Socket socket = fileSeverSocket.Accept();
                    IPEndPoint clientipe = (IPEndPoint)socket.RemoteEndPoint;
                    /*------------------------------发送协议--------------------------------*/
                    DateTime d = DateTime.Now;                  //查看时间

                    bytes = socket.Receive(recByte, recByte.Length, 0);               //第一次接受名字
                    Console.WriteLine(clientipe.Address.ToString() +"  "+ "发送文件"+Encoding.Unicode.GetString(recByte, 0, bytes));
                    nowName = Encoding.Unicode.GetString(recByte, 0, bytes);            //文件名全局变量
                    try
                    {
                        Bwfilw = new BinaryWriter(new FileStream(nowName, FileMode.Create));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("创建文件失败"+ e.ToString());
                        socket.Close();
                        continue;
                    }
                    socket.Send(recByte, bytes, 0);                     //第二次发送名字验证
                    /*------------------------------发送协议--------------------------------*/
                    do
                    {
                        bytes = socket.Receive(recByte, recByte.Length, 0);
                        Bwfilw.Write(recByte, 0, bytes);
                    }
                    while (bytes > 0);
                    Bwfilw.Close();
                    try
                    {
                        zipdata = File.ReadAllBytes(nowName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString() + "文件读取失败");
                    }
                    sendFileFlag();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString()  + "文件接收失败");
                }
            }
        }
        static void Re(Object s)
        {
            byte[] recByte = new byte[4096];
            int bytes;
            Socket serverSocket = (Socket)s;
            //IPEndPoint clientipe = (IPEndPoint)serverSocket.RemoteEndPoint;
            while (true)
            {
                try
                {
                    bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                }
                catch (Exception)
                {
                    int x = socketList.IndexOf(serverSocket);
                    userName.RemoveAt(x);
                    socketList.Remove(serverSocket);
                    return;
                }
                if (recByte[0]!=0x01 && recByte[1]!=0x01)
                {
                    Socket socket;
                    string str = Encoding.Unicode.GetString(recByte, 0, bytes);
                    for(int i=0;i<socketList.Count;i++)
                    {
                        socket = socketList[i];
                        try
                        {
                            socket.Send(recByte, bytes, 0);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                       
            }
        }


        static void SendName()
        {
            while(true)
            {
                string str = ":9";
                foreach (var user in userName)
                {
                    str += user;
                    str += "\r\n";
                }
                byte[] nameByte = Encoding.Unicode.GetBytes(str);
                Socket socket;
                for (int i=0;i< socketList.Count;i++)
                {
                    socket = socketList[i];
                    try
                    {
                        socket.Send(nameByte, nameByte.Length, 0);
                    }
                    catch (Exception)
                    {
                    }

                }
                //Console.WriteLine(str);
                Thread.Sleep(2000);
            }
        }
        static void sendFileFlag()           //客户准备接收文件
        {
            byte[] flag = { 0x02, 0x02 };
            Socket socket;
            for (int i = 0; i < socketList.Count; i++)
            {
                socket = socketList[i];
                try
                {
                    socket.Send(flag);
                }
                catch (Exception)
                {
                }

            }

        }


    }
}
