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
    public partial class AddComment : Form
    {
        object userId, workId;
        string userType, workType;
        string connectionString = ConfigurationManager.ConnectionStrings["StudyConnection"].ConnectionString;
        List<File> files;

        public AddComment(object user_id, object work_id, string user_type, string work_type)
        {
            InitializeComponent();

            userId = user_id;
            workId = work_id;
            userType = user_type;
            workType = work_type;
        }

        public class File
        {
            public File(int id, string date, int userId, string comment)
            {
                Id = id;
                Date = date;
                UserId = userId;
                Comment = comment;
            }

            public File()
            {
                Id = 0;
                Date = "";
                UserId = 0;
                Comment = "";
            }
            public int Id { get; private set; }
            public string Date { get; private set; }
            public int UserId { get; private set; }
            public string Comment { get; private set; }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            DateTime dateTime = DateTime.Now;
            string date = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            string text = textBox2.Text;
            if (text != "")
            {
                // Добавляем комментарий в таблицу
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandText = @"INSERT INTO Comments VALUES (@user_id, @work_id, @user_type, @work_type, @date, @comment)";
                    command.Parameters.Add("@user_id", SqlDbType.Int);
                    command.Parameters.Add("@work_id", SqlDbType.Int);
                    command.Parameters.Add("@user_type", SqlDbType.NChar, 30);
                    command.Parameters.Add("@work_type", SqlDbType.NChar, 30);
                    command.Parameters.Add("@date", SqlDbType.SmallDateTime);
                    command.Parameters.Add("@comment", SqlDbType.NVarChar);

                    
                    // Передаем данные в команду через параметры
                    command.Parameters["@user_id"].Value = userId;
                    command.Parameters["@work_id"].Value = workId;
                    command.Parameters["@user_type"].Value = userType;
                    command.Parameters["@work_type"].Value = workType;
                    command.Parameters["@date"].Value = date;
                    command.Parameters["@comment"].Value = text;

                    await command.ExecuteNonQueryAsync();
                }
                lvi_Update(listView1, "Comments");
            }
            else
            {
                MessageBox.Show("Введите комментарий!");
            }
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            int id = 0;
            try
            {
                id = listView1.SelectedIndices[0];
                textBox1.Text = files[id].Comment;
            }
            catch { }
        }

        private void AddComment_Load(object sender, EventArgs e)
        {
            listView1.View = System.Windows.Forms.View.Details;
            listView1.GridLines = true;
            lvi_Update(listView1, "Comments");
            string fio = GetUserName(Convert.ToInt32(userId), "Users");
            label2.Text = GetFileInfo(Convert.ToInt32(workId), "LabWorks");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int num = 0;
            int id = 0;
            try
            {
                num = listView1.SelectedIndices[0];
                id = files[num].Id;
            }
            catch { }
            if (id > 0)
                lvi_DeleteRow(listView1, "Comments", id);
            textBox1.Clear();
        }

        // Обновляем значения в ListView
        private async void lvi_Update(ListView listV, string table)
        {
            listV.Items.Clear();

            files = new List<File>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT id, date, user_id, comment FROM " + table + " WHERE work_id = '" + workId + "' AND work_type  = '" + workType + "'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        DateTime  dataTime = reader.GetDateTime(1);
                        string date = dataTime.ToString("HH:mm dd-MM-yyyy");
                        int user_id = reader.GetInt32(2);
                        string comment = reader.GetString(3);

                        File file_1 = new File(id, date, user_id, comment);
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
                    lvi.Text = files[i].Date;
                    string name = GetUserName(files[i].UserId, "Users");
                    lvi.SubItems.Add(name);
                    listV.Items.Add(lvi);
                }
            }
        }

        // Обновляем значения в ListView
        private void lvi_DeleteRow(ListView listV, string table, int id)
        {
            // Удаляем выделенный комментарий
            string sqlExpression = "DELETE FROM " + table + " WHERE id ='" + id + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.ExecuteNonQuery();
            }
            lvi_Update(listView1, "Comments");
        }

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

        private string GetFileInfo(int file_id, string table)
        {
            string file_name = "";
            int user_id = 0;
            
            string sqlExpression = "SELECT file_name, user_id FROM " + table + " WHERE id = '" + file_id.ToString() + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sqlExpression, connection);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) // Построчно считываем данные
                    {
                        file_name = reader.GetString(0);
                        user_id = reader.GetInt32(1);
                    }
                }
            }
            file_name = file_name.Trim();
            string user_name = GetUserName(user_id, "Users");

            return file_name + "         " + user_name;
        }
    }
}
