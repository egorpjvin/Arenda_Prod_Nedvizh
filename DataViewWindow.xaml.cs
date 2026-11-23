using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class DataViewWindow : Window
    {
        private readonly string _sql;
        private readonly SqlParameter[] _pars;

        public DataViewWindow(string title, string sql, params SqlParameter[] pars)
        {
            InitializeComponent();
            Title = title; TitleText.Text = title;
            _sql = sql; _pars = pars ?? new SqlParameter[0];
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(_sql, conn);
                    if (_pars.Length > 0) cmd.Parameters.AddRange(_pars);
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    Grid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) 
        { 
            Close(); 
        }
    }
}