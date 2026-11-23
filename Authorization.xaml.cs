using System;
using System.Data.SqlClient;
using System.Windows;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class Authorization : Window
    {
        public Authorization()
        {
            InitializeComponent();
            Loaded += Authorization_Loaded;
        }

        private void Authorization_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UsernameTextBox.Items.Clear();
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Login FROM Polzovateli WHERE Aktiven = 1 ORDER BY Login", conn);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) UsernameTextBox.Items.Add(r.GetString(0));
                }
                if (UsernameTextBox.Items.Count > 0)
                    UsernameTextBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = (UsernameTextBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Выберите пользователя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Login, Rol, Imya, Familiya FROM Polzovateli WHERE Login = @u AND Aktiven = 1", conn);
                    cmd.Parameters.AddWithValue("@u", login);

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        string userLogin = r.GetString(0);
                        string role = r.GetString(1);
                        string name = r.GetString(2) + " " + r.GetString(3);

                        Window next = null;
                        if (role == "Клиент") next = new ClientMenu(userLogin, name, role);
                        else if (role == "Агент") next = new AgentMenu(userLogin, name, role);
                        else if (role == "Администратор") next = new AdminMenu(userLogin, name, role);

                        if (next == null)
                        {
                            MessageBox.Show("Неизвестная роль: " + role);
                            return;
                        }
                        next.Show();
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutrhorizationButton_Click(object sender, RoutedEventArgs e)
        {
            new Registration().Show();
            Close();
        }
    }
}