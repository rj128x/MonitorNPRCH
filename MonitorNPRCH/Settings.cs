using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorNPRCH {
	/// <summary>
	/// Класс для работы с массивом точек
	/// </summary>
	public class PointsSettings {
		/// <summary>
		/// Массив точек
		/// </summary>
		public List<PointInfo> Points { get; set; }
		/// <summary>
		/// Словарь точек по имени точки в Овации
		/// </summary>
		[System.Xml.Serialization.XmlIgnoreAttribute]
		public Dictionary<string, string> PointsByName { get; set; }
		/// <summary>
		/// Словарь точек по ИД точки в XML файле
		/// </summary>
		[System.Xml.Serialization.XmlIgnoreAttribute]
		public Dictionary<string, string> PointsByDescr { get; set; }

		/// <summary>
		/// Создание массивов
		/// </summary>
		public void initData() {
			PointsByName = new Dictionary<string, string>();
			PointsByDescr = new Dictionary<string, string>();
			foreach (PointInfo pi in Points) {
				try {
					PointsByName.Add(pi.Name, pi.Descr);
				}
				catch (Exception e) {
					Logger.Info(String.Format("Ошибка при добавлении точки в словарь {0}  {1}", pi.Name, pi.Descr));
				}

				try {
					PointsByDescr.Add(pi.Descr, pi.Name);
				}
				catch (Exception e) {
					Logger.Info(String.Format("Ошибка при добавлении точки в словарь {0}  {1}", pi.Name, pi.Descr));
				}
			}
		}
	}

	/// <summary>
	/// Класс для описания точки
	/// </summary>
	public class PointInfo {
		/// <summary>
		/// Имя точки в Овации
		/// </summary>
		[System.Xml.Serialization.XmlAttribute]
		public string Name { get; set; }
		/// <summary>
		/// ИД точки в xml
		/// </summary>
		[System.Xml.Serialization.XmlAttribute]
		public string Descr { get; set; }
	}

	/// <summary>
	/// Класс настроек в системе
	/// </summary>
	public class Settings {
		/// <summary>
		/// Объект настроек
		/// </summary>
		protected static Settings settings;
		/// <summary>
		/// Путь к папке лог файлов
		/// </summary>
		public string LogPath { get; set; }
		/// <summary>
		/// Путь к папке с даными
		/// </summary>
		public string DataPath { get; set; }
		/// <summary>
		/// Файл массива точек для работы
		/// </summary>
		public string PointsFile { get; set; }
		/// <summary>
		/// Файл с информацией о сгенерированных отчетах
		/// </summary>
		public string ReportsListFile { get; set; }
		/// <summary>
		/// Сдвиг времени от UTC
		/// </summary>
		public int HoursUTC { get; set; }
		/// <summary>
		/// Глубина считывания при первом запуске
		/// </summary>
		public int DepthFirstRun { get; set; }
		/// <summary>
		/// Глубина считывания каждый час
		/// </summary>
		public int DepthRead { get; set; }
		/// <summary>
		/// Почтовый сервер
		/// </summary>
		public string SMTPServer { get; set; }
		/// <summary>
		/// От кого отправлять письмо
		/// </summary>
		public string SMTPFrom { get; set; }
		/// <summary>
		/// Кому отправлять информацию об ошибке
		/// </summary>
		public string SMTPErrorTo { get; set; }
		/// <summary>
		/// Пользователь
		/// </summary>
		public string SMTPUser { get; set; }
		/// <summary>
		/// Пароль
		/// </summary>
		public string SMTPPassword { get; set; }
		/// <summary>
		/// Порт
		/// </summary>
		public int SMTPPort { get; set; }
		/// <summary>
		/// Домен
		/// </summary>
		public string SMTPDomain { get; set; }
		/// <summary>
		/// Отправлять письмо об ошибке
		/// </summary>
		public bool SendErrorMail { get; set; }
		/// <summary>
		/// Активный режим работы с ftp
		/// </summary>
		public bool FTPActive { get; set; }
		/// <summary>
		/// Сервер ftp
		/// </summary>
		public string FTPServer { get; set; }
		/// <summary>
		/// Порт ftp
		/// </summary>
		public int FTPPort { get; set; }
		/// <summary>
		/// Пользователь ftp
		/// </summary>
		public string FTPUser { get; set; }
		/// <summary>
		/// Пароль ftp
		/// </summary>
		public string FTPPassword { get; set; }
		/// <summary>
		/// Список активных ГА (разделен ;)
		/// </summary>
		public string ActiveGA { get; set; }

		/// <summary>
		/// Настройки точек
		/// </summary>
		public PointsSettings Points;

		/// <summary>
		/// Список активных ГА для работы
		/// </summary>
		[System.Xml.Serialization.XmlIgnoreAttribute]
		public List<int> ActiveGAList { get; set; }


		/// <summary>
		/// Ссылка на единственный объект настроек
		/// </summary>
		public static Settings single {
			get {
				return settings;
			}
		}

		static Settings() {

		}

		/// <summary>
		/// Инициализация настроек
		/// </summary>
		public static void init() {
			//чтение настроек из xml
			Settings settings = XMLSer<Settings>.fromXML("Data\\Settings.xml");
			//чтение списка точек из xml
			settings.Points = XMLSer<PointsSettings>.fromXML("Data\\" + settings.PointsFile);
			//инициализация массивов точек
			settings.Points.initData();
			//Создание директории выходных файлов
			if (!Directory.Exists(settings.DataPath)) {
				Directory.CreateDirectory(settings.DataPath);
			}

			char[] sep = { ';' };
			string[] arr = settings.ActiveGA.Split(sep);
			settings.ActiveGAList = new List<int>();
			foreach (string str in arr) {
				try {
					settings.ActiveGAList.Add(Int32.Parse(str));
				}
				catch { }
			}
			Settings.settings = settings;
		}


	}
}
