using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;



namespace CuponRedeemer
{
    /// <summary>
    /// Enter point
    /// </summary>
    static class CuponProgram
    {
        public static ServerListener listener;
        public static InputControl control;
        public static CuponManager manager;

        static void Main()
        {
            Console.WriteLine("Cupon Manager v0.00001a early pre-alpha by Iaroslav Chernii 2017");
            Console.WriteLine("/help - show all commands");

            manager = new CuponManager();
            control = new InputControl();
            listener = new ServerListener();
        }
    }
    /// <summary>
    /// Server listener 
    /// </summary>
    class ServerListener
    {
        public static Thread serverThread;
        bool active = false;
        
        public ServerListener ()
        {
            if (serverThread != null)
                return;

                ThreadStart function = new ThreadStart(StartListening);
                serverThread = new Thread(function);
                serverThread.IsBackground = true;
                serverThread.Start();
            
        }

        /// <summary>
        /// Start listening
        /// </summary>
        public void StartListening()
        {
            if (active)
                return;

            active = true;
            string localIP = "192.168.1.67";
                             //  "172.31.13.91";
            int port = 5000;

            IPAddress localAdd = IPAddress.Parse(localIP);
            TcpListener listener = new TcpListener(localAdd, port);

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("\nConfigure IP to make it work (server module is dead) ...\nType /exit ");
                return;
            }
            

            Console.WriteLine("Listening...");
            while (true)
            {

                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);

                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                string result = CuponProgram.manager.UseCupon(dataReceived);

                Console.WriteLine($"Received:{dataReceived}");
                Console.WriteLine($"Answer{result}");

                stream.Write(Encoding.ASCII.GetBytes(result), 0, Encoding.ASCII.GetByteCount(result));
                client.Close();
            }
        }
    }
    /// <summary>
    /// Console input module
    /// </summary>
    class InputControl
    {
        //main thread (inputThread.isBackGround == false)
        public static Thread inputThread;
       
        public InputControl()
        {
            if (inputThread != null)
                return;
            //Start threads
            ThreadStart input = new ThreadStart(CheckInput);
            inputThread = new Thread(input);
            inputThread.Start();


        }

        /// <summary>
        /// Command input thread function
        /// </summary>
        void CheckInput()
        {
            while(true)
            {
                CheckForCommand(Console.ReadLine());
            }
        }
        /// <summary>
        /// Check current input for command
        /// </summary>
        /// <param name="input"></param>
        void CheckForCommand(string input)
        {
            string[] splitted = input.Split(' ');
            switch(splitted[0])
            {
                #region /help
                case "/help":
                    Console.WriteLine("\nAll commands:\n/exit - save and exit the program\n/groups - show all current cupon groups\n/cupons <groupname> - show all cupons, which belongs to group\n/newgroup <name> - create new cupons group\n/addcupons <group> <content> <count> <expire date> - add cupons to group");
                    break;
                #endregion
                #region /exit
                case "/exit":
                    Console.WriteLine("Saving values");

                    CuponProgram.manager.SaveProfile();
                    foreach (var item in CuponProgram.manager.CuponGroups)
                        item.RefreshLog();

                    Console.WriteLine("Shutting down...");

                    inputThread.Abort();
                    break;
                #endregion
                #region /groups
                case "/groups":
                    Console.WriteLine("\nCurrent groupes:");
                    if (CuponProgram.manager.CuponGroups.Count > 0)
                    foreach (var item in CuponProgram.manager.CuponGroups)
                        Console.WriteLine(item.Name + " " + item.Cupons.Count.ToString());
                    else
                        Console.WriteLine("\nNo Groups");
                    break;
                #endregion
                #region /cupons
                case "/cupons":
                    string name = string.Empty;

                    try
                    {
                        name = splitted[1];
                    }
                    catch
                    {
                        Console.WriteLine($"\nWrong command usage");
                        return;
                    }
                    Console.WriteLine($"\nLooking for {name} cupons:");

                    CuponGroup targetGroup = new CuponGroup("");
                    bool ok = false;

                    foreach (var item in CuponProgram.manager.CuponGroups)
                        if (item.Name == name)
                        {
                            ok = true;
                            targetGroup = item;
                            break;
                        }

                    if (!ok)
                    {
                        Console.WriteLine("\nCant find group with this name");
                        return;
                    }

                    if(targetGroup.Cupons.Count > 0)
                        for (int i = 0; i < targetGroup.Cupons.Count; i++)
                            Console.WriteLine($"{i+1}: {targetGroup.Cupons[i].Code} {targetGroup.Cupons[i].ContentId} {targetGroup.Cupons[i].ExpireTime.ToShortDateString()} {targetGroup.Cupons[i].Valid}"); 
                    else
                        Console.WriteLine($"\nGroup {targetGroup.Name} is empty!");

                    break;
                #endregion
                #region /newgroup
                case "/newgroup":

                    if (splitted.Length < 2)
                    {
                        Console.WriteLine("\nWrong arguments!");
                        return;
                    }
                       
                    CuponGroup newGroup = new CuponGroup(splitted[1]);
                    CuponProgram.manager.AddGroup(newGroup);

                    Console.WriteLine($"Created {newGroup.Name} group!");
                    break;
                #endregion
                #region /addcupons
                case "/addcupons":

                    if (splitted.Length < 5)
                    {
                        Console.WriteLine("\nWrong arguments!");
                        return;
                    }

                    string groupName = "";
                    try
                    {
                        groupName = splitted[1];
                    }
                    catch
                    {
                        Console.WriteLine("\nWrong arguments!");
                        return;
                    }

                    CuponGroup targetGroup2 = new CuponGroup("");
                    bool found = false;

                    foreach (var item in CuponProgram.manager.CuponGroups)
                    {
                        Console.WriteLine("\nFound group:" + item.Name);
                        if (item.Name == groupName)
                        {
                            found = true;
                            targetGroup2 = item;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine("\nCant find group with this name");
                        return;
                    }
                    try
                    {
                        targetGroup2.GenerateCupons(splitted[2], Convert.ToInt32(splitted[3]), splitted[4]);
                    }
                    catch
                    {
                        Console.WriteLine("\nWrong arguments!");
                    }
                    break;
                    #endregion
            }

            CuponProgram.manager.SaveProfile();
            CuponProgram.manager.CheckCupons();
        }
    }
    /// <summary>
    /// Cupon manager module
    /// </summary>
    class CuponManager
    {
        public static Thread updateThread;

        public List<CuponGroup> CuponGroups
        {
            get
            {
                return cuponBases;
            }
        }
        List<CuponGroup> cuponBases = new List<CuponGroup>();

        public CuponManager()
        {
            if (updateThread != null)
                return;

            LoadBasesFromFiles();
            SaveProfile();

            ThreadStart update = new ThreadStart(Update);
            updateThread = new Thread(update);
            updateThread.IsBackground = true;
            updateThread.Start();
        }

        /// <summary>
        /// Cupon update thread func
        /// </summary>
        void Update()
        {
            while (true)
            {
                CheckCupons();
                SaveProfile();
                Thread.Sleep(100);
            }
        }
        /// <summary>
        /// Check all cupons for their expiration date
        /// </summary>
        public void CheckCupons()
        {
            DateTime now = DateTime.Now;
            foreach (var cuponGroup in cuponBases)
            {
                foreach (var cupon in cuponGroup.Cupons)
                {
                    if(cupon.Valid)
                    if (cupon.ExpireTime < now)
                        cupon.Disable();
                }
                cuponGroup.RefreshLog();
            }
        }
        /// <summary>
        /// Answer to client for recieved code
        /// </summary>
        /// <param name="code">cupon code</param>
        /// <returns></returns>
        public string UseCupon(string code)
        {
            foreach (var item in cuponBases)
            {
                foreach (var cupon in item.Cupons)
                {
                    if(code == cupon.Code)
                    {
                        if (cupon.Valid)
                        {
                            cupon.Disable();
                            item.RefreshLog();
                            return  $"{item.Name} {cupon.ContentId}";
                        }
                        else return "Cupon is not valid";
                    }
                }
            }
            return "Wrong cupon";
        }
        /// <summary>
        /// Load cupon groups from file
        /// </summary>
        public void LoadBasesFromFiles()
        {
            if (File.Exists("C://CuponManager//Manager.txt"))
            {
                StreamReader reader = new StreamReader("C://CuponManager//Manager.txt");
                int fileCount = Convert.ToInt32(reader.ReadLine());
                for (int i = 0; i < fileCount; i++)
                {
                    string[] input = reader.ReadLine().Split(' ');
                    AddGroup(CuponGroup.LoadFromPath(input));
                }
                reader.Close();
            }
            else
            {
               FileStream a = File.Create("C://CuponManager//Manager.txt");
                a.Close();
            }
        }
        /// <summary>
        /// Add new CuponGroup object
        /// </summary>
        /// <param name="group">group</param>
        public void AddGroup(CuponGroup group)
        {
            cuponBases.Add(group);
        }
        /// <summary>
        /// Saves values to file
        /// </summary>
        public void SaveProfile()
        {
            string path = "C://CuponManager//Manager.txt";
            StreamWriter writer;
            if (File.Exists(path))
            {
                writer = new StreamWriter(path,false);
                writer.WriteLine(cuponBases.Count);
                foreach (var item in cuponBases)
                {
                    writer.WriteLine($"{item.Name} {item.LogFilePath}");
                }
            }
            else
            {
                writer = new StreamWriter(File.Create(path));
                writer.WriteLine(cuponBases.Count);
                foreach (var item in cuponBases)
                {
                    writer.WriteLine($"{item.Name} {item.LogFilePath}");
                }
            }
            writer.Close();
        }
    }
    class Cupon
    {
        //properties
        public string ContentId
        {
            get
            {
                return contentId;
            }
        }
        public string Code
        {
            get
            {
                return cuponString;
            }
        }
        public DateTime ExpireTime
        {
            get
            {
                return redeemTime;
            }
        }
        public bool Valid
        {
            get
            {
                return valid;
            }
        }
        //fields
        private string contentId;
        private DateTime redeemTime;
        private string cuponString;
        private bool valid = true;

        /// <summary>
        /// Create cupon
        /// </summary>
        /// <param name="time">Expire date</param>
        /// <param name="cupon">Cupon string</param>
        /// <param name="content">Content ID</param>
        public Cupon (DateTime expireTime, string cupon,string content,bool valid = true)
        {
            redeemTime = expireTime;
            cuponString = cupon;
            contentId = content;
            this.valid = valid;
        }

        /// <summary>
        /// Deactivate cupon
        /// </summary>
        public void Disable()
        {
            valid = false;
        }

    }
    class CuponGroup
    {
        //Properties
        public string Name
        {
            get
            {
                return name;
            }
        }
        public string LogFilePath
        {
            get
            {
                return path;
            }
        }
        public List<Cupon> Cupons
        {
            get
            {
                return cupons;
            }
        }

        private string name;
        private string path; 
        List<Cupon> cupons = new List<Cupon>();
        Random random = new Random();
        string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890+=-_)(";

        /// <summary>
        /// Create Cupon group with name
        /// </summary>
        /// <param name="name">name</param>
        public CuponGroup(string name)
        {
            this.name = name;
            path = $"C://CuponManager//{name}.txt";
        }
        /// <summary>
        /// Create CuponGroup and fill it with cupons
        /// </summary>
        /// <param name="name">group name</param>
        /// <param name="content">cupon id</param>
        /// <param name="count">count</param>
        /// <param name="expireDate">expire date</param>
        public CuponGroup(string name, string content, int count, string expireDate)
        {
            this.name = name;
            path = $"C://CuponManager//{name}.txt";
            GenerateCupons(content,count,expireDate);
        }


        /// <summary>
        /// Adds cupon to CuponGroup
        /// </summary>
        /// <param name="cupon"></param>
        public void AddCupon(Cupon cupon)
        {
            cupons.Add(cupon);
        }
        /// <summary>
        /// Get all cupons from save file
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static CuponGroup LoadFromPath(string[] input)
        {
            CuponGroup newGroup = new CuponGroup(input[0]);
            StreamReader groupReader = new StreamReader(input[1]);

            string[] allCupons = groupReader.ReadToEnd().Split('\n');
            
            foreach (string cuponInfo in allCupons)
            {
                if (cuponInfo == "")
                    continue;

                string[] info = cuponInfo.Split(' ');
                Cupon newCupon = new Cupon(DateTime.Parse(info[2]), info[0], info[1], Convert.ToBoolean(info[3]));
                newGroup.AddCupon(newCupon);
            }
            groupReader.Close();
            return newGroup;
        }
        /// <summary>
        /// Generate cupons and add them to CuponGroup
        /// </summary>
        /// <param name="content">cupon id</param>
        /// <param name="count">count</param>
        /// <param name="expireDate">expiration date</param>
        public void GenerateCupons(string content, int count, string expireDate)
        {
            for (int i = 0; i < count; i++)
            {
                startCuponGeneration: 
                 DateTime date = DateTime.Parse(expireDate);

                string cuponValue = name;
                for (int j = 0; j < 9; j++)
                {
                    int randomNumber = random.Next(0, chars.Length);
                    cuponValue += chars[randomNumber];
                }
                Cupon c = new Cupon(date, cuponValue, content);
                foreach (Cupon cupon in cupons)
                    if (cupon.Code == c.Code)
                        goto startCuponGeneration;
                cupons.Add(c);
            }
            Console.WriteLine($"Success! Now {cupons.Count} cupons!");
        }
        /// <summary>
        /// Refresh save file
        /// </summary>
        public void RefreshLog()
        {
            StreamWriter writer;
            if (File.Exists(path))
            {
                writer = new StreamWriter(path,false);
                foreach (var item in cupons)
                {
                    writer.WriteLine($"{item.Code} {item.ContentId} {item.ExpireTime.ToShortDateString()} {item.Valid}");
                }
            }
            else
            {
                writer = new StreamWriter(File.Create(path));
                foreach (var item in cupons)
                {
                    writer.WriteLine($"{item.Code} {item.ContentId} {item.ExpireTime.ToShortDateString()} {item.Valid}");
                }
            }
            writer.Close();
        }
    }
}
