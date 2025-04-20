using System;
using System.Windows;
using Npgsql;

namespace Interner_magazine
{
    public partial class MainWindow : Window
    {
        private DatabaseConnection _dbConnection = new DatabaseConnection();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string password = txtPassword.Password;

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

                    // Сначала проверяем, не заблокирован ли пользователь
                    string checkActiveQuery = "SELECT isactive FROM useraccount WHERE login = @login";
                    using (var checkCommand = new NpgsqlCommand(checkActiveQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@login", login);
                        var isActiveObj = checkCommand.ExecuteScalar();

                        // Если пользователь найден и заблокирован
                        if (isActiveObj != null && !(bool)isActiveObj)
                        {
                            txtError.Text = "Аккаунт заблокирован. Обратитесь к администратору.";
                            return;
                        }
                    }

                    // Проверяем логин и пароль
                    string query = "SELECT userid, firstname, lastname, login_attempts FROM useraccount WHERE login = @login";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string firstName = reader.GetString(1);
                                string lastName = reader.GetString(2);
                                int loginAttempts = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                                reader.Close(); // Закрываем reader перед следующими запросами

                                // Теперь проверяем пароль отдельно
                                string passwordQuery = "SELECT COUNT(*) FROM useraccount WHERE login = @login AND password = @password";
                                using (var passwordCommand = new NpgsqlCommand(passwordQuery, connection))
                                {
                                    passwordCommand.Parameters.AddWithValue("@login", login);
                                    passwordCommand.Parameters.AddWithValue("@password", password);

                                    int matchCount = Convert.ToInt32(passwordCommand.ExecuteScalar());

                                    if (matchCount > 0)
                                    {
                                        // Пароль верный, сбрасываем счетчик попыток
                                        string resetQuery = "UPDATE useraccount SET login_attempts = 0 WHERE userid = @userId";
                                        using (var resetCommand = new NpgsqlCommand(resetQuery, connection))
                                        {
                                            resetCommand.Parameters.AddWithValue("@userId", userId);
                                            resetCommand.ExecuteNonQuery();
                                        }

                                        // Открытие окна каталога с передачей данных
                                        CatalogWindow catalogWindow = new CatalogWindow(userId, firstName, lastName);
                                        catalogWindow.Show();
                                        this.Close();
                                    }
                                    else
                                    {
                                        // Пароль неверный, увеличиваем счетчик попыток
                                        loginAttempts++;

                                        if (loginAttempts >= 3)
                                        {
                                            // Блокируем пользователя
                                            string blockQuery = "UPDATE useraccount SET isactive = false, login_attempts = @attempts WHERE userid = @userId";
                                            using (var blockCommand = new NpgsqlCommand(blockQuery, connection))
                                            {
                                                blockCommand.Parameters.AddWithValue("@attempts", loginAttempts);
                                                blockCommand.Parameters.AddWithValue("@userId", userId);
                                                blockCommand.ExecuteNonQuery();
                                            }

                                            txtError.Text = "Аккаунт заблокирован из-за превышения количества попыток входа. Обратитесь к администратору.";
                                        }
                                        else
                                        {
                                            // Увеличиваем счетчик неудачных попыток
                                            string updateQuery = "UPDATE useraccount SET login_attempts = @attempts WHERE userid = @userId";
                                            using (var updateCommand = new NpgsqlCommand(updateQuery, connection))
                                            {
                                                updateCommand.Parameters.AddWithValue("@attempts", loginAttempts);
                                                updateCommand.Parameters.AddWithValue("@userId", userId);
                                                updateCommand.ExecuteNonQuery();
                                            }

                                            txtError.Text = $"Неверный пароль. Осталось попыток: {3 - loginAttempts}";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                txtError.Text = "Пользователь с таким логином не найден";
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

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Открыть окно регистрации
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog();
        }

        private void btnAdminLogin_Click(object sender, RoutedEventArgs e)
        {
            // Открыть окно входа для администратора
            AdminLoginWindow adminLoginWindow = new AdminLoginWindow();
            adminLoginWindow.Show();
            this.Close();
        }
    }
}
