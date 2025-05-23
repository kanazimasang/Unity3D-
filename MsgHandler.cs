﻿using System;
using System.Collections.Generic;
using System.Globalization;

class MsgHandler
{
    public static void MsgEnter(ClientState c, string msgArgs)
    {
        //解析参数
        //Console.WriteLine("MsgEnter" + msgArgs);
        string[] split = msgArgs.Split(',');
        string desc = split[0];
        float x = float.Parse(split[1]);
        float y = float.Parse(split[2]);
        float z = float.Parse(split[3]);
        float eulY = float.Parse(split[4]);
        //赋值
        c.hp = 100;
        c.x = x;
        c.y = y;
        c.z = z;
        c.eulY = eulY;
        //广播
        string sendStr = "Enter|" + msgArgs;
        foreach (ClientState cs in MainClass.clients.Values)
        {
            //将协议广播给所有的客户端
            MainClass.Send(cs, sendStr);
        }
    }

    public static void MsgList(ClientState c, string msgArgs)
    {
        //Console.WriteLine("MsgList" + msgArgs);
        string sendStr = "List|";
        foreach (ClientState cs in MainClass.clients.Values)
        {
            sendStr += cs.socket.RemoteEndPoint.ToString() + ",";
            sendStr += cs.x.ToString() + ",";
            sendStr += cs.y.ToString() + ",";
            sendStr += cs.z.ToString() + ",";
            sendStr += cs.eulY.ToString() + ",";
            sendStr += cs.hp.ToString() + ",";
        }
        MainClass.Send(c, sendStr);
    }

    public static void MsgMove(ClientState c, string msgArgs) {
        //解析参数
        string[] split = msgArgs.Split(',');
        string desc = split[0];
        float x = float.Parse(split[1]);
        float y = float.Parse(split[2]);
        float z = float.Parse(split[3]);
        //赋值
        c.x = x;
        c.y = y;
        c.z = z;
        //广播
        string sendStr = "Move|" + msgArgs;
        foreach(ClientState cs in MainClass.clients.Values)
        {
            //将信息发送至客户端
            MainClass.Send(cs, sendStr);
        }
    }
}
