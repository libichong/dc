using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace dc
{
    public class SynchronousSocketClient
    {
        public static void Lookup(string strLookUpKey)
        {
            // Data buffer for incoming data.
            byte[] bytes = new byte[1024];

            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // This example uses port 11000 on the local computer.
                IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.
                Socket sender = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.
                    byte[] msg = Encoding.ASCII.GetBytes(strLookUpKey + "<EOF>");

                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);
                    string dataRec = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    string[] dataLines = dataRec.Split(new char[] { '\t' });
                    string jumpFolder = dataLines[0];
                    if (dataLines.Length > 1)
                    {
                        for (int i = 0; i < dataLines.Length; i++)
                        {
                            Console.WriteLine("[ {0} ] {1}", i, dataLines[i]);
                        }
                        Console.Write("Choose the above directory to fast jump [0] : ");
                        var key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Enter || key.KeyChar == '0')
                        {

                        }
                        else if(key.KeyChar == '1')
                        {
                            jumpFolder = dataLines[1];
                        }
                        else if (key.KeyChar == '2' && dataLines.Length > 2)
                        {
                            jumpFolder = dataLines[2];
                        }
                        else if (key.KeyChar == '3' && dataLines.Length > 3)
                        {
                            jumpFolder = dataLines[3];
                        }
                        else if (key.KeyChar == '4' && dataLines.Length > 4)
                        {
                            jumpFolder = dataLines[4];
                        }
                        else
                        {
                            Console.WriteLine("Invalid input!");
                        }
                    }
                    ChangeDir(jumpFolder);
                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void ChangeDir(string path)
        {
            Directory.SetCurrentDirectory(path);
            return;
            var pStartInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
            pStartInfo.RedirectStandardInput = true;
            pStartInfo.RedirectStandardOutput = true;
            pStartInfo.UseShellExecute = false;
            pStartInfo.CreateNoWindow = true;

            var process = Process.Start(pStartInfo);
            if (process != null)
            {
                process.StandardInput.WriteLine("dir");
                process.StandardInput.Close();
            }
        }

        private static string WorkingDirectory = "";
        public static int Main(String[] args)
        {
            WorkingDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            Process[] processes = Process.GetProcessesByName("dchost");
            if (processes.Length == 0)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "dchost.exe";
                startInfo.WorkingDirectory = WorkingDirectory;
                Process.Start(startInfo);
            }
            if (args.Length == 1)
                Lookup(args[0]);
            return 0;
        }
    }
}