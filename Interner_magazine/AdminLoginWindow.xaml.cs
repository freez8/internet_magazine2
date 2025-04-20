using System;
using System.Windows;
using Npgsql;

namespace Interner_magazine
{
    public partial class AdminLoginWindow : Window
    {
        private DatabaseConnection _dbConnection = new DatabaseConnection();

        public AdminLoginWindow()
        {
            InitializeComponent();
        }

        private void btnAdminLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtAdminLogin.Text;
            string password = txtAdminPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                txtError.Text = "Пожалуйста, заполните все поля";
                return;
            }

            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT adminid, firstnameadmin, lastnameadmin FROM public.admin WHERE loginadmin = @login AND passwordadmin = @password";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int adminId = reader.GetInt32(0);
                                string firstName = reader.GetString(1);
                                string lastName = reader.GetString(2);

                                // Открыть панель администратора
                                AdminPanelWindow adminPanel = new AdminPanelWindow(adminId, firstName, lastName);
                                adminPanel.Show();
                                this.Close();
                            }
                            else
                            {
                                txtError.Text = "Неверный логин или пароль администратора";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                txtError.Text = $"Ошибка при входе: {ex.Message}";
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}