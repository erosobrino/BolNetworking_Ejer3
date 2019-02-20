using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BolNetworking_Ejer3
{
    class Program
    {
        int contClients = 0;
        bool timerStart = false;
        //int port = 135;
        int port = 31416;
        static int time = 10;
        int timeWait = time;
        private static readonly object l = new object();
        private static readonly object lTime = new object();
        List<StreamWriter> swClients = new List<StreamWriter>();
        List<int> numbers = new List<int>();
        Random random = new Random();

        static void Main(string[] args)
        {
            bool puertoValido = false;
            Program p = new Program();
            while (!puertoValido)
            {
                try
                {
                    IPEndPoint ie = new IPEndPoint(IPAddress.Any, p.port);
                    Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    puertoValido = true;
                    server.Bind(ie);
                    server.Listen(10);
                    Console.WriteLine("Waiting clients");
                    while (true)
                    {
                        Socket cliente = server.Accept();
                        int rand = p.random.Next(1, 21);
                        while (p.numbers.Contains(rand))
                        {
                            rand = p.random.Next(1, 21);
                        }
                        Thread hilo = new Thread(() => p.ClientThread(cliente, rand));
                        hilo.IsBackground = true;
                        hilo.Start();
                        p.contClients++;
                        if (p.contClients >= 2 && !p.timerStart)
                        {
                            Thread time = new Thread(() => p.ThreadTime(p));
                            time.Start();
                        }
                    }
                }
                catch (SocketException)
                {
                    Console.WriteLine("Invalid port");
                }
            }
        }

        private void ClientThread(Socket client, int rand)
        {
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw = null;
            try
            {
                using (ns = new NetworkStream(client))
                {

                    using (sr = new StreamReader(ns))
                    {

                        using (sw = new StreamWriter(ns))
                        {
                            lock (l)
                            {
                                numbers.Add(rand);
                                swClients.Add(sw);
                            }
                            sw.WriteLine("Welcome to the highest number game");
                            sw.Flush();
                            lock (lTime)
                                Monitor.Wait(lTime);
                            lock (l)
                            {
                                sw.WriteLine("Your number was " + rand);
                                if (rand == numbers.Max())
                                {
                                    sw.WriteLine("Congrats, you are the winner");
                                }
                                else
                                {
                                    sw.WriteLine("Sorry, you didn't win");
                                    sw.WriteLine("The winning number was " + numbers.Max());
                                }
                                sw.Flush();
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                lock (l)
                {
                    contClients--;
                    numbers.Remove(rand);
                    swClients.Remove(sw);
                }
            }
            lock (lTime)
                Monitor.Pulse(lTime);
            client.Close();
        }

        private void ThreadTime(Program p)
        {
            timerStart = true;
            while (timeWait > 0)
            {
                Thread.Sleep(1000);
                lock (l)
                {
                    for (int i = 0; i < swClients.Count; i++)
                    {
                        try
                        {
                            swClients[i].WriteLine(timeWait + " seconds left to start, there are " + contClients + " players");
                            swClients[i].Flush();
                        }
                        catch (IOException)
                        {
                            lock (l)
                            {
                                swClients.Remove(swClients[i]);
                                numbers.RemoveAt(i);
                                contClients--;
                            }
                        }
                    }
                }
                timeWait--;
            }
            lock (lTime)
            {
                try
                {
                    Console.WriteLine("There was " + contClients + " players and the highest number was " + numbers.Max());
                    for (int i = 0; i < contClients; i++)
                    {
                        Monitor.Pulse(lTime);
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("There was no players");
                }
            }
            Thread.Sleep(1000);
            lock (l)
            {
                swClients.Clear();
                numbers.Clear();
                p.contClients = 0;
                p.timerStart = false;
                timeWait = time;
            }
            Console.WriteLine("Waiting new players");
        }
    }
}
