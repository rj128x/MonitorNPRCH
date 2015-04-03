using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitorNPRCH
{
    class Program
    {
        static void Main(string[] args)
        {
            //Инициализация настроек
            Settings.init();
            //Создание лог файла
            Logger.InitFileLogger(Settings.single.LogPath, "MonitorNPRCH");
            //Инициализация списка обработанных отчетов
            ProcessedReports.Init();
            Logger.Info("Run");
            

            //Если переданы аргументы, то программа запущена в режиме проверки файлов на ftp
            if (args.Count() > 0) {
                int hours = Int32.Parse(args[0]);
                DateTime de = DateTime.Parse(DateTime.Now.ToString("dd.MM.yyyy HH:00:00"));
                DateTime ds = de.AddHours(-hours);
                CheckData.checkData(ds, de);
            }
            else{
                //Настройка форматов чисел (разделитель точка
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-GB");
                System.Threading.Thread.CurrentThread.CurrentCulture = ci;
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;

                //Создание объекта чтения
                MonitorNPRCH monitor = new MonitorNPRCH();

                //Счтиываем данные 
                monitor.StartRead(Settings.single.DepthFirstRun);
                while (true) {//В цикле каждые 15 минут считываем недостающие данные
                    monitor.StartRead(Settings.single.DepthRead);
                    Thread.Sleep(15 * 60 * 1000);
                }
            }
        }
    }
}
