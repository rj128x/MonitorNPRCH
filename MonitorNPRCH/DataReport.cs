using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using Ionic.Zip;

namespace MonitorNPRCH {
	/// <summary>
	/// Класс отчета
	/// </summary>
	public class DataReport {
		/// <summary>
		/// Дата начала
		/// </summary>
		public DateTime DateStart { get; protected set; }
		/// <summary>
		/// Дата окончания
		/// </summary>
		public DateTime DateEnd { get; protected set; }
		/// <summary>
		/// Считанные данные
		/// </summary>
		public Dictionary<DateTime, Dictionary<string, double>> Data;


		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="dateStart">Дата начала</param>
		/// <param name="dateEnd">Дата конца</param>
		public DataReport(DateTime dateStart, DateTime dateEnd) {
			Logger.Info(String.Format("Создание пустого отчета с {0} по {1}", dateStart, dateEnd));
			DateStart = dateStart;
			DateEnd = dateEnd;

			DateTime date = dateStart.AddSeconds(0);
			Data = new Dictionary<DateTime, Dictionary<string, double>>();
			//Заполнение отчета пусьыми данными
			while (date < dateEnd) {
				Data.Add(date, new Dictionary<string, double>());
				foreach (PointInfo pi in Settings.single.Points.Points) {
					Data[date].Add(pi.Descr, 0);
				}
				date = date.AddSeconds(1);
			}
			Logger.Info("Пустой отчет создан");
		}

		/// <summary>
		/// Считывает данные из Овации
		/// </summary>
		/// <returns>true, если данные успешно считаны</returns>
		public bool ReadData() {
			Logger.Info("Чтение данных");
			bool isOk = true;

			//Создание подключения
			OleDbConnection connection = new OleDbConnection("Provider=Ovation Process Historian OLE DB Provider; Data Source=Drop160_n6; RetrievalMode=MODE_LATEST;");
			OleDbDataReader reader = null;
			try {
				Logger.Info("Подключение к базе");
				connection.Open();
				foreach (PointInfo pi in Settings.single.Points.Points) {
					Logger.Info(String.Format("Чтение данных для точки {0} ({1})", pi.Name, pi.Descr));
					OleDbCommand command = connection.CreateCommand();

					//Формирование команды
					command.CommandText = String.Format("select timestamp, f_value  from processeddata (#{0}#, #{1}#, IntervalSize, 1, PointNamesOnly, OPH_TIMEAVERAGE, '{2}')",
														DateStart.AddHours(-Settings.single.HoursUTC).ToString("MM'/'dd'/'yyyy HH:mm:ss"),
														DateEnd.AddHours(-Settings.single.HoursUTC).ToString("MM'/'dd'/'yyyy HH:mm:ss"), pi.Name);
					command.CommandType = CommandType.Text;
					reader = command.ExecuteReader();
					//Чтение данных
					while (reader.Read()) {
						DateTime dt = DateTime.Parse(reader[0].ToString());
						dt = dt.AddHours(Settings.single.HoursUTC);
						double val = (double)reader[1];
						try {
							Data[dt][pi.Descr] = val;
						}
						catch (Exception e) {
							Logger.Info("Ошибка при обработке данных ");
							Logger.Info(e.ToString());
						}
					}
					reader.Close();
				}
			}
			catch (Exception e) {
				isOk = false;
				Logger.Info("Ошибка при выборке из БД");
				Logger.Info(e.ToString());
			}
			finally //Закрытие всех подключений к БД Овация
			{
				try {
					reader.Close();
				}
				catch { }

				try {
					Logger.Info("Отключение от базы");
					connection.Close();
				}
				catch (Exception e) {
					Logger.Info("Ошибка при закрытии подключения");
					Logger.Info(e.ToString());
				}
			}
			Logger.Info("Чтение данных завершено ok:" + isOk);
			return isOk;
		}

		/// <summary>
		/// Получение иммени файла на диске до передачи на ftp
		/// </summary>
		/// <param name="DateStart">Дата отчета</param>
		/// <param name="ga">Номер га</param>
		/// <param name="ext">расширение (при создании отчета - txt, при создании архива - txt.zip</param>
		/// <returns>возвращает имя файла на диске для передаци на ftp</returns>
		public static string getFileName(DateTime DateStart, int ga, string ext) {
			string dir = Settings.single.DataPath + "/" + ga.ToString("00") + "/" + DateStart.ToString("yyyy") + "/" + DateStart.ToString("MM") + "/" + DateStart.ToString("dd");
			string fileName = dir + "/" + String.Format("{0:00}{1}{2:00}.{3}", ga, DateStart.ToString("yyyyMMdd"), DateStart.Hour + 1, ext);
			return fileName;
		}

		/// <summary>
		/// Создает файлы с отчетами
		/// </summary>
		public void CreateReportFiles() {
			foreach (int ga in Settings.single.ActiveGAList) {
				string fileName = getFileName(DateStart, ga, "txt");
				Logger.Info("Создание файла отчета " + fileName);
				FileInfo fi = new FileInfo(fileName);
				//создание директории
				if (!Directory.Exists(fi.Directory.FullName)) {
					Directory.CreateDirectory(fi.Directory.FullName);
				}

				//Создание файла отчета
				TextWriter writer = new StreamWriter(fileName, false);
				foreach (DateTime date in Data.Keys) {
					int sec = date.Minute * 60 + date.Second + 1;
					string p1 = String.Format("GA{0} P", ga);
					string p2 = String.Format("GA{0} F", ga);
					string p3 = String.Format("GA{0} PZad", ga);
					double v = 60.0 / 48.0 * Data[date][p2];
					double p = Data[date][p1];
					double pz = Data[date][p3];
					//обнуление маленьких значений
					v = v < 0.5 ? 0 : v;
					p = p < 0.5 ? 0 : p;
					pz = pz < 0.5 ? 0 : pz;
					//строка отчета
					String str = String.Format("{0}:{1:0.0000};{2:0.0000};{3:0.0000};2;", sec, v, p, pz);
					writer.WriteLine(str);
				}
				writer.Close();

				//Создание архива
				string zipName = getFileName(DateStart, ga, "txt.zip");
				if (File.Exists(zipName)) { //если файл архива существуе, удалаем его
					File.Delete(zipName);
				}
				ZipFile zip = new ZipFile(zipName);
				zip.AddFile(fileName, "");
				zip.Save();
				fi.Delete();

				Logger.Info("Создание файла отчета завершено");
			}
		}
	}
}
