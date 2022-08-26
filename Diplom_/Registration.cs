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

namespace Diplom_
{
    public partial class Registration : Form
    {
        public Registration()
        {
            InitializeComponent();
        }

        private bool IsLoginCorrect(string str)
        {
            int num = 0;
            if ((str.Length > 0) && (str.Length <= 30))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (((int)str[i] >= 97 && (int)str[i] <= 122) || (str[i] == '_') || (str[i] == '.') || (str[i] == '-') || (char.IsDigit(str[i]) == true))
                    {
                        num++;
                    }
                }
                if (str.Length == num)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private bool IsPasswordCorrect(string str)
        {
            int num = 0;
            if ((str.Length > 0) && (str.Length <= 30))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (((int)str[i] >= 97 && (int)str[i] <= 122) || (char.IsDigit(str[i]) == true))
                    {
                        num++;
                    }
                }
                if (str.Length == num)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private bool IsFIOCorrect(string str)
        {
            int num = 0;
            if ((str.Length > 0) && (str.Length <= 30))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (char.IsLetter(str[i]) == true)
                    {
                        num++;
                    }
                }
                if (str.Length == num)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // Данные, введённые пользователем
            string l_name = textBox1.Text;
            string f_name = textBox2.Text;
            string patronymic = textBox3.Text;
            string login = textBox4.Text;
            string password = textBox5.Text;

            // Проверяем, корректно ли заполнены поля
            bool ln = IsFIOCorrect(l_name);
            bool fn = IsFIOCorrect(f_name);
            bool pt = IsFIOCorrect(patronymic);
            bool lg = IsLoginCorrect(login);
            bool ps = IsPasswordCorrect(password);

            // Получаем тип пользователя из comboBox2 и  проверяем заполено ли значение для user_group
            string selectedUserType = "";
            try
            {
                selectedUserType = comboBox2.SelectedItem.ToString();
            }
            catch { }
            string selectedGroup = "";
            bool type_1 = false;
            bool type_2 = false;

            // Если comboBox2 и comboBox1 заполнены, то тип пользователя - Студент
            if ((comboBox2.SelectedIndex > -1) && (comboBox1.SelectedIndex > -1))
            {
                if (selectedUserType == "Студент")
                {
                    selectedUserType = "user";
                    selectedGroup = comboBox1.SelectedItem.ToString().Trim();
                    type_1 = true;
                }
            }

            // Если comboBox2 и checkedListBox1 заполнены, то тип пользователя - Преподаватель
            if ((comboBox2.SelectedIndex > -1) && (checkedListBox1.CheckedItems.Count > 0))
            {
                if (selectedUserType == "Преподаватель")
                {
                    selectedUserType = "admin";
                    //selectedGroup = comboBox3.SelectedItem.ToString().Trim();
                    type_2 = true;
                }
            }
            
            // Проверяем, уникален ли логин
            string connectionString = ConfigurationManager.ConnectionStrings["StudyConnection"].ConnectionString;
            string sqlExpression = "SELECT login FROM Users WHERE login ='" + login + "'";
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
                    }
                }
            }

            bool isLoginUnique = false;
            if (sqlResult == "")
                isLoginUnique = true; // Логин уникален            

            // Если все условия выполняются, то добавляем пользователя
            if (((type_1 == true) || (type_2 == true)) && (ln = true) && (fn = true) && (pt = true) && (lg = true) && (ps = true) && (isLoginUnique == true))
            {
                // Добавляем новый файл
                // -----------------------------------------------------------------------------
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command_1 = new SqlCommand();
                    command_1.Connection = connection;
                    command_1.CommandText = @"INSERT INTO Users VALUES (@login, @password, @user_type, @last_name, @first_name, @patronymic, @user_group)";
                    command_1.Parameters.Add("@login", SqlDbType.NChar, 30);
                    command_1.Parameters.Add("@password", SqlDbType.NChar, 30);
                    command_1.Parameters.Add("@user_type", SqlDbType.NChar, 30);
                    command_1.Parameters.Add("@last_name", SqlDbType.NChar, 30);
                    command_1.Parameters.Add("@first_name", SqlDbType.NChar, 30);
                    command_1.Parameters.Add("@patronymic", SqlDbType.NChar, 30);
                    command_1.Parameters.Add("@user_group", SqlDbType.NVarChar, 100);

                    // Передаем данные в команду через параметры
                    command_1.Parameters["@login"].Value = login;
                    command_1.Parameters["@password"].Value = password;
                    command_1.Parameters["@user_type"].Value = selectedUserType;
                    command_1.Parameters["@last_name"].Value = l_name;
                    command_1.Parameters["@first_name"].Value = f_name;
                    command_1.Parameters["@patronymic"].Value = patronymic;
                    command_1.Parameters["@user_group"].Value = "";

                    await command_1.ExecuteNonQueryAsync();

                    SqlCommand command_2 = new SqlCommand();
                    command_2.CommandText = @"SELECT MAX(id) FROM Users";
                    command_2.Connection = connection;
                    object last_id = command_2.ExecuteScalar();

                    await command_2.ExecuteNonQueryAsync();

                    for (int i = 0; i < checkedListBox1.CheckedItems.Count; i++)
                    {
                        string subject = checkedListBox1.CheckedItems[i].ToString();
                        SqlCommand command_3 = new SqlCommand();
                        command_3.Connection = connection;
                        command_3.CommandText = @"UPDATE Subjects SET teacher_id = '" + last_id.ToString() + "' WHERE subject = '" + subject + "'";
                        
                        await command_3.ExecuteNonQueryAsync();

                    }
                    MessageBox.Show("Пользователь добавлен!");
                }
                this.Close();
            }
            else if ((isLoginUnique == false) && (lg == true))
                MessageBox.Show($"Логин \"{login}\" уже существует!", "Ошибка!");
            else
                MessageBox.Show("Неверно заполнены поля.", "Ошибка!");
        }

        private async void Form4_Load(object sender, EventArgs e)
        {
            comboBox1.Enabled = false;
            checkedListBox1.Enabled = false;

            string connectionString = ConfigurationManager.ConnectionStrings["StudyConnection"].ConnectionString;
            string sqlResult = "";

            // Добавляем названия групп в comboBox1
            string sqlExpression_1 = "SELECT * FROM Groups";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression_1, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        comboBox1.Items.Add(sqlResult);
                    }
                }
            }

            // Добавляем типы пользвателей в comboBox2
            comboBox2.Items.Add("Студент");
            comboBox2.Items.Add("Преподаватель");

            // Добавляем названия предметов в checkListBox1
            string sqlExpression_2 = "SELECT subject FROM Subjects";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression_2, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        sqlResult = reader.GetString(0);
                        sqlResult = sqlResult.Trim();
                        checkedListBox1.Items.Add(sqlResult);
                    }
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedUserType = comboBox2.SelectedItem.ToString();
            if (selectedUserType == "Студент")
            {
                comboBox1.Enabled = true;
                checkedListBox1.Enabled = false;
            }
            if (selectedUserType == "Преподаватель")
            {
                checkedListBox1.Enabled = true;
                comboBox1.Enabled = false;
            }
        }
    }
}
