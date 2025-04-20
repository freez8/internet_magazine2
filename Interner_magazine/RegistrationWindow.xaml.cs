using System;
using System.Windows;
using Npgsql;

namespace Interner_magazine
{
    public partial class RegistrationWindow : Window
    {
        private DatabaseConnection _dbConnection = new DatabaseConnection();

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения полей
            if (string.IsNullOrEmpty(txtLogin.Text) || string.IsNullOrEmpty(txtPassword.Password) ||
                string.IsNullOrEmpty(txtPhone.Text) || string.IsNullOrEmpty(txtFirstName.Text) ||
                string.IsNullOrEmpty(txtLastName.Text))
            {
                txtError.Text = "Пожалуйста, заполните все поля";
                return;
            }

            // Проверка, что телефон содержит только цифры
            if (!Int64.TryParse(txtPhone.Text, out _))
            {
                txtError.Text = "Телефон должен содержать только цифры";
                return;
            }

            try
            {
                using (var connection = _dbConnection.GetConnection())
                {
                    connection.Open();

                    // Проверка существования пользователя с таким логином
                    string checkQuery = "SELECT COUNT(*) FROM useraccount WHERE login = @login";
                    using (var checkCommand = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@login", txtLogin.Text);
                        long count = (long)checkCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            txtError.Text = "Пользователь с таким логином уже существует";
                            return;
                        }
                    }

                    // Добавление нового пользователя
                    string insertQuery = @"
                        INSERT INTO useraccount (login, password, phone, firstname, lastname)
                        VALUES (@login, @password, @phone, @firstname, @lastname)";

                    using (var insertCommand = new NpgsqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@login", txtLogin.Text);
                        insertCommand.Parameters.AddWithValue("@password", txtPassword.Password);
                        insertCommand.Parameters.AddWithValue("@phone", Int64.Parse(txtPhone.Text));
                        insertCommand.Parameters.AddWithValue("@firstname", txtFirstName.Text);
                        insertCommand.Parameters.AddWithValue("@lastname", txtLastName.Text);

                        insertCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Регистрация успешно выполнена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                txtError.Text = $"Ошибка при регистрации: {ex.Message}";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
