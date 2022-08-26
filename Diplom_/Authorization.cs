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

namespace Diplom_
{
    public partial class Authorization : Form
    {
        public Authorization()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string log = textBox1.Text;
            string pas = textBox2.Text;

            object id;
            string password = "";
            string typeOfUser = "";

            string connectionString = ConfigurationManager.ConnectionStrings["StudyConnection"].ConnectionString;
            string sqlExpression_1 = "SELECT id FROM Users WHERE login = '" + log + "'";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression_1, connection);
                id = await command.ExecuteScalarAsync();
            }

            string sqlExpression_2 = "SELECT password, user_type FROM Users WHERE id = '" + $"{id}" + "'";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(sqlExpression_2, connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) // построчно считываем данные
                    {
                        password = reader.GetString(0);
                        typeOfUser = reader.GetString(1);
                    }
                }
            }
            password = password.Trim();
            typeOfUser = typeOfUser.Trim();

            if ((pas == password) && (typeOfUser == "admin"))
            {
                Teacher newForm = new Teacher(id);
                newForm.Show();
            }
            else if ((pas == password) && (typeOfUser == "user"))
            {
                Student newForm = new Student(id);
                newForm.Show();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Registration newForm = new Registration();
            newForm.Show();
        }
    }
}
