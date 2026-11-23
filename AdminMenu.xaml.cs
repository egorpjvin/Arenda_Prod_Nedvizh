using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class AdminMenu : Window
    {
        public AdminMenu(string login, string name, string role)
        {
            InitializeComponent();
            UserInfoText.Text = $"Пользователь: {name}  •  Логин: {login}  •  Роль: {role}";
            LoadListings();
            LoadUsers();
        }

        // ==========================================
        // 1. ЗАГРУЗКА ДАННЫХ
        // ==========================================
        private void LoadListings()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT o.KOD_Obyavleniya, o.KOD_Nedvizhimosti, o.Zagolovok, o.TipObyavleniya, 
                               n.TipObekta, n.Gorod, n.Adres, n.Ploshchad, n.Komnat, o.Tsena, n.ImagePath 
                        FROM Obyavleniya o 
                        JOIN Nedvizhimost n ON n.KOD_Nedvizhimosti = o.KOD_Nedvizhimosti", conn);

                    using (var r = cmd.ExecuteReader())
                    {
                        var list = new System.Collections.Generic.List<ListingRow>();
                        while (r.Read())
                        {
                            list.Add(new ListingRow
                            {
                                KOD_Obyavleniya = r.GetInt32(0),
                                KOD_Nedvizhimosti = r.GetInt32(1),
                                Zagolovok = r.GetString(2),
                                TipObyavleniya = r.GetString(3),
                                TipObekta = r.GetString(4),
                                Gorod = r.GetString(5),
                                Adres = r.GetString(6),
                                Ploshchad = r.GetDecimal(7),
                                Komnat = r.GetInt32(8),
                                Tsena = r.GetDecimal(9),
                                ImagePath = r.IsDBNull(10) ? null : r.GetString(10)
                            });
                        }
                        ListingsList.ItemsSource = list;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки объявлений: " + ex.Message); }
        }

        private void LoadUsers()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var dt = new DataTable();
                    new SqlDataAdapter("SELECT KOD_Polzovatelya, Login, Familiya, Imya, Telefon, Rol FROM Polzovateli", conn).Fill(dt);
                    UsersGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message); }
        }

        // ==========================================
        // 2. УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ
        // ==========================================
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersGrid.SelectedItem is DataRowView row)
            {
                EditPanel.Visibility = Visibility.Visible;
                EditLogin.Text = row["Login"].ToString();
                EditSurname.Text = row["Familiya"].ToString();
                EditName.Text = row["Imya"].ToString();
                EditPhone.Text = row["Telefon"].ToString();
                EditRole.Text = row["Rol"].ToString();
            }
            else
            {
                EditPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersGrid.SelectedItem is DataRowView row)) return;
            int id = (int)row["KOD_Polzovatelya"];

            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("UPDATE Polzovateli SET Login=@l, Familiya=@f, Imya=@i, Telefon=@t, Rol=@r WHERE KOD_Polzovatelya=@id", conn);
                    cmd.Parameters.AddWithValue("@l", EditLogin.Text.Trim());
                    cmd.Parameters.AddWithValue("@f", EditSurname.Text.Trim());
                    cmd.Parameters.AddWithValue("@i", EditName.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", EditPhone.Text.Trim());
                    cmd.Parameters.AddWithValue("@r", EditRole.Text);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Данные пользователя успешно обновлены!", "Успех");
                LoadUsers();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка сохранения: " + ex.Message); }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersGrid.SelectedItem is DataRowView row)) return;
            int id = (int)row["KOD_Polzovatelya"];

            if (MessageBox.Show("Вы уверены, что хотите удалить пользователя? Если у него есть объявления, он будет переведен в статус 'Неактивен'.", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = Db.GetConnection())
                    {
                        conn.Open();
                        // Пытаемся удалить физически
                        var cmd = new SqlCommand("DELETE FROM Polzovateli WHERE KOD_Polzovatelya=@id", conn);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Пользователь удален.");
                    }
                }
                catch (SqlException)
                {
                    // Если удалить нельзя (есть связанные данные), делаем "мягкое" удаление (блокировка)
                    try
                    {
                        using (var conn = Db.GetConnection())
                        {
                            conn.Open();
                            var cmd = new SqlCommand("UPDATE Polzovateli SET Aktiven = 0 WHERE KOD_Polzovatelya=@id", conn);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Пользователь имеет связанные данные (объявления/сделки). Профиль переведен в статус 'Заблокирован'.", "Блокировка");
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Ошибка блокировки: " + ex.Message); }
                }
                LoadUsers();
            }
        }

        // ==========================================
        // 3. МОДЕРАЦИЯ ОБЪЯВЛЕНИЙ
        // ==========================================
        private void DeleteListing_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.Tag as ListingRow;
            if (row == null) return;

            if (MessageBox.Show($"Удалить объявление '{row.Zagolovok}'?", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = Db.GetConnection())
                    {
                        conn.Open();
                        // Удаляем объявление, затем сам объект
                        new SqlCommand($"DELETE FROM Obyavleniya WHERE KOD_Obyavleniya = {row.KOD_Obyavleniya}", conn).ExecuteNonQuery();
                        new SqlCommand($"DELETE FROM Nedvizhimost WHERE KOD_Nedvizhimosti = {row.KOD_Nedvizhimosti}", conn).ExecuteNonQuery();
                    }
                    LoadListings();
                }
                catch (Exception ex) { MessageBox.Show("Ошибка удаления (возможно, есть связанные сделки): " + ex.Message); }
            }
        }

        // ==========================================
        // 4. РЕЗЕРВНОЕ КОПИРОВАНИЕ БД
        // ==========================================
        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Файлы резервных копий (*.bak)|*.bak";
            sfd.FileName = $"ProdAren_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var conn = Db.GetConnection())
                    {
                        conn.Open();
                        string dbName = conn.Database;
                        // Запрос на резервное копирование
                        string sql = $"BACKUP DATABASE [{dbName}] TO DISK = '{sfd.FileName}' WITH FORMAT, MEDIANAME = 'ProdArenBackup', NAME = 'Full Backup of {dbName}';";

                        var cmd = new SqlCommand(sql, conn);
                        cmd.CommandTimeout = 120; // Даем больше времени на случай большой БД
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Резервная копия базы данных успешно создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusBarText.Text = $"Резервная копия создана: {sfd.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при создании резервной копии:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadListings();
            LoadUsers();
            StatusBarText.Text = "Данные обновлены.";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new Authorization().Show();
            Close();
        }
    }
}