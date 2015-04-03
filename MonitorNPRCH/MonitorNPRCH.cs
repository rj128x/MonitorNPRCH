using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorNPRCH {
    /// <summary>
    /// Информация о сформированных отчетах 
    /// </summary>
	public class ProcessedReports {
        /// <summary>
        /// ссылка на объект класса
        /// </summary>
		protected static ProcessedReports single;
		/// <summary>
		/// ссылка на объект класса
		/// </summary>
        public static ProcessedReports Single {
			get {
				return single;
			}
		}

        /// <summary>
        /// Информация о каждом отчете
        /// </summary>
		public class ReportInfo {
            /// <summary>
            /// Дата генерации отчета
            /// </summary>
			public DateTime GenDate;
            /// <summary>
            /// Дата отчета
            /// </summary>
			public DateTime RepDate;
            /// <summary>
            /// отчет считан успешно
            /// </summary>
			public bool ReadOK;
            /// <summary>
            /// отчет отправлен на ftp
            /// </summary>
			public bool SendOK;
		}

        /// <summary>
        /// Список отчетов
        /// </summary>
		public List<ReportInfo> Reports { get; set; }

        /// <summary>
        /// Инициализация списка отчетов
        /// </summary>
		public static void Init() {
			Logger.Info("Получение данных о сгенерированных ранее отчетах");
			ProcessedReports settings = new ProcessedReports();            
			try {
                //Попытка считать данные о сформированных отчетов из xml
				settings = XMLSer<ProcessedReports>.fromXML(Settings.single.DataPath + "/FinishedReports.xml");
			}
			catch {
			}
			if (settings == null) {//Если данные не считаны, создаем пустой объект
				settings = new ProcessedReports();
				settings.Reports = new List<ReportInfo>();
			}

			ProcessedReports.single = settings;

			Logger.Info("Получение данных о сгенерированных ранее отчетах завершено");
		}

        /// <summary>
        /// Получаем ссылку на информацию об отчете из массива
        /// </summary>
        /// <param name="RepDate">Необходимая дата отчета</param>
        /// <returns></returns>
		public static ReportInfo GetReportInfo(DateTime RepDate) {
			foreach (ReportInfo ri in single.Reports) {//Поиск отчета в списке
				if (RepDate == ri.RepDate) {
					return ri;
				}
			}
            //Если отчет не найден, создаем новый
			ReportInfo rep = new ReportInfo();
			rep.RepDate = RepDate;
			single.Reports.Add(rep);
			SaveReportInfo();
			return rep;
		}

        //Сохранение списка отчетов в xml файл
		public static void SaveReportInfo() {
            //Если список больше 50 позиций, первую удаляем
			if (single.Reports.Count > 50) {
				single.Reports.RemoveAt(0);
			}
            //Соранение в xml
			XMLSer<ProcessedReports>.toXML(single, Settings.single.DataPath + "/FinishedReports.xml");
		}

	}

    /// <summary>
    /// Класс осуществляющий чтение данных и передачу на ftp сервер
    /// </summary>
	public class MonitorNPRCH {
        /// <summary>
        /// Определяет, есть ли файлы по всем га за данную дату. 
        /// </summary>
        /// <param name="dt">Дата отчета</param>
        /// <returns></returns>
		protected bool ReportsFound(DateTime dt) {
			bool ok = true;
            foreach (int ga in Settings.single.ActiveGAList) {
				if (!File.Exists(DataReport.getFileName(dt, ga,"txt.zip")))
					ok = false;
			}
			return ok;
		}

        /// <summary>
        /// Считывает отчеты с текущей даты вглубь на hours часов
        /// </summary>
        /// <param name="hours">количество часов</param>
		public void StartRead(int hours) {
            //Дата начала 
			DateTime ds = DateTime.Parse(DateTime.Now.ToString("dd.MM.yyyy HH:00:00")).AddHours(-hours);
            //дата окончания
			DateTime de = ds.AddHours(hours);
            //дата текущего отчета
			DateTime dt = ds.AddHours(0);
			Logger.Info(String.Format("Генерация отчетов {0} --- {1}", ds, de));
			while (dt < de) {
                //Определаем, обрабатывалась ли уже дата
				bool generated = false; 
				ProcessedReports.ReportInfo CurrentReportInfo = null;
				foreach (ProcessedReports.ReportInfo ri in ProcessedReports.Single.Reports) {//Перебираем сгенерированные отчеты
					if (dt == ri.RepDate) {//Если дата есть в списке
						if (ri.ReadOK && ReportsFound(dt)) {//Есла данные успешно считаны и все файлы есть на диске
							generated = true;
							Logger.Info(String.Format("{0}: Отчет найден", dt));
							if (ri.SendOK) { //Если отчет был успешно отправлен, ничего не делаем
								Logger.Info(String.Format("{0}: Отчет был отправлен", dt));
							}
							else {//Иначе попытка повторной отправки
								Logger.Info("Повторная отправка отчета");
								bool sent = true;
                                foreach (int ga in Settings.single.ActiveGAList) {
									bool ok = FTPClass.SendFile(DataReport.getFileName(dt,ga,"txt.zip"));
									if (!ok)
										sent = false;
								}
								ri.SendOK = sent;
								ProcessedReports.SaveReportInfo();//Обновление информации об отчете в файле
                                if (!sent) {//Если отправка неуспешна, отправляем почту об ошибке
                                    MailClass.SendTextMail(String.Format("Ошибка при отправке отчета НПРЧ {0}",dt), "Не отправлен отчет НПРЧ");
                                }
							}
							break;
						}
						CurrentReportInfo = ri;
					}
				}

				if (!generated) {//Если данные не обработаны
					if (CurrentReportInfo == null)
						CurrentReportInfo = ProcessedReports.GetReportInfo(dt);
					CurrentReportInfo.GenDate = DateTime.Now;
					DataReport rep = new DataReport(dt, dt.AddHours(1));//Создаем новый объект для чтения данных
                    
					bool ok = rep.ReadData();//Считываем данные из Овации
					CurrentReportInfo.ReadOK = ok;     
					ProcessedReports.SaveReportInfo();

                    if (!ok) {//Если считывание неуспешно отправка почты об ошибке
                        MailClass.SendTextMail(String.Format("Ошибка при формировании отчета НПРЧ {0}", dt), "Не создан отчет НПРЧ");
                    }
					if (ok) {//Если данные успешно считаны
						rep.CreateReportFiles();//Создаем файлы на диске
						bool sent = true;
                        foreach (int ga in Settings.single.ActiveGAList) {//отправляем файлы на ftp
							bool log = FTPClass.SendFile(DataReport.getFileName(dt, ga,"txt.zip"));
							if (!log)
								sent = false;
						}
						CurrentReportInfo.SendOK = sent;
                        if (!sent) {//Если отправка неуспешна, отправляем почту об ошибке
                            MailClass.SendTextMail(String.Format("Ошибка при отправке отчета НПРЧ {0}", dt), "Не отправлен отчет НПРЧ");
                        }
                        //сохраняем информацию о файле
						ProcessedReports.SaveReportInfo();
					}
				}
				dt = dt.AddHours(1);
			}
			Logger.Info(String.Format("Генерация завершена"));
		}
	}
}
