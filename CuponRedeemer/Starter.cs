using System;
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
    
    
    
   
}
