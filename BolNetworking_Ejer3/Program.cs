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
        int contWinners = 0;
        int port = 31416;
        bool finish = false;
        int timeWait = 20;
        private static readonly object l = new object();
        List<StreamWriter> swClients = new List<StreamWriter>();

        Random random = new Random();
        static void Main(string[] args)
        {
            bool puertoValido = false;
            Program p = new Program();
            int rand = p.random.Next(1, 21);
            while (!puertoValido)
                try
                {
                    IPEndPoint ie = new IPEndPoint(IPAddress.Any, p.port);
                    Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    puertoValido = true;
                    server.Bind(ie);
                    server.Listen(10);
                    Console.WriteLine("Waiting clients");
                    Thread time = new Thread(() => p.ThreadTime(p.swClients));
                    time.Start();
                    while (true)
                    {
                        Socket cliente = server.Accept();
                        Thread hilo = new Thread(() => p.ClientThread(cliente, rand));
                        hilo.Start();
                    }
                }
                catch
                {
                    Console.WriteLine("Invalid port");
                }
            while (!p.finish) { }
            Console.WriteLine("There was " + p.contClients + " players and " + p.contWinners + " guess the number " + rand);
            Console.ReadLine();

        }
        private void ClientThread(Socket client, int rand)
        {
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw = null;
            int num = 0;
            try
            {
                using (ns = new NetworkStream(client))
                {

                    using (sr = new StreamReader(ns))
                    {

                        using (sw = new StreamWriter(ns))
                        {
                            lock (l)
                                contClients++;

                            sw.WriteLine("Introduce a number between 1-20");
                            sw.Flush();

                            while (num < 1)
                            {
                                try
                                {
                                    num = Convert.ToInt32(sr.ReadLine());
                                    if (num < 1 || num > 20)
                                        throw new Exception();
                                    sw.WriteLine("Your number is " + num);
                                }
                                catch (FormatException)
                                {
                                    sw.WriteLine("You should write a number between 1-20");
                                    num = 0;
                                }
                                catch
                                {
                                    sw.WriteLine("You should write a number between 1-20");
                                    num = 0;
                                }
                                sw.Flush();
                            }
                            lock (l)
                                swClients.Add(sw);
                            while (!finish) { }
                            lock (l)
                            {
                                if (contClients >= 2)
                                {
                                    if (rand == num)
                                    {
                                        sw.WriteLine("Congrats, you choose the right number");
                                        contWinners++;
                                    }
                                    else
                                    {
                                        sw.WriteLine("Sorry, you didn't choose the right number");
                                        sw.WriteLine("Your number was " + num);
                                        sw.WriteLine("The number to guess was " + rand);
                                    }
                                    sw.Flush();
                                }
                                else
                                {
                                    sw.WriteLine("There are not enough clients");
                                    sw.Flush();
                                }
                            }
                            client.Close();
                        }
                    }
                }
            }
            catch (IOException)
            {
                lock (l)
                {
                    contClients--;
                    swClients.Remove(sw);
                }
                client.Close();
            }
        }

        private void ThreadTime(List<StreamWriter> swClients)
        {
            while (timeWait > 0)
            {
                Thread.Sleep(1000);
                lock (l)
                {
                    try
                    {
                        foreach (StreamWriter sw in swClients)
                        {
                            try
                            {
                                sw.WriteLine(timeWait + " seconds left to start, there are " + contClients + " players");
                                sw.Flush();
                            }
                            catch (IOException)
                            {
                                lock (l)
                                {
                                    swClients.Remove(sw);
                                    contClients--;
                                }
                            }
                        }
                    }
                    catch (InvalidOperationException) { }
                }
                timeWait--;
            }
            finish = true;
        }
    }
}
