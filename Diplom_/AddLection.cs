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
    public partial class AddLection : Form
    {
        object user_id;
        string subject;
        string connectionString = ConfigurationManager.ConnectionStrings["StudyConnection"].ConnectionString;

        public AddLection(object id, string sub)
        {
            InitializeComponent();
            user_id = id; // <-----------------------------------
            subject = sub;
        }

        private void AddLection_Load(object sender, EventArgs e)
        {
            // Выберем все темы по выбранному предмету и добавим уникальные занчения в comboBox2
            AddThemesToCmb(comboBox2, subject);

            // Настраиваем колонки
            listView1.GridLines = true;
            listView1.View = System.Windows.Forms.View.Details;
        }

        // Функция добавления тем в ComboBox
        private async void AddThemesToCmb(ComboBox cmb, string subject)
        {
            cmb.Items.Clear();
            string sqlExpression = "SELECT theme FROM Lectures WHERE subject = '" + subject + "'";
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

        public class File
        {
            public File(int id, string filename, string type)
            {
                Id = id;
                FileName = filename;
                Type = type;
            }

            public File()
            {
                Id = 0;
                FileName = "";
                UserId = 0;
                Type = "";
            }
            public int Id { get; private set; }
            public string FileName { get; private set; }
            public int UserId { get; private set; }
            public string Type { get; private set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string theme = "";
            if (((textBox1.Text != "") && (checkBox1.Checked == true)) || (comboBox2.SelectedIndex != -1))
            {
                if (checkBox1.Checked == true)
                {
                    theme = textBox1.Text;
                }
                else
                {
                    theme = comboBox2.SelectedItem.ToString();
                }
                AddMaterial(listView1, "Lectures", subject, theme, "lection");
                AddThemesToCmb(comboBox2, subject);
            }
            else
                MessageBox.Show("Введите или выберите тему!");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                comboBox2.Enabled = false;
                comboBox2.SelectedIndex = -1;

            }
            else
            {
                comboBox2.Enabled = true;
            }
        }

        // Функция для добавления методического материала
        private async void AddMaterial(ListView listV, string table, string subject, string theme, string type)
        {
            string sub = subject;

            // Добавляем новый файл
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.CommandText = @"INSERT INTO " + table + " VALUES (@user_id, @file_name, @file_data, @subject, @theme, @type)";
                command.Parameters.Add("@user_id", SqlDbType.Int);
                command.Parameters.Add("@file_name", SqlDbType.NVarChar, 300);
                command.Parameters.Add("@subject", SqlDbType.NVarChar, 100);
                command.Parameters.Add("@theme", SqlDbType.NVarChar, 300);
                command.Parameters.Add("@type", SqlDbType.NVarChar, 100);

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
                    command.Parameters["@subject"].Value = sub;
                    command.Parameters["@theme"].Value = theme;
                    command.Parameters["@type"].Value = type;

                    await command.ExecuteNonQueryAsync();
                    MessageBox.Show($"Файл \"{shortFileName}\"\nдобавлен!");
                }
            }
            lvi_Update(listV, table, subject, theme);
        }

        // Функция для удаления методического материала
        private async void DeleteMaterial(ListView listV, string table, string subject, string theme)
        {
            // Удаляем выделенный файл
            string f_name = listV.SelectedItems[0].Text;
            string type = listV.SelectedItems[0].SubItems[1].Text;
            if (type == "Лекция")
            {
                type = "lection";
            }
            if (type == "Доп. материал")
            {
                type = "manual";
            }
            File find = new File();
            string sqlExpression = "DELETE FROM " + table + " WHERE user_id = '" + user_id + "' AND file_name ='" + f_name + "' AND subject = '" + subject + "' AND type = '" + type + "' AND theme = '" + theme + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                await command.ExecuteNonQueryAsync();
                MessageBox.Show($"Файл \"{f_name}\" \nудалён!");
            }

            // Обновляем ListView
            lvi_Update(listV, table, subject, theme);
        }

        // Обновляем значения в ListView
        private async void lvi_Update(ListView listV, string table, string subject, string theme)
        {
            listV.Items.Clear();

            List<File> files = new List<File>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT id, file_name, type FROM " + table + " WHERE subject = '" + subject + "' AND theme  = '" + theme + "'";

                SqlCommand command = new SqlCommand(sql, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        string filename = reader.GetString(1);
                        string type = reader.GetString(2);

                        File file_1 = new File(id, filename, type);
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
                    lvi.Text = files[i].FileName;
                    lvi.ImageIndex = 0;
                    if (files[i].Type == "lection")
                    {
                        lvi.SubItems.Add("Лекция");
                    }
                    if (files[i].Type == "manual")
                    {
                        lvi.SubItems.Add("Доп. материал");
                    }
                    listV.Items.Add(lvi);
                }
                
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string theme = "";
            if (((textBox1.Text != "") && (checkBox1.Checked == true)) || (comboBox2.SelectedIndex != -1))
            {
                if (checkBox1.Checked == true)
                {
                    theme = textBox1.Text;
                }
                else
                {
                    theme = comboBox2.SelectedItem.ToString();
                }
                AddMaterial(listView1, "Lectures", subject, theme, "manual");
                AddThemesToCmb(comboBox2, subject);
            }
            else
                MessageBox.Show("Введите или выберите тему!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string theme = "";
            if (checkBox1.Checked == true)
            {
                theme = textBox1.Text;
            }
            else
            {
                theme = comboBox2.SelectedItem.ToString();
            }
            DeleteMaterial(listView1, "Lectures", subject, theme);
            AddThemesToCmb(comboBox2, subject);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string theme = comboBox2.SelectedItem.ToString();
            lvi_Update(listView1, "Lectures", subject, theme);
        }
    }
}
