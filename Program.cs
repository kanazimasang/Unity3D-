using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.Metrics;

class ClientState
{
    public Socket socket;
    public byte[] readbuff = new byte[1024];
    public int hp = -100;
    public float x = 0;
    public float y = 0;
    public float z = 0;
    public float eulY = 0;
}
class MainClass
{
    //监听Socket
    static Socket listenfd;
    //客户端Socket及状态信息
    public static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

    public static void Main(string[] args)
    {
        Console.WriteLine("Hello Echo");
        //Socket
        Socket listenfd = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
        //Bind
        IPAddress ipAdr = IPAddress.Parse("127.0.0.1");
        IPEndPoint ipEd = new IPEndPoint(ipAdr, 8888);
        listenfd.Bind(ipEd);
        //Listen
        listenfd.Listen(0);
        Console.WriteLine("[服务器]启动成功");
        //checkRead
        List<Socket> checkRead = new List<Socket>();
        //等待
        while (true)
        {
            //填充checkRead列表
            checkRead.Clear();
            checkRead.Add(listenfd);
            foreach (ClientState s in clients.Values)
            {
                checkRead.Add(s.socket);
            }
            //select
            Socket.Select(checkRead, null, null, 1000);
            //检查可读对象
            foreach (Socket s in checkRead)
            {
                if (s == listenfd)
                {
                    ReadListenfd(s);
                }
                else
                {
                    ReadClientfd(s);
                }
            }
        }
    }


    //发送信息至客户端
    public static void Send(ClientState client, string message)
    {
        try
        {
            byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(message);
            client.socket.Send(sendBytes);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"向客户端发送失败: {ex.Message}");
            client.socket.Close();
            clients.Remove(client.socket);
        }
    }
    //读取Listenfd
    public static void ReadListenfd(Socket listenfd)
    {
        Console.WriteLine("Accept");
        Socket clientfd = listenfd.Accept();
        ClientState state = new ClientState();
        state.socket = clientfd;
        clients.Add(clientfd, state);
    }

    //读取Client fd
    public static bool ReadClientfd(Socket clientfd)
    {
        ClientState state = clients[clientfd];
        //接受
        int count = 0;

        try
        {
            count = clientfd.Receive(state.readbuff);
        }
        catch (SocketException ex)
        {
            MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
            object[] ob = { state };
            mei.Invoke(null, ob);

            clientfd.Close();
            clients.Remove(clientfd);
            Console.WriteLine("Receive SocketException " + ex.ToString());
            return false;
        }
        //客户端关闭
        if (count <= 0)
        {
            MethodInfo mei = typeof(EventHandler).GetMethod("OnDisconnect");
            object[] ob = { state };
            mei.Invoke(null, ob);

            clientfd.Close();
            clients.Remove(clientfd);
            Console.WriteLine("Socket Close");
            return false;
        }
        //广播
        string recvStr =
            System.Text.Encoding.Default.GetString(state.readbuff, 0, count);
        string[] split = recvStr.Split('|');
        Console.WriteLine("Receive" + recvStr);
        string msgName = split[0];
        string msgArgs = split[1];
        string funName = "Msg" + msgName;
        MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
        object[] o = { state, msgArgs };
        mi.Invoke(null, o);

        return true;
    }

    //Accept回调
    public static void AcceptCallback(IAsyncResult ar)
    {
        try
        {
            Console.WriteLine("[服务器]Accept");
            Socket listenfd = (Socket)ar.AsyncState;
            Socket clientfd = listenfd.EndAccept(ar);
            //clients列表
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd, state);
            //接受数据BeginReceive
            clientfd.BeginReceive(state.readbuff, 0, 1024, 0, ReceiveCallback, state);
            //继续Accept
            listenfd.BeginAccept(AcceptCallback, listenfd);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Socket Accept fail" + ex.ToString());
        }
    }




    //Receive回调
    public static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            ClientState state = (ClientState)ar.AsyncState;
            Socket clientfd = state.socket;
            int count = clientfd.EndReceive(ar);
            //客户端关闭
            if (count == 0)
            {
                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Socket Close");
                return;
            }

            string recvStr =
                System.Text.Encoding.Default.GetString(state.readbuff, 0, count);
            string sendStr =
                clientfd.RemoteEndPoint.ToString() + ":" + recvStr;
            byte[] sendBytes =
                System.Text.Encoding.Default.GetBytes("echo" + recvStr);
            //clientfd.Send(sendBytes);//减少代码量，不用异步
            foreach (ClientState s in clients.Values)
            {
                s.socket.Send(sendBytes);
            }
            clientfd.BeginReceive(state.readbuff, 0, 1024, 0,
                ReceiveCallback, state);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Socket Receive fail" + ex.ToString());
        }
    }
}
