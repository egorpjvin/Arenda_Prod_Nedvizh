using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class ClientMenu : Window
    {
        private readonly string _login;
        private readonly string _name;
        private readonly string _role;
        private readonly List<ListingRow> _allListings = new List<ListingRow>();
        private int _userId = 0;
        private bool _isMyListingsMode = false;

        public ClientMenu(string login, string name, string role)
        {
            InitializeComponent();
            _login = login; _name = name; _role = role;
            UserInfoText.Text = $"Пользователь: {_name}  •  Логин: {_login}  •  Роль: {_role}";
            _userId = LoadUserId();
            InitFilters();
            LoadListings();
        }

        private int LoadUserId()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT KOD_Polzovatelya FROM Polzovateli WHERE Login = @l", conn);
                    cmd.Parameters.AddWithValue("@l", _login);
                    var r = cmd.ExecuteScalar();
                    return r == null ? 0 : Convert.ToInt32(r);
                }
            }
            catch { return 0; }
        }

        private void InitFilters()
        {
            TypeBox.Items.Add("Все типы"); TypeBox.Items.Add("Квартира"); TypeBox.Items.Add("Дом"); TypeBox.Items.Add("Коммерческая"); TypeBox.Items.Add("Участок");
            TypeBox.SelectedIndex = 0;
            DealTypeBox.Items.Add("Все сделки"); DealTypeBox.Items.Add("Продажа"); DealTypeBox.Items.Add("Аренда");
            DealTypeBox.SelectedIndex = 0;
        }

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
                            int ownerId = r.GetInt32(2);
                            _allListings.Add(new ListingRow
                            {
                                KOD_Obyavleniya = r.GetInt32(0),
                                KOD_Nedvizhimosti = r.GetInt32(1),
                                KOD_Polzovatelya = ownerId,
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
                                IsMyListing = (ownerId == _userId) // Определяем, является ли объявление собственным
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки:\n" + ex.Message); }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (ListingsList == null) return;
            string search = (SearchBox.Text ?? "").Trim().ToLower();
            string city = (CityBox.Text ?? "").Trim().ToLower();
            string type = TypeBox.SelectedItem as string;
            string dealType = DealTypeBox.SelectedItem as string;
            decimal priceFrom = 0, priceTo = decimal.MaxValue, parsed;
            if (decimal.TryParse(PriceFromBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) && parsed > 0) priceFrom = parsed;
            if (decimal.TryParse(PriceToBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) && parsed > 0) priceTo = parsed;

            var filtered = new List<ListingRow>();
            foreach (var l in _allListings)
            {
                // Фильтрация по режиму (Мои / Все)
                if (_isMyListingsMode)
                {
                    if (!l.IsMyListing) continue; // В режиме "Мои" показываем только свои
                }
                else
                {
                    if (l.IsMyListing) continue; // В общем списке скрываем свои
                }

                if (!string.IsNullOrEmpty(search) && !((l.Zagolovok ?? "").ToLower().Contains(search) || (l.Adres ?? "").ToLower().Contains(search) || (l.Gorod ?? "").ToLower().Contains(search))) continue;
                if (!string.IsNullOrEmpty(city) && !(l.Gorod ?? "").ToLower().Contains(city)) continue;
                if (!string.IsNullOrEmpty(type) && type != "Все типы" && l.TipObekta != type) continue;
                if (!string.IsNullOrEmpty(dealType) && dealType != "Все сделки" && l.TipObyavleniya != dealType) continue;
                if (l.Tsena < priceFrom || l.Tsena > priceTo) continue;
                filtered.Add(l);
            }
            ListingsList.ItemsSource = filtered;
            CountText.Text = $"Найдено: {filtered.Count}";
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) { ApplyFilters(); }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = ""; CityBox.Text = ""; PriceFromBox.Text = ""; PriceToBox.Text = "";
            TypeBox.SelectedIndex = 0; DealTypeBox.SelectedIndex = 0; ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _isMyListingsMode = false;
            ListHeaderText.Text = "Действующие объявления";
            LoadListings();
        }

        private void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.Tag as ListingRow;
            if (row == null) return;
            var dlg = new AddRequestDialog(row.KOD_Obyavleniya, _userId, row.Zagolovok);
            if (dlg.ShowDialog() == true) MessageBox.Show("Заявка отправлена агенту.", "Успех");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.Tag as ListingRow;
            if (row != null)
            {
                // false = не агент, передаем row для редактирования
                var dlg = new AddListingDialog(_userId, false, row);
                if (dlg.ShowDialog() == true) LoadListings();
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var row = (sender as Button)?.Tag as ListingRow;
            if (row != null) new ListingDetailsWindow(row).ShowDialog();
        }

        private void Card_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            var row = item?.DataContext as ListingRow;
            if (row != null) new ListingDetailsWindow(row).ShowDialog();
        }

        private void BtnAddListing_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddListingDialog(_userId);
            if (dlg.ShowDialog() == true) LoadListings();
        }

        private void BtnMyListings_Click(object sender, RoutedEventArgs e)
        {
            _isMyListingsMode = true;
            ListHeaderText.Text = "Мои объявления";
            ApplyFilters();
        }

        private void BtnMyRequests_Click(object sender, RoutedEventArgs e)
        {
            new ClientRequestsWindow(_userId).ShowDialog();
        }

        private void BtnMyDeals_Click(object sender, RoutedEventArgs e)
        {
            new DataViewWindow("Мои сделки", "SELECT s.KOD_Sdelki, o.Zagolovok, s.TipSdelki, s.StatusSdelki, s.SummaSdelki, s.DataSdelki FROM Sdelki s JOIN Obyavleniya o ON o.KOD_Obyavleniya = s.KOD_Obyavleniya WHERE s.KOD_Pokupatelya = @u", new SqlParameter("@u", _userId)).ShowDialog();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) { new Authorization().Show(); Close(); }
    }
}