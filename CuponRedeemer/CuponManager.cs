using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace CuponRedeemer
{
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
                    if (cupon.Valid)
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
                    if (code == cupon.Code)
                    {
                        if (cupon.Valid)
                        {
                            cupon.Disable();
                            item.RefreshLog();
                            return $"{item.Name} {cupon.ContentId}";
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
                writer = new StreamWriter(path, false);
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
}
