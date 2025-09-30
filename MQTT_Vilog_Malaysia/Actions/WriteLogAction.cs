using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class WriteLogAction
    {
        string ErrorPathDir { get; set; }
        string ErrorPathFile { get; set; }
        string RunPathDir { get; set; }
        string RunPathFile { get; set; }


        public WriteLogAction()
        {
            DateTime today = DateTime.Now;

            ErrorPathDir = @"./Error";
            ErrorPathFile = @"./Error/log_" + today.Day + "_" + today.Month + "_" + today.Year + ".txt";
            RunPathDir = @"./Run";
            RunPathFile = @"./Run/log_" + today.Day + "_" + today.Month + "_" + today.Year + ".txt";
        }

        public bool CheckErrorDirExists()
        {
            bool check = false;
            try
            {
                check = Directory.Exists(ErrorPathDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return check;
        }

        public bool CheckRunDirExists()
        {
            bool check = false;
            try
            {
                check = Directory.Exists(RunPathDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return check;
        }

        public bool CheckErrorFileExists()
        {
            bool check = false;
            try
            {
                check = File.Exists(ErrorPathFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return check;
        }

        public bool CheckRunFileExists()
        {
            bool check = false;
            try
            {
                check = File.Exists(RunPathFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return check;
        }

        public void CreateErrorLogDir()
        {
            try
            {
                if (!CheckErrorDirExists())
                {
                    Directory.CreateDirectory(ErrorPathDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void CreateRunLogDir()
        {
            try
            {
                if (!CheckRunDirExists())
                {
                    Directory.CreateDirectory(RunPathDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void CreateErrorFile()
        {
            try
            {
                if (!CheckErrorFileExists())
                {
                    File.Create(ErrorPathFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void CreateRunFile()
        {
            try
            {
                if (!CheckRunFileExists())
                {
                    File.Create(RunPathFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task WriteErrorLog(string message)
        {
            try
            {
                CreateErrorLogDir();
                CreateErrorFile();

                using StreamWriter file = new(ErrorPathFile, append: true);

                await file.WriteLineAsync(message);

                file.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task WriteRunLog(string message)
        {
            try
            {
                CreateRunLogDir();
                CreateRunFile();

                using StreamWriter file = new(RunPathFile, append: true);

                await file.WriteLineAsync(message);

                file.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
