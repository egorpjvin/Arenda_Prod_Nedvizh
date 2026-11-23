using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class AddListingDialog : Window
    {
        private readonly int _currentUserId;
        private readonly bool _isAgent;
        private readonly ListingRow _editRow;
        private string _selectedImagePath = null;
        public AddListingDialog(int userId, bool isAgent = false, ListingRow row = null)
        {
            InitializeComponent();
            _currentUserId = userId;
            _isAgent = isAgent;
            _editRow = row;

            if (_isAgent && _editRow == null)
            {
                OwnerPanel.Visibility = Visibility.Visible;
                LoadOwners();
            }

            if (_editRow != null)
            {
                this.Title = "Редактирование объявления";

                ObjectTypeBox.Text = _editRow.TipObekta;
                TitleBox.Text = _editRow.Zagolovok;
                AddressBox.Text = _editRow.Adres;
                CityBox.Text = _editRow.Gorod;
                AreaBox.Text = _editRow.Ploshchad.ToString();
                RoomsBox.Text = _editRow.Komnat.ToString();
                DealTypeBox.Text = _editRow.TipObyavleniya;
                PriceBox.Text = _editRow.Tsena.ToString();
                DescriptionBox.Text = _editRow.Opisanie;

                if (_editRow.Etazh.HasValue) FloorBox.Text = _editRow.Etazh.ToString();
                if (_editRow.EtazheyVDome.HasValue) TotalFloorsBox.Text = _editRow.EtazheyVDome.ToString();
                if (_editRow.GodPostroyki.HasValue) YearBox.Text = _editRow.GodPostroyki.ToString();

                if (!string.IsNullOrEmpty(_editRow.ImagePath))
                {
                    _selectedImagePath = _editRow.ImagePath;
                    try { PreviewImage.Source = new BitmapImage(new Uri(_editRow.ImagePath)); } catch { }
                }
            }
        }

        private void LoadOwners()
        {
            using (var conn = Db.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT KOD_Polzovatelya, Familiya + ' ' + Imya FROM Polzovateli WHERE Rol = N'Клиент'", conn);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        OwnerBox.Items.Add(new ComboBoxItem { Content = r.GetString(1), Tag = r.GetInt32(0) });
                }
                if (OwnerBox.Items.Count > 0) OwnerBox.SelectedIndex = 0;
            }
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Изображения (*.jpg;*.png)|*.jpg;*.png" };
            if (dlg.ShowDialog() == true)
            {
                string imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesFolder)) Directory.CreateDirectory(imagesFolder);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(dlg.FileName);
                string destPath = Path.Combine(imagesFolder, fileName);
                File.Copy(dlg.FileName, destPath, true);
                _selectedImagePath = "pack://siteoforigin:,,,/Images/" + fileName;
                PreviewImage.Source = new BitmapImage(new Uri(destPath));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int ownerId = _editRow != null ? _editRow.KOD_Polzovatelya : _currentUserId;
            if (_isAgent && _editRow == null && OwnerBox.SelectedItem is ComboBoxItem item)
                ownerId = (int)item.Tag;

            string tip = (ObjectTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? ObjectTypeBox.Text;
            string dealType = (DealTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? DealTypeBox.Text;

            if (!decimal.TryParse(AreaBox.Text.Replace('.', ','), out decimal area) || !decimal.TryParse(PriceBox.Text.Replace('.', ','), out decimal price) || !int.TryParse(RoomsBox.Text, out int rooms))
            { MessageBox.Show("Проверьте числа!"); return; }

            int.TryParse(FloorBox.Text, out int fl);
            int.TryParse(TotalFloorsBox.Text, out int tf);
            int.TryParse(YearBox.Text, out int yr);

            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            if (_editRow == null)
                            {
                                var cmdN = new SqlCommand(@"INSERT INTO Nedvizhimost (KOD_Polzovatelya, TipObekta, Adres, Gorod, Rayon, Ploshchad, Komnat, Etazh, EtazheyVDome, GodPostroyki, Opisanie, ImagePath) VALUES (@u, @t, @a, @g, @r, @p, @k, @fl, @tf, @yr, @o, @img); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
                                cmdN.Parameters.AddWithValue("@u", ownerId);
                                cmdN.Parameters.AddWithValue("@t", tip);
                                cmdN.Parameters.AddWithValue("@a", AddressBox.Text);
                                cmdN.Parameters.AddWithValue("@g", CityBox.Text);
                                cmdN.Parameters.AddWithValue("@r", DistrictBox.Text);
                                cmdN.Parameters.AddWithValue("@p", area);
                                cmdN.Parameters.AddWithValue("@k", rooms);
                                cmdN.Parameters.AddWithValue("@fl", fl == 0 ? (object)DBNull.Value : fl);
                                cmdN.Parameters.AddWithValue("@tf", tf == 0 ? (object)DBNull.Value : tf);
                                cmdN.Parameters.AddWithValue("@yr", yr == 0 ? (object)DBNull.Value : yr);
                                cmdN.Parameters.AddWithValue("@o", string.IsNullOrEmpty(DescriptionBox.Text) ? (object)DBNull.Value : DescriptionBox.Text);
                                cmdN.Parameters.AddWithValue("@img", _selectedImagePath ?? (object)DBNull.Value);
                                int newId = (int)cmdN.ExecuteScalar();

                                var cmdO = new SqlCommand("INSERT INTO Obyavleniya (KOD_Nedvizhimosti, KOD_Polzovatelya, KOD_Agenta, TipObyavleniya, Zagolovok, Tsena, StatusObyavleniya) VALUES (@n, @u, @a, @t, @z, @c, N'Активно')", conn, tx);
                                cmdO.Parameters.AddWithValue("@n", newId);
                                cmdO.Parameters.AddWithValue("@u", ownerId);
                                cmdO.Parameters.AddWithValue("@a", _isAgent ? _currentUserId : (object)DBNull.Value);
                                cmdO.Parameters.AddWithValue("@t", dealType);
                                cmdO.Parameters.AddWithValue("@z", TitleBox.Text);
                                cmdO.Parameters.AddWithValue("@c", price);
                                cmdO.ExecuteNonQuery();
                            }
                            else
                            {
                                var cmdN = new SqlCommand(@"UPDATE Nedvizhimost SET TipObekta=@t, Adres=@a, Gorod=@g, Rayon=@r, Ploshchad=@p, Komnat=@k, Etazh=@fl, EtazheyVDome=@tf, GodPostroyki=@yr, Opisanie=@o, ImagePath=@img WHERE KOD_Nedvizhimosti=@id", conn, tx);
                                cmdN.Parameters.AddWithValue("@id", _editRow.KOD_Nedvizhimosti);
                                cmdN.Parameters.AddWithValue("@t", tip);
                                cmdN.Parameters.AddWithValue("@a", AddressBox.Text);
                                cmdN.Parameters.AddWithValue("@g", CityBox.Text);
                                cmdN.Parameters.AddWithValue("@r", DistrictBox.Text);
                                cmdN.Parameters.AddWithValue("@p", area);
                                cmdN.Parameters.AddWithValue("@k", rooms);
                                cmdN.Parameters.AddWithValue("@fl", fl == 0 ? (object)DBNull.Value : fl);
                                cmdN.Parameters.AddWithValue("@tf", tf == 0 ? (object)DBNull.Value : tf);
                                cmdN.Parameters.AddWithValue("@yr", yr == 0 ? (object)DBNull.Value : yr);
                                cmdN.Parameters.AddWithValue("@o", string.IsNullOrEmpty(DescriptionBox.Text) ? (object)DBNull.Value : DescriptionBox.Text);
                                cmdN.Parameters.AddWithValue("@img", _selectedImagePath ?? (object)DBNull.Value);
                                cmdN.ExecuteNonQuery();

                                var cmdO = new SqlCommand("UPDATE Obyavleniya SET TipObyavleniya=@t, Zagolovok=@z, Tsena=@c WHERE KOD_Obyavleniya=@id", conn, tx);
                                cmdO.Parameters.AddWithValue("@id", _editRow.KOD_Obyavleniya);
                                cmdO.Parameters.AddWithValue("@t", dealType);
                                cmdO.Parameters.AddWithValue("@z", TitleBox.Text);
                                cmdO.Parameters.AddWithValue("@c", price);
                                cmdO.ExecuteNonQuery();
                            }
                            tx.Commit(); DialogResult = true;
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    }
}