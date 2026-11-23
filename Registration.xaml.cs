using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class Registration : Window
    {
        public Registration()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string surname = SurnameTextBox.Text.Trim();
            string name = NameTextBox.Text.Trim();
            string login = LoginTextBox.Text.Trim();
            string telephone = TelephoneTextBox.Text.Trim();
            string role = (RoleBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Клиент";

            if (string.IsNullOrEmpty(surname) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(telephone))
            {
                MessageBox.Show("Заполните все обязательные поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var check = new SqlCommand("SELECT COUNT(*) FROM Polzovateli WHERE Login = @l", conn);
                    check.Parameters.AddWithValue("@l", login);
                    if ((int)check.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var cmd = new SqlCommand("INSERT INTO Polzovateli (Login, Email, Imya, Familiya, Telefon, Rol) VALUES (@l, @e, @n, @s, @t, @r)", conn);
                    cmd.Parameters.AddWithValue("@l", login);
                    cmd.Parameters.AddWithValue("@e", login + "@prodaren.ru");
                    cmd.Parameters.AddWithValue("@n", name);
                    cmd.Parameters.AddWithValue("@s", surname);
                    cmd.Parameters.AddWithValue("@t", telephone);
                    cmd.Parameters.AddWithValue("@r", role);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                new Authorization().Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка регистрации:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}