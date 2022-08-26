using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeWord = Microsoft.Office.Interop.Word;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;



namespace Diplom_
{
    public partial class Student : Form
    {
        object user_id;
        string grp;
        string connectionString = ConfigurationManager.ConnectionStrings["StudyConnection"].ConnectionString;
        string filename1;
        string filename2;
        string filename3;
        string user_name;

        List<File_Lectures> filesLectures;
        List<File_Laboratory> filesLabManuals;
        List<File_Laboratory> filesLabReports;
        List<File_Laboratory> filesTestReports;

        public class File
        {
            public File(int id, string filename, byte[] data)
            {
                Id = id;
                FileName = filename;
                Data = data;
            }

            public File(int id, string filename, byte[] data, string type)
            {
                Id = id;
                FileName = filename;
                Data = data;
                Type = type;
            }

            public File(int id, string filename, byte[] data, int user_id, string status)
            {
                Id = id;
                FileName = filename;
                Data = data;
                UserId = user_id;
                Status = status;
            }

            public File()
            {
                Id = 0;
                FileName = "";
                Data = null;
                UserId = 0;
                Status = "";
                Type = "";
            }
            public int Id { get; private set; }
            public string FileName { get; private set; }
            public byte[] Data { get; private set; }
            public int UserId { get; private set; }
            public string Status { get; private set; }
            public string Type { get; private set; }
        }

        public class File_Lectures
        {
            public File_Lectures(int id, int user_id, string file_name, byte[] data, string subject, string theme, string type)
            {
                Id = id;
                UserId = user_id;
                FileName = file_name;
                FileData = data;
                Subject = subject;
                Theme = theme;
                Type = type;
            }

            public File_Lectures()
            {
                Id = 0;
                UserId = 0;
                FileName = "";
                FileData = null;
                Subject = "";
                Theme = "";
                Type = "";
            }
            public int Id { get; private set; }
            public int UserId { get; private set; }
            public string FileName { get; private set; }
            public byte[] FileData { get; private set; }
            public string Subject { get; private set; }
            public string Theme { get; private set; }
            public string Type { get; private set; }
        }

        public class File_Laboratory
        {
            public File_Laboratory(int id, int user_id, string file_name, byte[] data, string subject, string theme, string type, string status)
            {
                Id = id;
                UserId = user_id;
                FileName = file_name;
                FileData = data;
                Subject = subject;
                Theme = theme;
                WorkType = type;
                Status = status;
            }

            public File_Laboratory()
            {
                Id = 0;
                UserId = 0;
                FileName = "";
                FileData = null;
                Subject = "";
                Theme = "";
                WorkType = "";
                Status = "";
            }
            public int Id { get; private set; }
            public int UserId { get; private set; }
            public string FileName { get; private set; }
            public byte[] FileData { get; private set; }
            public string Subject { get; private set; }
            public string Theme { get; private set; }
            public string WorkType { get; private set; }
            public string Status { get; private set; }
        }

        public Student(object id)
        {
            InitializeComponent();
            user_id = id;
            tabControl1.TabPages[0].Text = "Главная";
            tabControl1.TabPages[1].Text = "Лекции";
            tabControl1.TabPages[2].Text = "Лабораторные работы";
            tabControl1.TabPages[3].Text = "Контрольные работы";
            tabControl1.TabPages[4].Text = "К зачёту и экзаменам";
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.PerformStep();
        }

        private bool CompareFile(string Path1, string Path2)
        {
            //compare files byte to byte

            int file1byte;
            int file2byte;

            FileStream fs1 = new FileStream(Path1, FileMode.Open);
            FileStream fs2 = new FileStream(Path2, FileMode.Open);

            do
            {
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            fs1.Close();
            fs2.Close();

            return ((file1byte - file2byte) == 0);
        }

        private string CompareFileWord(string Path1, string Path2, string compareFileFolder, ProgressBar pBar)
        {
            //create Word application
            var app = new OfficeWord.Application();
            app.DisplayAlerts = OfficeWord.WdAlertLevel.wdAlertsNone;
            object missing = System.Reflection.Missing.Value;
            object readOnly = false;
            object AddToRecent = false;
            object Visible = true;

            try
            {
                //try open signed file 
                OfficeWord.Document docZero = app.Documents.Open(Path2, ref missing, ref readOnly, ref AddToRecent, Visible: ref Visible);

                docZero.Final = false;
                docZero.TrackRevisions = true;
                docZero.ShowRevisions = true;
                docZero.PrintRevisions = true;

                //compare file from card and signed file
                docZero.Compare(Path1, missing, OfficeWord.WdCompareTarget.wdCompareTargetCurrent, true, false, false, false, false);
                string name = System.IO.Path.GetFileName(Path1);
                string fileName = compareFileFolder + "_verified_" + name;

                //save file of compare
                docZero.SaveAs2(fileName);
                docZero.Close();
                app.Quit();

                pBar.Value = pBar.Maximum;
                return fileName;
            }

            catch
            {
                pBar.Value = pBar.Maximum;
                app.Quit();
                return "";
            }
        }

        private string Take_Compare(string filePath)
        {
            string resultText = "";
            using (WordprocessingDocument wordprocDoc = WordprocessingDocument.Open(filePath, true))
            {
                Body body = wordprocDoc.MainDocumentPart.Document.Body;
                //take each paragraph which contain text
                IEnumerable<Paragraph> paragraphs = body.Elements<Paragraph>().Where(paragrahp => paragrahp.InnerText != "");
                List<Paragraph> paragraphsList = paragraphs.ToList();
                StringBuilder csvResult = new StringBuilder();
                string fileName = Path.GetFileName(filePath);

                int delFlag = 0;
                string text = "";
                for (var i = 0; i < paragraphsList.Count(); i++)
                {
                    //take paragraph which have local name "del"(this string contain change text)
                    if (paragraphsList[i].ChildElements.Where(child => child.LocalName == "del").Count() != 0)
                    {
                        // if paragraph before not "del" paragraph, add as context 
                        if (delFlag == 0)
                        {
                            if (i > 0)
                            {
                                text = paragraphsList[i - 1].InnerText;
                            }

                            delFlag = 1;
                        }

                        //take text from "del" paragraph

                        foreach (OpenXmlElement child in paragraphsList[i].ChildElements)
                        {

                            if (child.LocalName == "del")
                            {
                                text = text + " <del> " + child.InnerText + " </del> ";
                            }
                            else
                            {
                                text = text + child.InnerText;
                            }
                        }
                    }
                    else
                    {
                        //if before usual paragraph was "del" paragraph, add this paragraph and finish process
                        if (delFlag == 1)
                        {
                            text = text + paragraphsList[i].InnerText;
                            string newLine = string.Format("{0};{1}", fileName, text).Replace("</del>  <del>", " ");
                            csvResult.AppendLine(newLine);
                            delFlag = 0;
                            text = "";
                        }
                    }
                }
                if (delFlag == 1)
                {
                    //if file end on usual paragraph, after paragraph with "del"
                    text = text + paragraphsList[paragraphsList.Count() - 1].InnerText;
                    string newLine = string.Format("{0};{1}", fileName, text).Replace("</del>  <del>", " ");
                    csvResult.AppendLine(newLine);
                    delFlag = 0;
                    text = "";
                }
                wordprocDoc.Close();

                //delete carriage return symbol
                if (csvResult.ToString().Length > 2)
                {
                    resultText = csvResult.ToString().Substring(0, csvResult.ToString().Length - 2);
                }
                else
                {
                    resultText = csvResult.ToString();
                }


            }
            return resultText;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {

            // Приветствуем пользователя
            string fam = "";
            string nam = "";
            string pat = "";
            string sqlExpression_1 = "SELECT last_name, first_name, patronymic FROM Users WHERE id = '" + $"{user_id}" + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression_1, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // Построчно считываем данные
                    {
                        fam = reader.GetString(0);
                        nam = reader.GetString(1);
                        pat = reader.GetString(2);
                    }
                }
            }
            fam = fam.Trim();
            nam = nam.Trim();
            pat = pat.Trim();
            label2.Text = "Добро пожаловать, " + fam + ' ' + nam + ' ' + pat + '!';
            user_name = fam + ' ' + nam[0] + ". " + pat[0] + '.';
            grp = "";

            // Настраиваем колонки
            listView1.GridLines = true;
            listView1.View = System.Windows.Forms.View.Details;
            listView2.GridLines = true;
            listView2.View = System.Windows.Forms.View.Details;
            listView3.GridLines = true;
            listView3.View = System.Windows.Forms.View.Details;
            listView4.GridLines = true;
            listView4.View = System.Windows.Forms.View.Details;
            listView5.GridLines = true;
            listView5.View = System.Windows.Forms.View.Details;
            listView6.GridLines = true;
            listView6.View = System.Windows.Forms.View.Details;

            // Добавляем названия предметов в ComboBox
            AddSubjectsToCmb(comboBox3, "SubjectsOfGroups", user_id);
            AddSubjectsToCmb(comboBox5, "SubjectsOfGroups", user_id);
            AddSubjectsToCmb(comboBox9, "SubjectsOfGroups", user_id);
            AddSubjectsToCmb(comboBox14, "SubjectsOfGroups", user_id);
        }

        // Функция, добавляющая студентов в ComboBox
        private async void ListOfGroupsLoad(ComboBox cmb, string subject)
        {
            cmb.Items.Clear();
            string sqlExpression = "SELECT group_name FROM SubjectsOfGroups WHERE subject = '" + subject + "'";
            string sqlResult = "";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        cmb.Items.Add(sqlResult);
                    }
                }
            }
        }

        private List<int> ListOfStudentsLoad(ComboBox cmb, string group)
        {
            cmb.Items.Clear();
            List<int> usersID = new List<int>();
            string sqlExpression = "SELECT id FROM Users WHERE user_group = '" + group + "'";
            int sqlResult;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sqlResult = reader.GetInt32(0);
                        usersID.Add(sqlResult);
                        cmb.Items.Add(GetUserName(sqlResult, "Users"));
                    }
                }
            }
            return usersID;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox3.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите дисциплину!");
            }
            if (subject != "")
            {
                AddLection newForm = new AddLection(user_id, subject);
                newForm.Show();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int num = 0;
            int id = 0;
            try
            {
                num = listView1.SelectedIndices[0];
                id = filesLectures[num].Id;
            }
            catch { }
            if (id > 0)
                SaveFile("Lectures", id);
        }

        // Функция для сохранения выбранного файла по id
        private async void SaveFile(string table, int id_)
        {
            File_Lectures file = new File_Lectures();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE id = '" + id_.ToString() + "'";
                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = (byte[])reader.GetValue(3);
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string type = reader.GetString(6);

                        file = new File_Lectures(id, user_id, file_name, data, subject, theme, type);
                    }
                }
            }
            if (file.FileData != null)
            {
                using (FileStream fs = new FileStream(file.FileName, FileMode.OpenOrCreate))
                {
                    fs.Write(file.FileData, 0, file.FileData.Length);
                    MessageBox.Show($"Файл \"{file.FileName}\" \nсохранен!");
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFile(listView1);
        }

        // Функция для открытия выбранного файла
        private void OpenFile(ListView listV)
        {
            string f_name = listV.SelectedItems[0].Text;
            var exePath = AppDomain.CurrentDomain.BaseDirectory; //path to exe file
            var path = Path.Combine(exePath, f_name);
            try
            {
                Process.Start(path);
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии файла.\nНеобходимо загрузить данный файл!");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ScrollUp(listView1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ScrollDown(listView1);
        }

        // Функция "Листать вверх"
        private void ScrollUp(ListView listV)
        {
            int numberOfItems = listV.Items.Count;
            if ((numberOfItems > 0))
            {
                if (listV.SelectedItems.Count != 0)
                {
                    int currentIndex = listV.SelectedItems[0].Index;
                    if (currentIndex > 0)
                    {
                        listV.Items[currentIndex].Selected = false;
                        listV.Items[currentIndex - 1].Selected = true;
                    }
                }
                else
                    listV.Items[0].Selected = true;
            }
        }

        // Функция "Листать вниз"
        private void ScrollDown(ListView listV)
        {
            int numberOfItems = listV.Items.Count;
            if ((numberOfItems > 0))
            {
                if (listV.SelectedItems.Count != 0)
                {
                    int currentIndex = listV.SelectedItems[0].Index;
                    if (currentIndex < numberOfItems - 1)
                    {
                        listV.Items[currentIndex].Selected = false;
                        listV.Items[currentIndex + 1].Selected = true;
                    }
                }
                else
                    listV.Items[0].Selected = true;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string additional = "";

            string subject = "";
            try
            {
                subject = comboBox3.SelectedItem.ToString();
            }
            catch { }

            int num = 0;
            int id = 0;
            string file_name = "";
            try
            {
                num = listView1.SelectedIndices[0];
                id = filesLectures[num].Id;
                file_name = filesLectures[num].FileName;
            }
            catch { }

            string theme = "";

            try
            {
                theme = comboBox4.SelectedItem.ToString();
                additional += " AND theme = '" + theme + "'";
            }
            catch { }

            if (id > 0)
            {
                DeleteFile("Lectures", id);
                MessageBox.Show($"Файл \"{file_name}\" удалён!");
                lvi_Lectures_Update(listView1, "Lectures", subject, additional);
            }
        }

        // Функция для удаления методического материала
        private void DeleteFile(string table, int id)
        {
            string sqlExpression = "DELETE FROM " + table + " WHERE id ='" + id + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();
            }
        }

        // Функция обновления данных в listView (для лекций) - сортировка по дисциплине
        private async void lvi_Lectures_Update(ListView listV, string table, string subject_)
        {
            filesLectures = new List<File_Lectures>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string type = reader.GetString(6);

                        File_Lectures file = new File_Lectures(id, user_id, file_name, data, subject, theme, type);
                        filesLectures.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesLectures.Count > 0)
            {
                for (int i = 0; i < filesLectures.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesLectures[i].FileName;
                    lvi.ImageIndex = 0;
                    if (filesLectures[i].Type == "lection")
                    {
                        lvi.SubItems.Add("Лекция");
                    }
                    if (filesLectures[i].Type == "manual")
                    {
                        lvi.SubItems.Add("Доп. материал");
                    }
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        // Функция обновления данных в listView (для лекций) - сортировка по дисциплине и доп параметру
        private async void lvi_Lectures_Update(ListView listV, string table, string subject_, string additional)
        {
            filesLectures = new List<File_Lectures>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "'" + additional;

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string type = reader.GetString(6);

                        File_Lectures file = new File_Lectures(id, user_id, file_name, data, subject, theme, type);
                        filesLectures.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesLectures.Count > 0)
            {
                for (int i = 0; i < filesLectures.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesLectures[i].FileName;
                    lvi.ImageIndex = 0;
                    if (filesLectures[i].Type == "lection")
                    {
                        lvi.SubItems.Add("Лекция");
                    }
                    if (filesLectures[i].Type == "manual")
                    {
                        lvi.SubItems.Add("Доп. материал");
                    }
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox5.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите дисциплину!");
            }

            string theme = "";
            try
            {
                theme = comboBox7.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите работу!");
            }

            if ((subject != "") && (theme != ""))
            {
                AddMaterial("LabWorks", subject, theme, "laba");
                lvi_LabReports_ComboBoxes_Update();
            }
        }

        // Функция для добавления отчёта по лабораторным
        private async void AddMaterial(string table, string subject, string theme, string type)
        {
            // Добавляем новый файл
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.CommandText = @"INSERT INTO " + table + " VALUES (@user_id, @file_name, @file_data, @subject, @theme, @work_type, @status)";
                command.Parameters.Add("@user_id", SqlDbType.Int);
                command.Parameters.Add("@file_name", SqlDbType.NVarChar, 300);
                command.Parameters.Add("@subject", SqlDbType.NVarChar, 100);
                command.Parameters.Add("@theme", SqlDbType.NVarChar, 300);
                command.Parameters.Add("@work_type", SqlDbType.NVarChar, 100);
                command.Parameters.Add("@status", SqlDbType.NVarChar, 100);

                string filename = "";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        filename = openFileDialog1.FileName;
                    }
                    catch { }
                }

                if (filename != "")
                {
                    // Получаем короткое имя файла для сохранения в бд
                    string shortFileName = filename.Substring(filename.LastIndexOf('\\') + 1);

                    // Массив для хранения бинарных данных файла
                    byte[] fileData;
                    using (FileStream fs = new FileStream(filename, FileMode.Open))
                    {
                        fileData = new byte[fs.Length];
                        fs.Read(fileData, 0, fileData.Length);
                        command.Parameters.Add("@file_data", SqlDbType.VarBinary, Convert.ToInt32(fs.Length));
                    }

                    // Передаем данные в команду через параметры
                    command.Parameters["@user_id"].Value = user_id;
                    command.Parameters["@file_name"].Value = shortFileName;
                    command.Parameters["@file_data"].Value = fileData;
                    command.Parameters["@subject"].Value = subject;
                    command.Parameters["@theme"].Value = theme;
                    command.Parameters["@work_type"].Value = "otchet";
                    if (type == "laba")
                        command.Parameters["@status"].Value = "not_viewed";
                    if (type == "test")
                        command.Parameters["@status"].Value = "";

                    await command.ExecuteNonQueryAsync();
                    MessageBox.Show($"Файл \"{shortFileName}\"\nдобавлен!");
                }
            }
        }

        // Функция для добавления методического материала
        private async void AddManual(ListView listV, string table, string subject)
        {
            string selectedItem = subject;

            // Добавляем новый файл
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.CommandText = @"INSERT INTO " + table + " VALUES (@file_name, @file_data, @subject)";
                command.Parameters.Add("@file_name", SqlDbType.NVarChar, 300);
                command.Parameters.Add("@subject", SqlDbType.NVarChar, 100);

                string filename = "";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    filename = openFileDialog1.FileName;
                }

                // Получаем короткое имя файла для сохранения в бд
                string shortFileName = filename.Substring(filename.LastIndexOf('\\') + 1); // forest.jpg

                // Массив для хранения бинарных данных файла
                byte[] fileData;
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    fileData = new byte[fs.Length];
                    fs.Read(fileData, 0, fileData.Length);
                    command.Parameters.Add("@file_data", SqlDbType.VarBinary, Convert.ToInt32(fs.Length));
                }
                // Передаем данные в команду через параметры
                command.Parameters["@file_name"].Value = shortFileName;
                command.Parameters["@file_data"].Value = fileData;
                command.Parameters["@subject"].Value = selectedItem;

                await command.ExecuteNonQueryAsync();
                MessageBox.Show($"Файл \"{shortFileName}\"\nдобавлен!");
            }
            lvi_1_Update(listV, table, subject);
        }

        // Функция для обновления данных в listView типа 1
        private async void lvi_1_Update(ListView listV, string table, string subject)
        {
            listV.Items.Clear();

            List<File> files = new List<File>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT id, file_name FROM " + table + " WHERE subject = '" + subject + "'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        string filename = reader.GetString(1);
                        byte[] data = null;// (byte[])reader.GetValue(2);

                        File file_1 = new File(id, filename, data);
                        files.Add(file_1);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (files.Count > 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = files[i].FileName;
                    lvi.ImageIndex = 0;
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        // Обновляем значения в ListView (для заданий к лабораторным) - сортировка по дисциплине
        private async void lvi_LabManuals_Update(ListView listV, string table, string subject_)
        {
            filesLabManuals = new List<File_Laboratory>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "' AND work_type = 'zadanie'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string work_type = reader.GetString(6);
                        string status = "";

                        File_Laboratory file = new File_Laboratory(id, user_id, file_name, data, subject, theme, work_type, status);
                        filesLabManuals.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesLabManuals.Count > 0)
            {
                for (int i = 0; i < filesLabManuals.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesLabManuals[i].FileName;
                    lvi.ImageIndex = 0;
                    lvi.SubItems.Add(filesLabManuals[i].Theme);
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        // Обновляем значения в ListView (для заданий к лабораторным) - сортировка по дисциплине и дополнительному фильтру
        private async void lvi_LabManuals_Update(ListView listV, string table, string subject_, string aditional)
        {
            filesLabManuals = new List<File_Laboratory>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "' AND work_type = 'zadanie'" + aditional;

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string work_type = reader.GetString(6);
                        string status = "";

                        File_Laboratory file = new File_Laboratory(id, user_id, file_name, data, subject, theme, work_type, status);
                        filesLabManuals.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesLabManuals.Count > 0)
            {
                for (int i = 0; i < filesLabManuals.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesLabManuals[i].FileName;
                    lvi.ImageIndex = 0;
                    lvi.SubItems.Add(filesLabManuals[i].Theme);
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        // Обновляем значения в ListView (для отчётов студентов) - сортировка по предмету
        private async void lvi_LabReports_Update(ListView listV, string table, string subject_)
        {
            filesLabReports = new List<File_Laboratory>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "' AND work_type = 'otchet' AND user_id = '" + user_id.ToString() + "'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string work_type = reader.GetString(6);
                        string status = reader.GetString(7); ;

                        File_Laboratory file = new File_Laboratory(id, user_id, file_name, data, subject, theme, work_type, status);
                        filesLabReports.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesLabReports.Count > 0)
            {
                for (int i = 0; i < filesLabReports.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesLabReports[i].FileName;
                    lvi.ImageIndex = 0;
                    string fio = GetUserName(filesLabReports[i].UserId, "Users");
                    string engStatus = filesLabReports[i].Status;
                    string status = "";
                    if (engStatus == "not_viewed")
                    {
                        status = "Не просмотрен";
                    }
                    else if (engStatus == "credited")
                    {
                        status = "Зачтён";
                    }
                    else if (engStatus == "commented")
                    {
                        status = "Есть замечания";
                    }
                    lvi.SubItems.Add(fio);
                    lvi.SubItems.Add(status);
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        // Обновляем значения в ListView (для отчётов студентов) - сортировка по предмету и доп. параметру
        private async void lvi_LabReports_Update(ListView listV, string table, string subject_, string additional)
        {
            filesLabReports = new List<File_Laboratory>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "' AND work_type = 'otchet'" + additional;

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string work_type = reader.GetString(6);
                        string status = reader.GetString(7); ;

                        File_Laboratory file = new File_Laboratory(id, user_id, file_name, data, subject, theme, work_type, status);
                        filesLabReports.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesLabReports.Count > 0)
            {
                for (int i = 0; i < filesLabReports.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesLabReports[i].FileName;
                    lvi.ImageIndex = 0;
                    string fio = GetUserName(filesLabReports[i].UserId, "Users");
                    string engStatus = filesLabReports[i].Status;
                    string status = "";
                    if (engStatus == "not_viewed")
                    {
                        status = "Не просмотрен";
                    }
                    else if (engStatus == "credited")
                    {
                        status = "Зачтён";
                    }
                    else if (engStatus == "commented")
                    {
                        status = "Есть замечания";
                    }
                    lvi.SubItems.Add(fio);
                    lvi.SubItems.Add(status);
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        // Обновляем значения в ListView (для отчётов студентов) - сортировка по предмету и доп. параметру
        private List<File_Laboratory> lvi_TestReports_Update(ListView listV, string table, string subject_, string additional)
        {
            List<File_Laboratory> filesTestReports = new List<File_Laboratory>();

            listV.Items.Clear();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.OpenAsync();
                string sql = "SELECT * FROM " + table + " WHERE subject = '" + subject_ + "' AND user_id = '" + user_id.ToString() + "' AND work_type = 'otchet'" + additional;

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        int user_id = reader.GetInt32(1);
                        string file_name = reader.GetString(2);
                        byte[] data = null;
                        string subject = reader.GetString(4);
                        string theme = reader.GetString(5);
                        string work_type = reader.GetString(6);
                        string status = reader.GetString(7); ;

                        File_Laboratory file = new File_Laboratory(id, user_id, file_name, data, subject, theme, work_type, status);
                        filesTestReports.Add(file);
                    }
                }
            }
            // Поместим в ListView файлы из получившегося списка
            if (filesTestReports.Count > 0)
            {
                for (int i = 0; i < filesTestReports.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = filesTestReports[i].FileName;
                    lvi.ImageIndex = 0;
                    string fio = GetUserName(filesTestReports[i].UserId, "Users");
                    string status = filesTestReports[i].Status;
                    lvi.SubItems.Add(fio);
                    lvi.SubItems.Add(status);
                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
            return filesTestReports;
        }

        // Функция, которая определяем Фамилию И.О. по id пользователя
        private string GetUserName(int user_id, string table)
        {
            string fam = "";
            string nam = "";
            string pat = "";

            string sqlExpression = "SELECT last_name, first_name, patronymic FROM " + table + " WHERE id = '" + user_id.ToString() + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) // Построчно считываем данные
                    {
                        fam = reader.GetString(0);
                        nam = reader.GetString(1);
                        pat = reader.GetString(2);
                    }
                }
            }
            fam = fam.Trim();
            nam = nam.Trim();
            pat = pat.Trim();

            string user_name = fam + ' ' + nam[0] + ". " + pat[0] + '.';
            return user_name;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            ScrollUp(listView2);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            ScrollUp(listView3);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            ScrollDown(listView2);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            ScrollDown(listView3);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            int num = -1;
            int id = 0;
            try
            {
                num = listView3.SelectedIndices[0];
            }
            catch { }
            if (num > -1)
            {
                id = filesLabReports[num].Id;
                ChangeStatus("LabWorks", id, "credited");
                lvi_LabReports_ComboBoxes_Update();
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            int num = -1;
            int id = 0;
            try
            {
                num = listView3.SelectedIndices[0];
            }
            catch { }
            if (num > -1)
            {
                id = filesLabReports[num].Id;
                ChangeStatus("LabWorks", id, "not_viewed");
                lvi_LabReports_ComboBoxes_Update();
            }
        }

        // Функция, которая отмечает, что работа проверена
        private async void ViewedWork(ListView listV, string table, string subject)
        {

            try
            {
                string f_name = listV.SelectedItems[0].Text;
                File find = new File();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "UPDATE " + table + " SET status = 'viewed' WHERE file_name = '" + f_name + "'";
                    SqlCommand command = new SqlCommand(sql, connection);
                    int number = command.ExecuteNonQuery();
                    MessageBox.Show("Обновлена " + number.ToString() + " строка.");
                }
                lvi_LabReports_Update(listV, table, subject);
            }
            catch
            {
                MessageBox.Show("Выберите работу!");
            }
        }

        // Функция, которая отмечает, что работа проверена
        private void ChangeStatus(string table, int work_id, string status)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.OpenAsync();
                string sql = "UPDATE " + table + " SET status = '" + status + "' WHERE id = '" + work_id + "'";
                SqlCommand command = new SqlCommand(sql, connection);
                int number = command.ExecuteNonQuery();
            }
        }

        // Функция, которая отмечает, что работа не проверена
        private async void NotViewedWork(ListView listV, string table, string subject)
        {
            try
            {
                string f_name = listV.SelectedItems[0].Text;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "UPDATE " + table + " SET status = 'not_viewed' WHERE file_name = '" + f_name + "'";
                    SqlCommand command = new SqlCommand(sql, connection);
                    int number = command.ExecuteNonQuery();
                    MessageBox.Show("Обновлена " + number.ToString() + " строка.");
                }
                lvi_LabReports_Update(listV, table, subject);
            }
            catch
            {
                MessageBox.Show("Выберите работу!");
            }
        }

        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFile(listView2);
        }

        private void listView3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFile(listView3);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            int num = 0;
            int id = 0;
            try
            {
                num = listView2.SelectedIndices[0];
                id = filesLabManuals[num].Id;
            }
            catch { }
            if (id > 0)
                SaveFile("LabWorks", id);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            int num = 0;
            int id = 0;
            try
            {
                num = listView3.SelectedIndices[0];
                id = filesLabReports[num].Id;
            }
            catch { }
            if (id > 0)
                SaveFile("LabWorks", id);
        }

        // Найдём непроверенные работы
        private void button18_Click(object sender, EventArgs e)
        {
            string additional = " AND status = 'not_viewed'";
            string subject = "";
            try
            {
                subject = comboBox5.SelectedItem.ToString();
            }
            catch { }

            additional += " AND user_id = '" + user_id.ToString() + "'";

            string theme = "";
            try
            {
                theme = comboBox7.SelectedItem.ToString();
            }
            catch { }
            if (theme != "")
                additional += " AND theme = '" + theme + "'";

            if (subject != "")
            {
                lvi_LabReports_Update(listView3, "LabWorks", subject, additional);
            }
        }

        private async void NotViewedWorks(ListView listV, string table)
        {
            listV.Items.Clear();

            List<File> files = new List<File>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT id, file_name, user_id, status FROM " + table + " WHERE work_type = 'otchet' AND subject = '" + grp + "' AND status = 'not_viewed'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        string filename = reader.GetString(1);
                        byte[] data = null;
                        int userid = reader.GetInt32(2);
                        string status = reader.GetString(3);

                        File file_1 = new File(id, filename, data, userid, status);
                        files.Add(file_1);
                    }
                }
            }
            // Поместим в ListView файлы из списка
            if (files.Count > 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = files[i].FileName; // 1 колонка
                    lvi.ImageIndex = 0;

                    string l_n = "";
                    string f_n = "";
                    string pat = "";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string sql = "SELECT last_name, first_name, patronymic FROM Users WHERE id = '" + files[i].UserId.ToString() + "'";

                        SqlCommand command = new SqlCommand(sql, connection);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                l_n = reader.GetString(0);
                                f_n = reader.GetString(1);
                                pat = reader.GetString(2);
                            }
                        }
                    }
                    l_n = l_n.Trim();
                    f_n = f_n.Trim();
                    pat = pat.Trim();

                    string fio = l_n + ' ' + f_n[0] + ". " + pat[0] + '.';

                    lvi.SubItems.Add(fio); // 2 колонка

                    string st = files[i].Status.Trim();
                    if (st == "not_viewed")
                        lvi.SubItems.Add("Не просмотрен"); // 3 колонка
                    if (st == "viewed")
                        lvi.SubItems.Add("Проверен");

                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        private async void SelectStudent(ListView listV, ComboBox cmb, string table)
        {
            listV.Items.Clear();

            List<File> files = new List<File>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT id, file_name, user_id, status FROM " + table + " WHERE work_type = 'otchet' AND subject = '" + grp + "' AND user_id = '" + user_id.ToString() + "'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        string filename = reader.GetString(1);
                        byte[] data = null;
                        int userid = reader.GetInt32(2);
                        string status = reader.GetString(3);

                        File file_1 = new File(id, filename, data, userid, status);
                        files.Add(file_1);
                    }
                }
            }
            // Поместим в ListView файлы из списка
            if (files.Count > 0)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = files[i].FileName; // 1 колонка
                    lvi.ImageIndex = 0;

                    string l_n = "";
                    string f_n = "";
                    string pat = "";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string sql = "SELECT last_name, first_name, patronymic FROM Users WHERE id = '" + files[i].UserId.ToString() + "'";

                        SqlCommand command = new SqlCommand(sql, connection);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                l_n = reader.GetString(0);
                                f_n = reader.GetString(1);
                                pat = reader.GetString(2);
                            }
                        }
                    }
                    l_n = l_n.Trim();
                    f_n = f_n.Trim();
                    pat = pat.Trim();

                    string fio = l_n + ' ' + f_n[0] + ". " + pat[0] + '.';

                    lvi.SubItems.Add(fio); // 2 колонка

                    string st = files[i].Status.Trim();
                    if (st == "not_viewed")
                        lvi.SubItems.Add("Не просмотрен"); // 3 колонка
                    if (st == "viewed")
                        lvi.SubItems.Add("Проверен");

                    listV.Items.Add(lvi);
                }
                listV.Items[0].Selected = true;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            lvi_LabReports_ComboBoxes_Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int num = 0;
            int id = 0;
            string subject = "";
            try
            {
                subject = comboBox5.SelectedItem.ToString();
            }
            catch { }
            string file_name = "";
            try
            {
                num = listView3.SelectedIndices[0];
                id = filesLabReports[num].Id;
                file_name = filesLabReports[num].FileName;
            }
            catch { }
            string theme = "";
            string additional = "";
            try
            {
                theme = comboBox7.SelectedItem.ToString();
                additional = " AND theme = '" + theme + "'";

            }
            catch { }
            if ((id > 0) && (subject != "") && (theme == ""))
            {
                DeleteFile("LabWorks", id);
                MessageBox.Show($"Файл \"{file_name}\" удалён!");
                lvi_LabReports_ComboBoxes_Update();
            }
            if ((id > 0) && (subject != "") && (theme != ""))
            {
                DeleteFile("LabWorks", id);
                MessageBox.Show($"Файл \"{file_name}\" удалён!");
                lvi_LabReports_ComboBoxes_Update();
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            ScrollUp(listView5);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            ScrollDown(listView5);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            ScrollUp(listView4);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            ScrollDown(listView4);
        }

        private void button26_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox9.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите дисциплину!");
            }

            string theme = "";
            try
            {
                theme = comboBox2.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите работу!");
            }

            if ((subject != "") && (theme != ""))
            {
                AddMaterial("TestWorks", subject, theme, "test");
                lvi_TestReports_ComboBoxes_Update();
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            int num = 0;
            int id = 0;
            string subject = "";
            try
            {
                subject = comboBox9.SelectedItem.ToString();
            }
            catch { }
            string file_name = "";
            try
            {
                num = listView4.SelectedIndices[0];
                id = filesTestReports[num].Id;
                file_name = filesLabReports[id].FileName;
            }
            catch { }
            MessageBox.Show(filesLabReports[10].FileName);
            string theme = "";
            string additional = "";
            try
            {
                theme = comboBox2.SelectedItem.ToString();
                additional = " AND theme = '" + theme + "'";

            }
            catch { }
            if ((id > 0) && (subject != ""))
            {
                DeleteFile("TestWorks", id);
                MessageBox.Show($"Файл удалён!");
                filesTestReports = lvi_TestReports_Update(listView4, "TestWorks", subject, additional);
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            string file_name = "";
            try
            {
                file_name = listView5.SelectedItems[0].Text;
            }
            catch { }

            if (file_name != "")
            {
                SaveFile(listView5, "TestWorks");
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox9.SelectedItem.ToString();
            }
            catch
            {

            }
            if (subject != "")
            {
                // Обнуляем значения в ComboBox'ах
                comboBox12.SelectedIndex = -1;
                comboBox2.SelectedIndex = -1;

                // Добавим названия контрольных в ComboBox'ы
                string aditional = " AND work_type = 'zadanie'";
                AddThemesToCmb(comboBox12, "TestWorks", subject, aditional);
                AddThemesToCmb(comboBox2, "TestWorks", subject, aditional);

                string additional = "";
                // Добавим в listView5 и в listView4 данные из таблицы TestWorks
                lvi_LabManuals_Update(listView5, "TestWorks", subject);
                filesTestReports = lvi_TestReports_Update(listView4, "TestWorks", subject, additional);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFile(listView4);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            OpenFile(listView6);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            OpenFile(listView5);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            string file_name = "";
            try
            {
                file_name = listView4.SelectedItems[0].Text;
            }
            catch { }

            if (file_name != "")
            {
                SaveFile(listView4, "TestWorks");
            }
        }

        private void listView5_DoubleClick(object sender, EventArgs e)
        {
            OpenFile(listView5);
        }

        private void listView4_DoubleClick(object sender, EventArgs e)
        {
            OpenFile(listView4);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvi_LabReports_ComboBoxes_Update();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectStudent(listView4, comboBox2, "TestWorks");
        }

        private void button33_Click(object sender, EventArgs e)
        {
            ScrollUp(listView6);
        }

        private void button32_Click(object sender, EventArgs e)
        {
            ScrollDown(listView6);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox14.SelectedItem.ToString();
            }
            catch { }
            if (subject != "")
                AddManual(listView6, "ForExams", subject);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            string file_name = "";
            try
            {
                file_name = listView6.SelectedItems[0].Text;
            }
            catch { }

            string subject = "";
            try
            {
                subject = comboBox14.SelectedItem.ToString();
            }
            catch { }
            if ((subject != "") && (file_name != ""))
            {
                string sqlExpression = "DELETE FROM ForExams WHERE file_name ='" + file_name + "'";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(sqlExpression, connection);
                    command.ExecuteNonQuery();
                }
                lvi_1_Update(listView6, "ForExams", subject);
            }
        }

        private void button34_Click(object sender, EventArgs e)
        {
            string file_name = "";
            try
            {
                file_name = listView6.SelectedItems[0].Text;
            }
            catch { }

            if (file_name != "")
            {
                SaveFile(listView6, "ForExams");
            }
        }

        // Функция для сохранения выбранного файла по названию файла
        private async void SaveFile(ListView listV, string table)
        {
            string f_name = listV.SelectedItems[0].Text;
            File find = new File();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT id, file_name, file_data FROM " + table + " WHERE file_name = '" + f_name + "'";
                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        string filename = reader.GetString(1);
                        byte[] data = (byte[])reader.GetValue(2);

                        find = new File(id, filename, data);
                    }
                }
            }
            // сохраним найденный файл из списка
            if (find.Data != null)
            {
                using (FileStream fs = new FileStream(find.FileName, FileMode.OpenOrCreate))
                {
                    fs.Write(find.Data, 0, find.Data.Length);
                    MessageBox.Show($"Файл \"{find.FileName}\" \nсохранен!");
                }
            }
        }

        private void button35_Click(object sender, EventArgs e)
        {
            string path1 = filename1;
            string path2 = filename2;
            string path3 = filename3;
            if ((textBox5.Text != "") && (textBox6.Text != ""))
            {
                progressBar1.Show();

                progressBar1.Visible = true;
                progressBar1.Minimum = 1;
                progressBar1.Maximum = 100;
                progressBar1.Value = 1;
                progressBar1.Step = 5;

                var exePath = AppDomain.CurrentDomain.BaseDirectory;

                timer1.Enabled = true;
                timer1.Interval = 700;
                timer1.Tick += timer1_Tick;

                string result = CompareFileWord(path1, path2, exePath, progressBar1);
                string shortFileName = result.Substring(result.LastIndexOf('\\') + 1);
                textBox8.Text = shortFileName;
            }
            else
                MessageBox.Show("Выберите 2 файла!");
        }

        private string AddForCompare(TextBox textB, string filename)
        {
            // Добавляем новый файл
            filename = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog1.FileName;

            }
            // Получаем короткое имя файла для сохранения в бд
            string shortFileName = filename.Substring(filename.LastIndexOf('\\') + 1);
            textB.Text = shortFileName;
            return filename;
        }

        private void button36_Click(object sender, EventArgs e)
        {
            filename1 = AddForCompare(textBox5, filename1);
        }

        private void button37_Click(object sender, EventArgs e)
        {
            filename2 = AddForCompare(textBox6, filename2);
        }

        private void button39_Click(object sender, EventArgs e)
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory; //path to exe file
            var path = Path.Combine(exePath, textBox8.Text);
            try
            {
                Process.Start(path);
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии файла.\nНеобходимо загрузить данный файл!");
            }
        }

        private void listView5_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFile(listView5);
        }

        private void listView4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFile(listView4);
        }

        private void listView6_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFile(listView6);
        }

        private void button38_Click(object sender, EventArgs e)
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory; //path to exe file
            var path = Path.Combine(exePath, "4_kurs.pdf");
            try
            {
                Process.Start(path);
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии файла.\nНеобходимо загрузить данный файл!");
            }
        }

        private void button41_Click(object sender, EventArgs e)
        {
            string additional = "";
            string subject = "";
            try
            {
                subject = comboBox3.SelectedItem.ToString();
            }
            catch { }
            string theme = "";
            try
            {
                theme = comboBox4.SelectedItem.ToString();
                additional = " AND theme = '" + theme + "'";
            }
            catch { }

            if (subject != "")
                lvi_Lectures_Update(listView1, "Lectures", subject, additional);
        }

        // Функция добавления тем в ComboBox
        private async void AddThemesToCmb(ComboBox cmb, string table, string subject)
        {
            cmb.Items.Clear();
            string sqlExpression = "SELECT theme FROM " + table + " WHERE subject = '" + subject + "'";
            string sqlResult = "";
            List<string> themes = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        if (!themes.Contains(sqlResult))
                        {
                            themes.Add(sqlResult);
                            cmb.Items.Add(themes.Last());
                        }
                    }
                }
            }
        }

        // Функция добавления тем в ComboBox (с перегрузкой)
        private async void AddThemesToCmb(ComboBox cmb, string table, string subject, string aditional)
        {
            cmb.Items.Clear();
            string sqlExpression = "SELECT theme FROM " + table + " WHERE subject = '" + subject + "'" + aditional;
            string sqlResult = "";
            List<string> themes = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        if (!themes.Contains(sqlResult))
                        {
                            themes.Add(sqlResult);
                            cmb.Items.Add(themes.Last());
                        }
                    }
                }
            }
        }

        private string GetUserGroup(string table, int user_id)
        {
            string group = "";
            string sqlExpression = "SELECT user_group FROM " + table + " WHERE id = '" + user_id.ToString() + "'";
            string sqlResult = "";
            List<string> themes = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        group = sqlResult;
                    }
                }
            }
            return group;
        }

        private async void AddSubjectsToCmb(ComboBox cmb, string table, object user_id)
        {
            cmb.Items.Clear();
            string group = GetUserGroup("Users", Convert.ToInt32(user_id));
            string sqlExpression = "SELECT subject FROM " + table + " WHERE group_name = '" + group + "'";
            string sqlResult = "";
            List<string> themes = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        cmb.Items.Add(sqlResult);
                    }
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox4.SelectedIndex = -1;
            string subject = comboBox3.SelectedItem.ToString();
            AddThemesToCmb(comboBox4, "Lectures", subject);
            lvi_Lectures_Update(listView1, "Lectures", subject);
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            string additional = "";
            string subject = "";
            try
            {
                subject = comboBox3.SelectedItem.ToString();
            }
            catch { }
            string theme = "";
            try
            {
                theme = comboBox4.SelectedItem.ToString();
                additional = " AND theme = '" + theme + "'";
            }
            catch { }

            if (subject != "")
                lvi_Lectures_Update(listView1, "Lectures", subject, additional);
        }

        private void button40_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox3.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите дисциплину!");
            }
            if (subject != "")
            {
                string additional = " AND type = 'lection'";
                lvi_Lectures_Update(listView1, "Lectures", subject, additional);
            }
        }

        private void button42_Click(object sender, EventArgs e)
        {
            string subject = "";
            try
            {
                subject = comboBox3.SelectedItem.ToString();
            }
            catch
            {
                MessageBox.Show("Выберите дисциплину!");
            }
            if (subject != "")
            {
                string additional = " AND type = 'manual'";
                lvi_Lectures_Update(listView1, "Lectures", subject, additional);
            }
        }

        private void button43_Click(object sender, EventArgs e)
        {
            OpenFile(listView1);
        }

        private async void button44_Click(object sender, EventArgs e)
        {
            int num = -1;
            int id = 0;
            try
            {
                num = listView3.SelectedIndices[0];
            }
            catch { }
            if (num > -1)
            {
                id = filesLabReports[num].Id;
                ChangeStatus("LabWorks", id, "commented");
                lvi_LabReports_ComboBoxes_Update();
                AddComment newForm = new AddComment(user_id, id, "admin", "laboratory");
                newForm.Show();
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Обнуляем значения в ComboBox'ах
            comboBox8.SelectedIndex = -1;
            comboBox7.SelectedIndex = -1;

            // Добавим названия лабораторных в ComboBox'ы
            string subject = comboBox5.SelectedItem.ToString();
            string aditional = " AND work_type = 'zadanie'";
            AddThemesToCmb(comboBox8, "LabWorks", subject, aditional);
            AddThemesToCmb(comboBox7, "LabWorks", subject, aditional);

            // Добавим в listView2 и в listView3 данные из таблицы LabWorks
            lvi_LabManuals_Update(listView2, "LabWorks", subject);
            lvi_LabReports_Update(listView3, "LabWorks", subject);
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            string aditional = "";

            string subject = "";
            try
            {
                subject = comboBox5.SelectedItem.ToString();
            }
            catch { }

            string theme = "";
            try
            {
                theme = comboBox8.SelectedItem.ToString();
                aditional = " AND theme = '" + theme + "'";
            }
            catch { }

            if ((subject != "") && (theme != ""))
                lvi_LabManuals_Update(listView2, "LabWorks", subject, aditional);
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvi_LabReports_ComboBoxes_Update();
        }

        private void lvi_LabReports_ComboBoxes_Update()
        {
            string additional = "";
            string subject = "";
            try
            {
                subject = comboBox5.SelectedItem.ToString();
            }
            catch { }

            additional += " AND user_id = '" + user_id.ToString() + "'";

            string theme = "";
            try
            {
                theme = comboBox7.SelectedItem.ToString();
            }
            catch { }
            if (theme != "")
                additional += " AND theme = '" + theme + "'";

            if (subject != "")
            {
                lvi_LabReports_Update(listView3, "LabWorks", subject, additional);
            }
        }

        private void lvi_TestReports_ComboBoxes_Update()
        {
            string additional = "";
            string subject = "";
            try
            {
                subject = comboBox9.SelectedItem.ToString();
            }
            catch { }

            additional += " AND user_id = '" + user_id.ToString() + "'";

            string theme = "";
            try
            {
                theme = comboBox2.SelectedItem.ToString();
            }
            catch { }
            if (theme != "")
                additional += " AND theme = '" + theme + "'";

            if (subject != "")
            {
                filesTestReports = lvi_TestReports_Update(listView4, "TestWorks", subject, additional);
            }
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvi_LabReports_ComboBoxes_Update();
        }

        private void button45_Click(object sender, EventArgs e)
        {
            // Добавим названия лабораторных в ComboBox'ы
            string subject = "";
            try
            {
                comboBox5.SelectedItem.ToString();
            }
            catch { }
            if (subject != "")
            {
                // Обнуляем значения в ComboBox'ах
                comboBox8.SelectedIndex = -1;
                comboBox7.SelectedIndex = -1;

                string aditional = " AND work_type = 'zadanie'";
                AddThemesToCmb(comboBox8, "LabWorks", subject, aditional);
                AddThemesToCmb(comboBox7, "LabWorks", subject, aditional);

                // Добавим в listView2 и в listView3 данные из таблицы LabWorks
                lvi_LabManuals_Update(listView2, "LabWorks", subject);
                lvi_LabReports_Update(listView3, "LabWorks", subject);
            }
        }

        private void button47_Click(object sender, EventArgs e)
        {
            OpenFile(listView2);
        }

        private void button48_Click(object sender, EventArgs e)
        {
            OpenFile(listView3);
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            OpenFile(listView2);
        }

        private void button46_Click(object sender, EventArgs e)
        {
            string additional = " AND status = 'credited'";
            string subject = "";
            try
            {
                subject = comboBox5.SelectedItem.ToString();
            }
            catch { }

            additional += " AND user_id = '" + user_id.ToString() + "'";

            string theme = "";
            try
            {
                theme = comboBox7.SelectedItem.ToString();
            }
            catch { }
            if (theme != "")
                additional += " AND theme = '" + theme + "'";

            if (subject != "")
            {
                lvi_LabReports_Update(listView3, "LabWorks", subject, additional);
            }
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Обнуляем значения в ComboBox'ах
            comboBox12.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;

            // Добавим названия контрольных в ComboBox'ы
            string subject = comboBox9.SelectedItem.ToString();
            string aditional = " AND work_type = 'zadanie'";
            AddThemesToCmb(comboBox12, "TestWorks", subject, aditional);
            AddThemesToCmb(comboBox2, "TestWorks", subject, aditional);

            string additional = "";
            // Добавим в listView5 и в listView4 данные из таблицы TestWorks
            lvi_LabManuals_Update(listView5, "TestWorks", subject);
            filesTestReports = lvi_TestReports_Update(listView4, "TestWorks", subject, additional);
        }

        private void comboBox10_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvi_TestReports_ComboBoxes_Update();
        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvi_TestReports_ComboBoxes_Update();
        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {

            string subject = "";
            try
            {
                subject = comboBox9.SelectedItem.ToString();
            }
            catch { }
            string additional = "";
            if (subject != "")
                filesTestReports = lvi_TestReports_Update(listView4, "TestWorks", subject, additional);
        }

        private void comboBox12_SelectedIndexChanged(object sender, EventArgs e)
        {
            string aditional = "";

            string subject = "";
            try
            {
                subject = comboBox9.SelectedItem.ToString();
            }
            catch { }

            string theme = "";
            try
            {
                theme = comboBox12.SelectedItem.ToString();
                aditional = " AND theme = '" + theme + "'";
            }
            catch { }

            if ((subject != "") && (theme != ""))
                lvi_LabManuals_Update(listView5, "TestWorks", subject, aditional);
        }

        private void button21_Click_1(object sender, EventArgs e)
        {
            OpenFile(listView5);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            OpenFile(listView4);
        }

        private void button19_Click_1(object sender, EventArgs e)
        {
            // Обнуляем значения в ComboBox'ах
            comboBox12.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;

            // Добавим названия контрольных в ComboBox'ы
            string subject = comboBox9.SelectedItem.ToString();
            string aditional = " AND work_type = 'zadanie'";
            AddThemesToCmb(comboBox12, "TestWorks", subject, aditional);
            AddThemesToCmb(comboBox2, "TestWorks", subject, aditional);

            string additional = "";
            // Добавим в listView5 и в listView4 данные из таблицы TestWorks
            lvi_LabManuals_Update(listView5, "TestWorks", subject);
            filesTestReports = lvi_TestReports_Update(listView4, "TestWorks", subject, additional);
        }

        private void comboBox14_SelectedIndexChanged(object sender, EventArgs e)
        {
            string subject = comboBox14.SelectedItem.ToString();
            lvi_1_Update(listView6, "ForExams", subject);
        }

        private void button22_Click_1(object sender, EventArgs e)
        {
            OpenFile(listView6);
        }
    }
}
