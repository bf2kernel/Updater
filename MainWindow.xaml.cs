using System;
using System.Text;
using System.Windows;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.ComponentModel;
using Ionic.Zip;



namespace BF_Launcher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string COMMON_URL = "http://195.206.232.197/"; 
        private string UPDATE_URL = "http://195.206.232.197/BF_Launcher/updates/";
        private string CheckConnection_URL = "https://google.com/"; 
        private string ARCH_TYPE = ".zip";
        private string CRITICAL_FILE = "critical.list";
        private string FULL_FILE = "full.list";
        private string VERSION_FILE = "update.version";
        private string MainDirSavePath = Directory.GetCurrentDirectory() + "\\";
        private string[] delimeter = { "|" };
        private string[] delimeterF = { "\\" };
        private string MyDocumentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        System.ComponentModel.BackgroundWorker BW_CRITICAL = new System.ComponentModel.BackgroundWorker();
        System.ComponentModel.BackgroundWorker BW_FULL = new System.ComponentModel.BackgroundWorker();
        System.ComponentModel.BackgroundWorker BW_DOWNLOAD = new System.ComponentModel.BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            CheckConnection();
            this.BW_CRITICAL.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BW_CRITICAL_DoWork);
            this.BW_FULL.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BW_FULL_DoWork);
            this.BW_DOWNLOAD.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BW_DOWNLOAD_DoWork);
        }

        // delegates

        #region /*Updater*/

        private delegate void MsgProgressDelegate(string text);
        public void WriteLog(string text)
        {
            FileStream logFS = new FileStream(MainDirSavePath + "\\update.log", FileMode.Append, FileAccess.Write);
            StreamWriter logSW = new StreamWriter(logFS, Encoding.GetEncoding("windows-1251"));
            logSW.WriteLine(text);
            logSW.Close();
            logFS.Close();
        }

        public void MsgProgress(string text)
        {
            if (!this.L_PROGRESS.Dispatcher.CheckAccess())
            {
                this.L_PROGRESS.Dispatcher.Invoke(new MsgProgressDelegate(MsgProgress), text);
            }
            else
            {
                WriteLog(text);
                this.L_PROGRESS.Text = text;
            }
        }

        private delegate void setMaxProgressFileDelegate(int val);
        private void setMaxProgressFile(int val)
        {
            if (!this.PB_FILE.Dispatcher.CheckAccess())
                this.PB_FILE.Dispatcher.Invoke(new setMaxProgressFileDelegate(setMaxProgressFile), val);
            else
                this.PB_FILE.Maximum = val;
        }

        private delegate void UpdateProgressFileDelegate(int val);
        private void UpdateProgressFile(int val)
        {
            if (!this.PB_FILE.Dispatcher.CheckAccess())
                this.PB_FILE.Dispatcher.Invoke(new UpdateProgressFileDelegate(UpdateProgressFile), val);
            else
                this.PB_FILE.Value = val;
        }

        private delegate void setMaxProgressFullDelegate(int val);
        private void setMaxProgressFull(int val)
        {
            if (!this.PB_FULL.Dispatcher.CheckAccess())
                this.PB_FULL.Dispatcher.Invoke(new setMaxProgressFullDelegate(setMaxProgressFull), val);
            else
                this.PB_FULL.Maximum = val;
        }

        private delegate void UpdateProgressFullDelegate(int val);
        private void UpdateProgressFull(int val)
        {
            if (!this.PB_FULL.Dispatcher.CheckAccess())
                this.PB_FULL.Dispatcher.Invoke(new UpdateProgressFullDelegate(UpdateProgressFull), val);
            else
                this.PB_FULL.Value = val;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //this.StartDownload();            
            this.BW_CRITICAL.RunWorkerAsync();
        }

        private void PB_FC_Click(object sender, EventArgs e)
        {
            this.BW_FULL.RunWorkerAsync();
        }

        private void CriticalUpdate()
        {
            this.BW_CRITICAL.RunWorkerAsync();
        }

        private int GetPathcVersion(string path, bool delete)
        {
            if (File.Exists(MainDirSavePath + VERSION_FILE))
            {
                //читаем его и получаем версию патча
                string VersionFileLine;
                int PatchVersion = 0;
                StreamReader VersionFile = new StreamReader(MainDirSavePath + VERSION_FILE);
                while ((VersionFileLine = VersionFile.ReadLine()) != null)
                {
                    if (VersionFileLine != "")
                    {
                        PatchVersion = Convert.ToInt16(VersionFileLine);
                    }
                }
                VersionFile.Close();

                //удаляем файл
                if (delete == true)
                    File.Delete(MainDirSavePath + VERSION_FILE);

                return PatchVersion;
            }
            return 0;
        }

        private void Update(bool Type)
        {
            if (File.Exists(MainDirSavePath + "\\update.log"))
            {
                File.Delete(MainDirSavePath + "\\update.log");
            }
            //bool Type = true;// true = critical, false = full
            string UpdFile = "";
            int PatchVersion = this.GetPathcVersion(MainDirSavePath + VERSION_FILE, true);
            //MsgProgress("Инициализируем процесс обновления");
            MsgProgress("Initialize the update process");
            if (Type)
            {
                this.Downdload(UPDATE_URL + VERSION_FILE, MainDirSavePath + VERSION_FILE, VERSION_FILE);

                int UpdPatchVersion = this.GetPathcVersion(MainDirSavePath + VERSION_FILE, false);

                if (PatchVersion == UpdPatchVersion)
                {
                    UpdateProgressFull(100);
                    //MsgProgress("Обновление не требуется");
                    MsgProgress("Update is not required");
                    return;
                }
                this.Downdload(UPDATE_URL + CRITICAL_FILE, MainDirSavePath + CRITICAL_FILE, CRITICAL_FILE);
                UpdFile = CRITICAL_FILE;
            }
            else
            {
                this.Downdload(UPDATE_URL + FULL_FILE, MainDirSavePath + FULL_FILE, FULL_FILE);
                UpdFile = FULL_FILE;
            }
            if (File.Exists(MainDirSavePath + UpdFile))
            {
                // Считаем общий прогресс
                string prefLine;
                int lCnt = 0;
                int err = 0;

                StreamReader prelFile = new StreamReader(MainDirSavePath + UpdFile);
                while ((prefLine = prelFile.ReadLine()) != null)
                {
                    lCnt++;
                }
                prelFile.Close();
                setMaxProgressFull(lCnt);


                // читаем файл обновлений
                string fLine;
                int fCnt = 0;
                Crc32 crc32 = new Crc32();
                StreamReader lFile = new StreamReader(MainDirSavePath + UpdFile);
                while ((fLine = lFile.ReadLine()) != null)
                {
                    // качаем = 1 нет = 0
                    int dl = 0;

                    UpdateProgressFile(0);

                    string[] arLine = fLine.Split(delimeter, StringSplitOptions.RemoveEmptyEntries);

                    //MsgProgress("Собираем информацию о файле: " + arLine[0]);
                    MsgProgress("Gathering information about the file: " + arLine[0]);

                    if (System.IO.File.Exists(MainDirSavePath + arLine[0]))
                    {
                        string FileHash = crc32.Get(MainDirSavePath + arLine[0]);
                        FileInfo SizecF = new FileInfo(MainDirSavePath + arLine[0]);

                        if (arLine[1] != Convert.ToString(SizecF.Length) || arLine[2] != FileHash)
                        {
                            dl = 1;
                            //MsgProgress("Файл: " + arLine[0] + " требует обновлений");
                            MsgProgress("File: " + arLine[0] + "requires updates");
                        }
                    }
                    else
                    {
                        //MsgProgress("Файл:" + arLine[0] + " не найден");
                        MsgProgress("File:" + arLine[0] + " not found");
                        dl = 1;
                    }

                    if (dl == 1)
                    {
                        //MsgProgress("Файл: " + arLine[0] + " требует обновлений");
                        //MsgProgress("Загружаем файл " + arLine[0]);
                        MsgProgress("File: " + arLine[0] + " requires updates");
                        MsgProgress("Upload files " + arLine[0]);

                        // проверяем директорию, если нету создаем
                        string ndFile = arLine[0].Replace("\\", "/");
                        string[] arDF = arLine[0].Split(delimeterF, StringSplitOptions.RemoveEmptyEntries);

                        if (!System.IO.Directory.Exists(MainDirSavePath + arDF[0]))
                        {
                            System.IO.Directory.CreateDirectory(MainDirSavePath + "\\" + arDF[0]);
                        }

                        string dfUrl = UPDATE_URL + ndFile + ARCH_TYPE;
                        string dfSP = MainDirSavePath + arLine[0] + ARCH_TYPE;

                        bool dlOk = this.Downdload(dfUrl, dfSP, arLine[0]);
                        if (dlOk == false)
                            err = 1;

                        if (dlOk)
                        {
                            // проверяем корректность скачанного файла
                            FileInfo SizecF = new FileInfo(MainDirSavePath + arLine[0] + ARCH_TYPE);
                            string zipFileHash = crc32.Get(MainDirSavePath + arLine[0] + ARCH_TYPE);

                            if (arLine[3] != Convert.ToString(SizecF.Length) && zipFileHash != arLine[4])
                            {
                                UpdateProgressFile(100);
                                //MsgProgress("Загружен не корректный файл, запускаем проверку заново.");
                                MsgProgress("Uploaded file is not correct, run the test again.");
                                this.Update(Type);
                                err = 1;
                            }
                            else
                            {
                                //MsgProgress("Распаковываем файл " + arLine[0] + ARCH_TYPE);
                                MsgProgress("Extract the file " + arLine[0] + ARCH_TYPE);
                                //бекапим старый файл
                                if (File.Exists(MainDirSavePath + arLine[0]))
                                    File.Move(MainDirSavePath + arLine[0], MainDirSavePath + arLine[0] + ".bak");

                                //распаковываем
                                //MsgProgress("Распаковываем файл " + arLine[0] + ARCH_TYPE);
                                MsgProgress("Extract the file " + arLine[0] + ARCH_TYPE);
                                Unzip(MainDirSavePath + arLine[0] + ARCH_TYPE, MainDirSavePath + "\\" + arDF[0]);

                                //Проверяем на корректность
                                FileInfo SizecNF = new FileInfo(MainDirSavePath + arLine[0]);
                                string NewFileHash = crc32.Get(MainDirSavePath + arLine[0]);

                                if (arLine[1] != Convert.ToString(SizecNF.Length) && NewFileHash != arLine[2])
                                {
                                    //MsgProgress("Файл:" + arLine[0] + " распакован не корректно");
                                    MsgProgress("File:" + arLine[0] + " unpacked incorrectly");
                                    File.Delete(MainDirSavePath + arLine[0]);
                                    File.Move(MainDirSavePath + arLine[0] + ".bak", MainDirSavePath + arLine[0]);
                                    this.Update(Type);
                                }
                                else
                                {
                                    //MsgProgress("Файл " + arLine[0] + ARCH_TYPE + "успешно распакован");
                                    MsgProgress("File " + arLine[0] + ARCH_TYPE + "successfully unpacked");
                                    File.Delete(MainDirSavePath + arLine[0] + ARCH_TYPE);
                                    File.Delete(MainDirSavePath + arLine[0] + ".bak");
                                    UpdateProgressFile(100);
                                }
                            }
                        }
                    }
                    else
                    {
                        //MsgProgress("Файл: " + arLine[0] + " не требует обновлений");
                        MsgProgress("File: " + arLine[0] + " does not require updates");
                        UpdateProgressFile(100);
                    }
                    fCnt++;
                    setMaxProgressFile(100);
                    UpdateProgressFile(100);
                    UpdateProgressFull(fCnt);
                }
                lFile.Close();

                if (err == 0)
                    //MsgProgress("Обновления успешно завершены");
                    MsgProgress("Updates successfully completed");
                else
                    //MsgProgress("При обновлении произошли проблемы");
                    MsgProgress("There have been problems during upgrading");
                if (File.Exists(MainDirSavePath + UpdFile))
                {
                    File.Delete(MainDirSavePath + UpdFile);
                }
            }
            else
            {
                //MsgProgress("Файл обновлений не найден");
                MsgProgress("Update file not found");
            }
        }

        private void BW_CRITICAL_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            Update(true);
        }

        private void BW_FULL_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            Update(false);
        }

        private void BW_DOWNLOAD_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
        }

        /* ZIP */

        public static void Unzip(string exFile, string exDir)
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(exFile))
                {
                    foreach (ZipEntry ef in zip)
                    {
                        //ef.Extract(exDir, true);  // overwrite == true  
                        zip.ExtractAll(exDir, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
                //return true;
            }
            catch
            {
                //return false;
            }
        }

        /* Downloader */
        private Stream strResponse;
        private Stream strLocal;
        private HttpWebRequest webRequest;
        private HttpWebResponse webResponse;

        private bool Downdload(string Url, string SaveFilePath, string strFile)
        {
            //Url = Url.ToLower();
            //MessageBox.Show(Url);
            try
            {
                int startPointInt = 0;
                if (File.Exists(SaveFilePath))
                {
                    startPointInt = Convert.ToInt32(new FileInfo(SaveFilePath).Length);
                }

                webRequest = (HttpWebRequest)WebRequest.Create(Url);
                webRequest.AddRange(startPointInt);
                webRequest.Credentials = CredentialCache.DefaultCredentials;
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                Int64 fileSize = webResponse.ContentLength;

                strResponse = webResponse.GetResponseStream();

                if (startPointInt == 0)
                {
                    try
                    {
                        strLocal = new FileStream(SaveFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    }
                    catch
                    {
                        MessageBox.Show("Critical Error. Exit.");
                        this.Close();
                    }
                }
                else
                {
                    strLocal = new FileStream(SaveFilePath, FileMode.Append, FileAccess.Write, FileShare.None);
                }

                int bytesSize = 0;
                byte[] downBuffer = new byte[2048];

                setMaxProgressFile(Convert.ToInt32(fileSize) + 20 + startPointInt);

                //UpdateProgressBarFile
                while ((bytesSize = strResponse.Read(downBuffer, 0, downBuffer.Length)) > 0)
                {
                    strLocal.Write(downBuffer, 0, bytesSize);
                    UpdateProgressFile(Convert.ToInt32(strLocal.Length + 20));

                    int PercentProgress = Convert.ToInt32((strLocal.Length * 100) / fileSize);
                    //string textDownloadProgress = "Файл " + strFile + ": загружено " + strLocal.Length + " из " + (fileSize + startPointInt) + " (" + PercentProgress + "%)";
                    string textDownloadProgress = "File " + strFile + ": loaded " + strLocal.Length + " from " + (fileSize + startPointInt) + " (" + PercentProgress + "%)";

                    MsgProgress(textDownloadProgress);
                }
                strResponse.Close();
                strLocal.Close();
            }
            catch (WebException exception)
            {
                if (exception.Status == WebExceptionStatus.ProtocolError && exception.Message.Contains("416"))
                {
                    //MsgProgress("Файл уже полностью загружен");
                    MsgProgress("The file is already fully loaded");
                    return true;
                }
                else
                {
                    if (((HttpWebResponse)exception.Response).StatusCode.ToString() == "NotFound")
                    {
                        //MsgProgress("Файл не найден на сервере обновлений. Код ошибки:" + ((HttpWebResponse)exception.Response).StatusCode.ToString());
                        MsgProgress("File not found on the update server. Error code:" + ((HttpWebResponse)exception.Response).StatusCode.ToString());
                    }
                    else
                    {
                        //MsgProgress("Проблемы при загрузке файла. Код ошибки:" + ((HttpWebResponse)exception.Response).StatusCode.ToString());
                        MsgProgress("Problems while downloading the file. Error code:" + ((HttpWebResponse)exception.Response).StatusCode.ToString());
                        Downdload(Url, SaveFilePath, "");
                    }
                    return false;
                }

            }
            return true;
        }
        /* End Downdloader */

        #endregion

        #region/*Проверяем подключение к интернету*/

        private void CheckConnection()
        {
            // проверка соединения с Интернет   
            ConnectionAvailableExternal(CheckConnection_URL).ToString();
            //ConnectionAvailable(UPDATE_URL).ToString();
        }

        public bool ConnectionAvailable(string strServer)
        {
            try
            {
                HttpWebRequest reqFP = (HttpWebRequest)HttpWebRequest.Create(strServer);

                HttpWebResponse rspFP = (HttpWebResponse)reqFP.GetResponse();
                if (HttpStatusCode.OK == rspFP.StatusCode)
                {
                    // HTTP = 200 - Интернет безусловно есть! 
                    rspFP.Close();
                    return true;
                }
                else
                {
                    // сервер вернул отрицательный ответ, возможно что инета нет
                    MessageBox.Show("Соединение с сервером не может быть установлено. Сервер временно недоступен в связи с нарушением его работы, техническими работами либо загрузки обновлений. Приносим извинения за временные неудобства.");
                    rspFP.Close();
                    return false;
                }
            }
            catch (WebException)
            {
                // Ошибка, значит интернета у нас нет. Плачем :'(
                MessageBox.Show("Соединение с сервером не может быть установлено. Сервер временно недоступен в связи с нарушением его работы, техническими работами либо загрузки обновлений. Приносим извинения за временные неудобства.");
                return false;
            }
        }

        public bool ConnectionAvailableExternal(string strServerExternal)
        {
            try
            {
                HttpWebRequest reqFPExternal = (HttpWebRequest)HttpWebRequest.Create(strServerExternal);

                HttpWebResponse rspFPExternal = (HttpWebResponse)reqFPExternal.GetResponse();
                if (HttpStatusCode.OK == rspFPExternal.StatusCode)
                {
                    // HTTP = 200 - Интернет безусловно есть! 
                    rspFPExternal.Close();
                    ConnectionAvailable(COMMON_URL).ToString(); /*Вторая проверка тут*/
                    return true;
                }
                else
                {
                    // сервер вернул отрицательный ответ, возможно что инета нет
                    MessageBox.Show("Интернет соединение не может быть установлено, проверьте подключение к интернету.");
                    rspFPExternal.Close();
                    return false;
                }
            }
            catch (WebException)
            {
                // Ошибка, значит интернета у нас нет. Плачем :'(
                MessageBox.Show("Интернет соединение не может быть установлено, проверьте подключение к интернету.");
                return false;
            }
        }

        #endregion

        #region/*Закрыть*/

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(MainDirSavePath + "//BF_Launcher.exe"))
            {
                ProcessStartInfo startbf2 = new ProcessStartInfo();
                startbf2.FileName = "BF_Launcher.exe";
                Process.Start(startbf2);
            }
            else
            {
                //MessageBox.Show("Файл bf2.exe не найден!");
                MessageBox.Show("Что то пошло не так. Файл BF_Launcher.exe не найден!");
            }
            this.Close();
        }

        #endregion

        #region/*Update Launcher*/

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            this.BW_FULL.RunWorkerAsync();
        }

        #endregion
    }
}