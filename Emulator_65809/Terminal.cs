using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Emul809or
{

    public class TerminalConnectionChangedEventArgs : EventArgs
    {
        public int count;
    }

    public class Terminal
    {

        UART uart;
        TcpListener server = null;
        public List<NetworkStream> connections = new List<NetworkStream>();

        public Terminal(UART device)
        {
            uart = device;
            uart.UARTOutChanged += Uart1_UARTOutChanged;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                RunServerThread();
            }).Start();

        }

        private async void Uart1_UARTOutChanged(object sender, UARTOutChangedEventArgs e)
        {
            if (e.DataChanged)
            {
                var C = uart.fetchChar();
                byte[] send_data = new byte[1];
                send_data[0] = (byte)C;
                foreach (var n in connections)
                {
                    await n.WriteAsync(send_data, 0, 1);
                }
            }
        }

        private async void RunServerThread()
        {
            Int32 port = 65000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);

            // Start listening for client requests.
            server.Start();
            try
            {
                while (true)
                    await Accept(await server.AcceptTcpClientAsync());
            }
            finally { server.Stop(); }
        }

        async Task Accept(TcpClient client)
        {
            await Task.Yield();
            try
            {
                using (client)
                using (NetworkStream n = client.GetStream())
                {
                    connections.Add(n);

                    byte[] data = new byte[1024];
                    int bytesRead = 0;

                    data[0] = 255;
                    data[1] = 251;
                    data[2] = 1;
                    data[3] = 255;
                    data[4] = 251;
                    data[5] = 3;
                    data[6] = 255;
                    data[7] = 252;
                    data[8] = 34;
                    n.Write(data, 0, 9);


                    try
                    {
                        while (true)
                        {
                            bytesRead = await n.ReadAsync(data);
                            for (int i = 0; i < bytesRead; i++)
                            {
                                uart.CharIn(data[i]);
                            }
                        }
                    }
                    catch 
                    {
                        connections.Remove(n);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


    }
}
