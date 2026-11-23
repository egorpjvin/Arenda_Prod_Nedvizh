using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class AddDealDialog : Window
    {
        private readonly int _agentId;
        private readonly ListingRow _listing;
        private int? _preSelectedBuyerId;

        public AddDealDialog(int agentId, ListingRow listing)
        {
            InitializeComponent();
            _agentId = agentId;
            _listing = listing;
            ListingTitleText.Text = $"Сделка по объекту: {listing.Zagolovok}";
            SumBox.Text = listing.Tsena.ToString();

            if (listing.ReadyClientId.HasValue)
            {
                _preSelectedBuyerId = listing.ReadyClientId;
                ListingTitleText.Text += $"\n(Выбран клиент: {listing.ReadyClientName})";
            }

            LoadClients();
        }

        private void LoadClients()
        {
            using (var conn = Db.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT KOD_Polzovatelya, Familiya + ' ' + Imya FROM Polzovateli WHERE Rol = N'Клиент'", conn);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var item = new ComboBoxItem { Content = r.GetString(1), Tag = r.GetInt32(0) };
                        BuyerBox.Items.Add(item);

                        // Автоматически выбираем нужного клиента
                        if (_preSelectedBuyerId.HasValue && (int)item.Tag == _preSelectedBuyerId.Value)
                            BuyerBox.SelectedItem = item;
                    }
                }
                if (BuyerBox.SelectedItem == null && BuyerBox.Items.Count > 0) BuyerBox.SelectedIndex = 0;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (BuyerBox.SelectedItem == null) { MessageBox.Show("Выберите покупателя!"); return; }
            int buyerId = (int)((ComboBoxItem)BuyerBox.SelectedItem).Tag;

            if (!decimal.TryParse(SumBox.Text.Replace('.', ','), out decimal sum) || !decimal.TryParse(FeeBox.Text.Replace('.', ','), out decimal fee))
            { MessageBox.Show("Проверьте числа!"); return; }

            string dealType = (DealTypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            using (var conn = Db.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("INSERT INTO Sdelki (KOD_Obyavleniya, KOD_Pokupatelya, KOD_Prodavtsa, KOD_Agenta, TipSdelki, StatusSdelki, SummaSdelki, KomissiyaAgenta) VALUES (@l, @b, @s, @a, @t, N'В процессе', @sum, @f)", conn);
                cmd.Parameters.AddWithValue("@l", _listing.KOD_Obyavleniya);
                cmd.Parameters.AddWithValue("@b", buyerId);
                cmd.Parameters.AddWithValue("@s", _listing.KOD_Polzovatelya);
                cmd.Parameters.AddWithValue("@a", _agentId);
                cmd.Parameters.AddWithValue("@t", dealType);
                cmd.Parameters.AddWithValue("@sum", sum);
                cmd.Parameters.AddWithValue("@f", fee);
                cmd.ExecuteNonQuery();

                // После оформления сделки сбрасываем флаг готовности у заявки
                var cmdUpd = new SqlCommand("UPDATE Zayavki SET StatusZayavki = N'Выполнена', GotovKSdelke = 0 WHERE KOD_Obyavleniya = @l AND KOD_Klienta = @b", conn);
                cmdUpd.Parameters.AddWithValue("@l", _listing.KOD_Obyavleniya);
                cmdUpd.Parameters.AddWithValue("@b", buyerId);
                cmdUpd.ExecuteNonQuery();
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}