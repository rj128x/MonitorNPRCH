using BytesRoad.Net.Ftp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitorNPRCH {
    /// <summary>
    /// Класс для проверки наличия файлов на ftp cthdtht
    /// </summary>
    public static class CheckData {
        /// <summary>
        /// Считывает данные
        /// </summary>
        /// <param name="dateStart">Дата начала</param>
        /// <param name="dateEnd">Дата окончания</param>
        public static void checkData(DateTime dateStart, DateTime dateEnd) {
            bool send=true;
            for (int ga = 1; ga <= 10; ga++) {
                bool sendGA = true;
                DateTime date = dateStart.AddHours(0);
                while (date < dateEnd) {
                    bool ok = FileExists(ga, date);
                    sendGA = sendGA && ok;
                    date = date.AddHours(1);
                }
                send = send && sendGA;
            }
            if (!send) {
                MailClass.SendTextMail(String.Format("Ошибка при проверке данных на ftp {0} - {1}", dateStart, dateEnd),"Нет данных на ftp");
            }
        }

        /// <summary>
        /// Проверяет есть ли файл на ftp сервере
        /// </summary>
        /// <param name="ga">номер га</param>
        /// <param name="date">дата отчета</param>
        /// <returns></returns>
        public static bool FileExists(int ga, DateTime date) {
            bool ok = true;
            try {
                Logger.Info(String.Format("Проверка файла на ftp: ГА{0} за {1}", ga, date));
                FtpClient client = new FtpClient();
                int timeout = 10000;

                client.PassiveMode = !Settings.single.FTPActive;
                client.Connect(timeout, Settings.single.FTPServer, Settings.single.FTPPort);
                client.Login(timeout, Settings.single.FTPUser, Settings.single.FTPPassword);

                List<string> dirs = new List<string>();
                dirs.Add(ga.ToString("00"));
                dirs.Add(date.ToString("yyyy"));
                dirs.Add(date.ToString("MM"));
                dirs.Add(date.ToString("dd"));

                foreach (string dir in dirs) {
                    try {
                        Logger.Info("Вход в дирректорию " + dir);
                        client.ChangeDirectory(timeout, dir);
                    }
                    catch (Exception e) {
                        Logger.Info("Ошибка входа в дирректорию " + dir);
                        ok = false;
                        break;
                    }
                }

                if (ok) {
                    string fn = String.Format("{0:00}{1}{2:00}.{3}", ga, date.ToString("yyyyMMdd"), date.Hour + 1, "txt.zip");
                    try {
                        Logger.Info("Получение файла " + fn);
                        client.GetFile(timeout, fn);
                    }
                    catch (Exception e) {
                        Logger.Info("Ошибка получения файла " + fn);
                        ok = false;
                    }
                }

                client.Disconnect(timeout);
            }
            catch (Exception e) {
                Logger.Info("Ошибка при проверке файла");
                Logger.Info(e.ToString());
                ok = false;                
            }
            Logger.Info("Проверка завершена: " + ok.ToString());
            return ok;
        }
    }
}
