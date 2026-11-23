using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class AgentMenu : Window
    {
        private readonly string _login;
        private int _agentId = 0;
        private readonly List<ListingRow> _allListings = new List<ListingRow>();

        public AgentMenu(string login, string name, string role)
        {
            InitializeComponent();
            _login = login;
            UserInfoText.Text = $"Пользователь: {name}  •  Логин: {login}  •  Роль: {role}";

            using (var conn = Db.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT KOD_Polzovatelya FROM Polzovateli WHERE Login = @l", conn);
                cmd.Parameters.AddWithValue("@l", _login);
                _agentId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            InitFilters();
            LoadReadyDeals();
        }

        private void InitFilters()
        {
            TypeBox.Items.Add("Все типы"); TypeBox.Items.Add("Квартира"); TypeBox.Items.Add("Дом"); TypeBox.Items.Add("Коммерческая"); TypeBox.Items.Add("Участок");
            TypeBox.SelectedIndex = 0;
            DealTypeBox.Items.Add("Все сделки"); DealTypeBox.Items.Add("Продажа"); DealTypeBox.Items.Add("Аренда");
            DealTypeBox.SelectedIndex = 0;
        }

        // Загрузка ВСЕХ объявлений (по кнопке "Показать все")
        private void LoadListings()
        {
            _allListings.Clear();
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT o.KOD_Obyavleniya, o.KOD_Nedvizhimosti, o.KOD_Polzovatelya, 
                               o.Zagolovok, o.TipObyavleniya, n.TipObekta, n.Gorod, n.Adres, 
                               n.Ploshchad, n.Komnat, o.Tsena, o.StatusObyavleniya, 
                               o.KolichestvoProsmotrov, n.ImagePath, n.Opisanie, 
                               n.Etazh, n.EtazheyVDome, n.GodPostroyki
                        FROM Obyavleniya o
                        JOIN Nedvizhimost n ON n.KOD_Nedvizhimosti = o.KOD_Nedvizhimosti
                        ORDER BY o.KOD_Obyavleniya DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            _allListings.Add(new ListingRow
                            {
                                KOD_Obyavleniya = r.GetInt32(0),
                                KOD_Nedvizhimosti = r.GetInt32(1),
                                KOD_Polzovatelya = r.GetInt32(2),
                                Zagolovok = r.GetString(3),
                                TipObyavleniya = r.GetString(4),
                                TipObekta = r.GetString(5),
                                Gorod = r.GetString(6),
                                Adres = r.GetString(7),
                                Ploshchad = r.GetDecimal(8),
                                Komnat = r.GetInt32(9),
                                Tsena = r.GetDecimal(10),
                                StatusObyavleniya = r.GetString(11),
                                KolichestvoProsmotrov = r.GetInt32(12),
                                ImagePath = r.IsDBNull(13) ? null : r.GetString(13),
                                Opisanie = r.IsDBNull(14) ? null : r.GetString(14),
                                Etazh = r.IsDBNull(15) ? (int?)null : r.GetInt32(15),
                                EtazheyVDome = r.IsDBNull(16) ? (int?)null : r.GetInt32(16),
                                GodPostroyki = r.IsDBNull(17) ? (int?)null : r.GetInt32(17)
                            });
                        }
                    }
                }
                ListHeaderText.Text = "Все объявления в системе";
                BtnShowAll.Visibility = Visibility.Collapsed; // Скрываем кнопку, т.к. мы и так смотрим все
                ApplyFilters();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки объявлений: " + ex.Message); }
        }

        // Загрузка ТОЛЬКО тех объявлений, где клиент нажал "Готов к сделке"
        private void LoadReadyDeals()
        {
            _allListings.Clear();
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT o.KOD_Obyavleniya, o.KOD_Nedvizhimosti, o.KOD_Polzovatelya, 
                               o.Zagolovok, o.TipObyavleniya, n.TipObekta, n.Gorod, n.Adres, 
                               n.Ploshchad, n.Komnat, o.Tsena, o.StatusObyavleniya, 
                               o.KolichestvoProsmotrov, n.ImagePath, n.Opisanie, 
                               n.Etazh, n.EtazheyVDome, n.GodPostroyki,
                               z.KOD_Klienta AS ReadyClientId,
                               z.ReadyClientName
                        FROM Obyavleniya o
                        JOIN Nedvizhimost n ON n.KOD_Nedvizhimosti = o.KOD_Nedvizhimosti
                        CROSS APPLY (
                            SELECT TOP 1 z.KOD_Klienta, p.Familiya + ' ' + p.Imya AS ReadyClientName
                            FROM Zayavki z
                            JOIN Polzovateli p ON p.KOD_Polzovatelya = z.KOD_Klienta
                            WHERE z.KOD_Obyavleniya = o.KOD_Obyavleniya AND z.GotovKSdelke = 1
                            ORDER BY z.KOD_Zayavki DESC
                        ) z
                        ORDER BY o.KOD_Obyavleniya DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            _allListings.Add(new ListingRow
                            {
                                KOD_Obyavleniya = r.GetInt32(0),
                                KOD_Nedvizhimosti = r.GetInt32(1),
                                KOD_Polzovatelya = r.GetInt32(2),
                                Zagolovok = r.GetString(3),
                                TipObyavleniya = r.GetString(4),
                                TipObekta = r.GetString(5),
                                Gorod = r.GetString(6),
                                Adres = r.GetString(7),
                                Ploshchad = r.GetDecimal(8),
                                Komnat = r.GetInt32(9),
                                Tsena = r.GetDecimal(10),
                                StatusObyavleniya = r.GetString(11),
                                KolichestvoProsmotrov = r.GetInt32(12),
                                ImagePath = r.IsDBNull(13) ? null : r.GetString(13),
                                Opisanie = r.IsDBNull(14) ? null : r.GetString(14),
                                Etazh = r.IsDBNull(15) ? (int?)null : r.GetInt32(15),
                                EtazheyVDome = r.IsDBNull(16) ? (int?)null : r.GetInt32(16),
                                GodPostroyki = r.IsDBNull(17) ? (int?)null : r.GetInt32(17),
                                ReadyClientId = r.GetInt32(18),
                                ReadyClientName = r.GetString(19)
                            });
                        }
                    }
                }
                ListHeaderText.Text = "Объявления, требующие оформления сделки";
                BtnShowAll.Visibility = Visibility.Visible; // Показываем кнопку, чтобы можно было вернуться ко всем
                ApplyFilters();

                if (_allListings.Count == 0)
                    MessageBox.Show("Нет клиентов, готовых к сделке.");
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки готовых сделок: " + ex.Message); }
        }

        private void ApplyFilters()
        {
            if (ListingsList == null) return;
            string search = (SearchBox.Text ?? "").Trim().ToLower();
            string city = (CityBox.Text ?? "").Trim().ToLower();
            string type = TypeBox.SelectedItem as string;
            string dealType = DealTypeBox.SelectedItem as string;

            var filtered = new List<ListingRow>();
            foreach (var l in _allListings)
            {
                if (!string.IsNullOrEmpty(search) && !((l.Zagolovok ?? "").ToLower().Contains(search) || (l.Adres ?? "").ToLower().Contains(search))) continue;
                if (!string.IsNullOrEmpty(city) && !(l.Gorod ?? "").ToLower().Contains(city)) continue;
                if (!string.IsNullOrEmpty(type) && type != "Все типы" && l.TipObekta != type) continue;
                if (!string.IsNullOrEmpty(dealType) && dealType != "Все сделки" && l.TipObyavleniya != dealType) continue;
                filtered.Add(l);
            }
            ListingsList.ItemsSource = filtered;
            CountText.Text = $"Найдено: {filtered.Count}";
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) { ApplyFilters(); }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = ""; CityBox.Text = "";
            TypeBox.SelectedIndex = 0; DealTypeBox.SelectedIndex = 0; ApplyFilters();
        }

        private void ShowReadyDeals_Click(object sender, RoutedEventArgs e) { LoadReadyDeals(); }
        private void ShowAll_Click(object sender, RoutedEventArgs e) { LoadListings(); }

        private void BtnAddListing_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddListingDialog(_agentId, true);
            if (dlg.ShowDialog() == true)
            {
                if (BtnShowAll.Visibility == Visibility.Visible) LoadReadyDeals();
                else LoadListings();
            }
        }

        private void DealButton_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.Tag as ListingRow;
            if (row != null)
            {
                var dlg = new AddDealDialog(_agentId, row);
                if (dlg.ShowDialog() == true)
                {
                    MessageBox.Show("Сделка успешно оформлена!", "Успех");
                    if (BtnShowAll.Visibility == Visibility.Visible) LoadReadyDeals();
                    else LoadListings();
                }
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.Tag as ListingRow;
            if (row != null) new ListingDetailsWindow(row).ShowDialog();
        }

        private void BtnRequests_Click(object sender, RoutedEventArgs e) { new DataViewWindow("Заявки", "SELECT * FROM Zayavki").ShowDialog(); }
        private void BtnDeals_Click(object sender, RoutedEventArgs e) { new DataViewWindow("Сделки", "SELECT * FROM Sdelki").ShowDialog(); }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (BtnShowAll.Visibility == Visibility.Visible) LoadReadyDeals();
            else LoadListings();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e) { new Authorization().Show(); Close(); }
    }
}