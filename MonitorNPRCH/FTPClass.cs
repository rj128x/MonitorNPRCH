using BytesRoad.Net.Ftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MonitorNPRCH {
	/// <summary>
	/// Класс для работы с ftp
	/// </summary>
	public class FTPClass {
		/// <summary>
		/// Отправка файла на сервер
		/// </summary>
		/// <param name="fileName">Имя файла</param>
		/// <returns>true, если файл успешно отправлен</returns>
		public static bool SendFile(string fileName) {
			bool ok = true;
			try {
				Logger.Info("Отправка файла на ftp: " + fileName);
				FtpClient client = new FtpClient();
				int timeout = 10000;

				//Подключение к ftp
				client.PassiveMode = !Settings.single.FTPActive;
				client.Connect(timeout, Settings.single.FTPServer, Settings.single.FTPPort);
				client.Login(timeout, Settings.single.FTPUser, Settings.single.FTPPassword);


				FileInfo fi = new FileInfo(fileName);
				List<string> dirs = new List<string>();
				DirectoryInfo dir = fi.Directory;
				DirectoryInfo InitDI = new DirectoryInfo(Settings.single.DataPath);

				//Создание списка директорий для доступа к файлу
				dirs.Add(dir.Name);
				while (dir.Parent.FullName != InitDI.FullName) {
					dirs.Add(dir.Parent.Name);
					dir = dir.Parent;
				}

				//Создание иерархии на ftp сервере
				for (int i = dirs.Count - 1; i >= 0; i--) {
					try {
						client.ChangeDirectory(timeout, dirs[i]);
					}
					catch {
						try {
							Logger.Info(String.Format("Создание директории {0}", dirs[i]));
							client.CreateDirectory(timeout, dirs[i]);
							client.ChangeDirectory(timeout, dirs[i]);
						}
						catch (Exception e) {
							Logger.Info("Ошибка при создаинии директории ");
							Logger.Info(e.ToString());
						}
					}
				}

				try //Отправка файла на ftp
				{
					client.PutFile(timeout, fi.Name, fileName);
				}
				catch (Exception e) {
					Logger.Info("-----");
					Logger.Info(e.ToString());
					ok = false;
				}
				client.Disconnect(timeout);
			}
			catch (Exception e) {
				Logger.Info("Ошибка при отправке файла");
				Logger.Info(e.ToString());
				ok = false;
				//MailClass.SendTextMail(String.Format("Ошибка при отправке отчета НПРЧ {0} ", fileName), e.ToString());
			}
			Logger.Info("Отправка завершена: " + ok.ToString());
			return ok;
		}




	}
}

