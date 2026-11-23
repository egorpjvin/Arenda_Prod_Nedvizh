using System;
using System.Data.SqlClient;
using System.Windows;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class AddRequestDialog : Window
    {
        private readonly int _listingId;
        private readonly int _clientId;

        public AddRequestDialog(int listingId, int clientId, string listingTitle)
        {
            InitializeComponent();
            _listingId = listingId;
            _clientId = clientId;
            TitleText.Text = "Заявка на: " + listingTitle;
            DateBox.SelectedDate = DateTime.Today.AddDays(1);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextTextBox.Text))
            {
                MessageBox.Show("Введите текст заявки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "INSERT INTO Zayavki (KOD_Obyavleniya, KOD_Klienta, TekstZayavki, StatusZayavki, DataProsmotra) " +
                        "VALUES (@l, @c, @t, N'Новая', @d)", conn);
                    cmd.Parameters.AddWithValue("@l", _listingId);
                    cmd.Parameters.AddWithValue("@c", _clientId);
                    cmd.Parameters.AddWithValue("@t", TextTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@d", DateBox.SelectedDate ?? DateTime.Today);
                    cmd.ExecuteNonQuery();
                }
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании заявки:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}