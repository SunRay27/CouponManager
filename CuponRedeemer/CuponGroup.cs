using System;
using System.IO;
using System.Collections.Generic;


namespace CuponRedeemer
{
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
            GenerateCupons(content, count, expireDate);
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
                writer = new StreamWriter(path, false);
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
