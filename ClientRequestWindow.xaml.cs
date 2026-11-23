using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class ClientRequestsWindow : Window
    {
        private readonly int _userId;

        public ClientRequestsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT z.KOD_Zayavki, o.Zagolovok, z.StatusZayavki, z.DataProsmotra, z.GotovKSdelke 
                        FROM Zayavki z 
                        JOIN Obyavleniya o ON o.KOD_Obyavleniya = z.KOD_Obyavleniya 
                        WHERE z.KOD_Klienta = @u", conn);
                    cmd.Parameters.AddWithValue("@u", _userId);
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    Grid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заявок:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int id)
            {
                if (MessageBox.Show("Отозвать заявку?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = Db.GetConnection())
                        {
                            conn.Open();
                            new SqlCommand($"DELETE FROM Zayavki WHERE KOD_Zayavki = {id}", conn).ExecuteNonQuery();
                        }
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show("Ошибка удаления: " + ex.Message); }
                }
            }
        }

        private void ReadyForDeal_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int id)
            {
                try
                {
                    using (var conn = Db.GetConnection())
                    {
                        conn.Open();
                        new SqlCommand($"UPDATE Zayavki SET GotovKSdelke = 1 WHERE KOD_Zayavki = {id}", conn).ExecuteNonQuery();
                    }
                    MessageBox.Show("Агент уведомлен о вашей готовности к сделке!");
                    LoadData();
                }
                catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}