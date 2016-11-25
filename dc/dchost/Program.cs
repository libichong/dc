using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;

namespace dchost
{
    class Program
    {
        private static bool init = false;
        // Incoming data from the client.
        public static string data = null;
        private static DCDictionary DirDictionary = null;
        private static DCAliasMapping AliasMapping = null;

        public static void StartListening(object ParamPort)
        {
            int Port = Convert.ToInt32(ParamPort);
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                Console.WriteLine(string.Format("dchost is listening on {0}:{1}. Waiting for a connection...", ipAddress.ToString(), Port));
                // Start listening for connections.
                while (true)
                {
                    // Program is suspended while waiting for an incoming connection.
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.
                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            data = data.Substring(0, data.Length - 5);
                            break;
                        }
                    }

                    // Show the data on the console.
                    Console.WriteLine("lookup the directory : {0}", data);
                    string msg = "";
                    if (!init)
                    {
                        msg = "Server is indexing the directories!";
                    }
                    else
                    {
                        string[] tokens = data.Split(new char[] { '\\', '/' });
                        bool found = false;
                        var term = AliasMapping.ContainsKey(tokens[tokens.Length - 1].ToLower()) ? AliasMapping[tokens[tokens.Length - 1].ToLower()] : tokens[tokens.Length - 1].ToLower();
                        if (tokens.Length == 1)
                        {
                            if (DirDictionary.ContainsKey(term))
                            {
                                msg = string.Join("\t", DirDictionary[data.ToLower()].Take(10).ToList());
                                found = true;
                            }
                        }
                        else if (tokens.Length == 2)
                        {
                            if (DirDictionary.ContainsKey(term))
                            {
                                var alias = AliasMapping.ContainsKey(tokens[0].ToLower()) ? AliasMapping[tokens[0].ToLower()] : tokens[0].ToLower();
                                var list = DirDictionary[term].Where(x => x.ToLower().Contains(alias)).ToList();
                                if (list.Count() > 0)
                                {
                                    msg = string.Join("\t", list.Take(10).ToList());
                                    found = true;
                                }
                            }
                        }
                        else if (tokens.Length == 3)
                        {
                            if (DirDictionary.ContainsKey(term.ToLower()))
                            {
                                var alias = AliasMapping.ContainsKey(tokens[0].ToLower()) ? AliasMapping[tokens[0].ToLower()] : tokens[0].ToLower();
                                var alias1 = AliasMapping.ContainsKey(tokens[1].ToLower()) ? AliasMapping[tokens[1].ToLower()] : tokens[1].ToLower();
                                var list = DirDictionary[term].Where(x => x.ToLower().Contains(alias) && x.ToLower().Contains(alias)).ToList();
                                if (list.Count() > 0)
                                {
                                    msg = string.Join("\t", list.Take(10).ToList());
                                    found = true;
                                }
                            }
                        }

                        if (tokens.Length > 3)
                        {
                            msg = string.Format("{0} TOO DEEP!", data);
                        }
                        else if (!found)
                        {
                            msg = string.Format("{0} NOT FOUND!", data);
                        }

                    }
                    // Echo the data back to the client.
                    byte[] msgBuf = Encoding.ASCII.GetBytes(msg);

                    handler.Send(msgBuf);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }
        public static int Main(string[] args)
        {
            DirDictionary = new DCDictionary();
            AliasMapping = new DCAliasMapping();
            var assmblyPath = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            var aliasFilePath = Path.Combine(assmblyPath, "alias.map");
            if (File.Exists(aliasFilePath))
            {
                using (FileStream fileStream = new FileStream(aliasFilePath, FileMode.Open))
                {
                    AliasMapping.Deserialize(fileStream);
                }
                Console.WriteLine("Loading the alias mapping ...");
                foreach (var kvp in AliasMapping)
                {
                    Console.WriteLine(kvp.Key + " = " + kvp.Value);
                }
                Console.WriteLine("{0} Items has alias.", AliasMapping.Count());
            }
            if (args.Length > 0 && args[0] == "alias")
            {
                Console.WriteLine("Alias mapping format: [key=value]: ");
                while(true)
                {
                    var line = Console.ReadLine();
                    if(!string.IsNullOrEmpty(line))
                    {
                        var tokens = line.Split(new char[] { '=' });
                        if(tokens.Length != 2)
                        {
                            Console.WriteLine("Invalid input!" + line);
                        }
                        else
                        {
                            if(AliasMapping.ContainsKey(tokens[0].ToLower()))
                            {
                                Console.WriteLine("Duplicate alias! [{0} = {1}]", tokens[0].ToLower(), AliasMapping[tokens[0].ToLower()]);
                            }
                            else
                            {
                                AliasMapping[tokens[0].ToLower()] = tokens[1].ToLower();
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                FileStream dcAlias = null;
                if (!File.Exists(aliasFilePath))
                {
                    dcAlias = new FileStream(aliasFilePath, FileMode.Create);
                }
                else
                {
                    dcAlias = new FileStream(aliasFilePath, FileMode.Open);
                }
                AliasMapping.Serialize(dcAlias);
                dcAlias.Close();
                return 0;
            }
            var dcFiles = new DirectoryInfo(assmblyPath).GetFiles("*.data", SearchOption.TopDirectoryOnly);
            foreach (var dcFile in dcFiles)
            {
                Console.WriteLine("Loading dc file:" + dcFile.FullName);
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                using (FileStream fileStream = new FileStream(dcFile.FullName, FileMode.Open))
                {
                    DirDictionary.Deserialize(fileStream);
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Console.WriteLine("Elapsed:" + ts.TotalMilliseconds / 1000);
            }
            if(args.Length >0 && args[0] == "build")
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (Directory.Exists(args[i]))
                    {
                        var dirDictionary = new DCDictionary();
                        var dirToIndex = args[i];
                        string folderName = new DirectoryInfo(dirToIndex).Name;
                        string dcDataPath = Path.Combine(assmblyPath, folderName + ".data");
                        FileStream dcData = null;
                        if (!File.Exists(dcDataPath))
                        {
                            dcData = new FileStream(dcDataPath, FileMode.Create);
                        }
                        else
                        {
                            dcData = new FileStream(dcDataPath, FileMode.Open);
                        }
                        var dirInfo = new DirectoryInfo(dirToIndex);
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();
                        Console.WriteLine("Started to build index for the folder: " + dirToIndex);
                        try
                        {
                            var dirs = dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories);
                            dirDictionary.Add(dirInfo);
                            foreach (var dir in dirs)
                            {
                                dirDictionary.Add(dir);
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                        }
                        dirDictionary.Serialize(dcData);
                        DirDictionary.Merge(dirDictionary);
                        dcData.Close();
                        stopWatch.Stop();
                        dirDictionary.Dispose();
                        TimeSpan ts = stopWatch.Elapsed;
                        Console.WriteLine("Elapsed:" + ts.TotalMilliseconds / 1000);
                    }
                }
                return 0;
            }

            Thread thread = new Thread(StartListening);
            thread.Start(11001);
           
            init = true;
            return 0;
        }
    }
}
