using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class AddPaymentDialog : Window
    {
        public AddPaymentDialog() { InitializeComponent(); }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DealIdBox.Text, out int did) ||
                !decimal.TryParse(SumBox.Text.Replace('.', ','), out decimal sum))
            {
                MessageBox.Show("Проверьте числа!");
                return;
            }

            string method = (MethodBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string purpose = (PurposeBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            using (var conn = Db.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("INSERT INTO Platezhi (KOD_Sdelki, SummaPlatezha, SposobOplaty, NaznacheniePlatezha) VALUES (@d, @s, @m, @p)", conn);
                cmd.Parameters.AddWithValue("@d", did);
                cmd.Parameters.AddWithValue("@s", sum);
                cmd.Parameters.AddWithValue("@m", method);
                cmd.Parameters.AddWithValue("@p", purpose);
                cmd.ExecuteNonQuery();
            }
            DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}