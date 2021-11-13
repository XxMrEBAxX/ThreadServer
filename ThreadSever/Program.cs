using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace ThreadSever
{
    class Program
    {
        public static List<ClientListener> clientList = new List<ClientListener>();
        static void Main(string[] args)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 13000);
            tcpListener.Start();
            Console.WriteLine("Waiting for clients...");
            while (true)
            {
                while(!tcpListener.Pending())
                    // 연결이 없으면 1초씩 기다림.
                {
                    Thread.Sleep(1000);
                }
                // 요청을 받으면 ClientListener 클래스에 요청 받은 클라이언트를 넘겨줌.
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("connection : {0}", (IPEndPoint)client.Client.RemoteEndPoint);
                ClientListener clientThread = new ClientListener(client);
                clientList.Add(clientThread);
            }
        }
    }
    class ClientListener
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private  NetworkStream networkStream;
        Thread thread;
        public ClientListener(TcpClient client)
        {
            this.client = client;
            networkStream = client.GetStream();
            reader = new StreamReader(networkStream);
            writer = new StreamWriter(networkStream);
            thread = new Thread(Run);
            thread.Start();
            thread.IsBackground = true;
        }
        ~ClientListener()
        {
            client.Close();
            reader.Close();
            writer.Close();
            networkStream.Close();
            thread.Abort();
        }

        private void Run()
        {
            while (true)
            {
                String line = "";
                try
                {
                    line = reader.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    break;
                }
                if (line == "$exit")
                {
                    Program.clientList.Remove(this);
                    Console.WriteLine("{0} : 접속 종료", client.Client.RemoteEndPoint);
                    break;
                }
                if (client.Connected)
                    Console.WriteLine("[연결됨] : {0} : {1}", client.Client.RemoteEndPoint, line);
                foreach (object clientThread in Program.clientList)
                {
                    ClientListener client = (ClientListener)clientThread;
                    if (!client.Equals(this))
                    {
                        StreamWriter writer = client.writer;
                        writer.WriteLine("{0} : {1}", this.client.Client.RemoteEndPoint, line);
                        writer.Flush();
                    }
                }
            }
        }
        public override bool Equals(Object obj)
            //[ Equals 재정의 조건 ]
            //재귀적(Reflexive)이어야 함 :  x.Equals(x)가 true를 반환해야 한다.
            //대칭성(Symmetric)이 있어야함 : x.Equals(y) 와 y.Equals(x) 의 반환값은 같아야 한다.
            //전이성(Transitive) 이 있어야 함 : x.Equals(y) 가 참이고 y.Equals(z)가 참이면 x.Equals(z) 는 참이어야 한다.
            //일관성이 있어야함 : 비교되는 두 값 사이에 변화가 일어나지 않았다면 언제나 같은 값을 반환해야 함.
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                ClientListener other = (ClientListener)obj;
                return (this.client.Client.RemoteEndPoint == other.client.Client.RemoteEndPoint);
            }
        }
        public override int GetHashCode() { return 0; }
        // Equals 재정의 시 반드시 재정의 해줘야 하는 함수.
        // 객체의 생명주기 동안 변치 않는 임의의 고정된 숫자를 하나 반환한다.
    }
}