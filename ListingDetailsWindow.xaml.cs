using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Prodazha_Arenda_Nedvizhki
{
    public partial class ListingDetailsWindow : Window
    {
        public ListingDetailsWindow(ListingRow row)
        {
            InitializeComponent();

            TitleText.Text = row.Zagolovok;
            PriceText.Text = $"{row.Tsena:N0} ₽";
            TypeText.Text = $"Тип: {row.TipObekta} ({row.TipObyavleniya})";
            CityText.Text = $"Город: {row.Gorod}";
            AddressText.Text = $"Адрес: {row.Adres}";
            RoomsText.Text = $"Комнат: {row.Komnat}";
            AreaText.Text = $"Площадь: {row.Ploshchad:N2} м²";
            FloorText.Text = row.Etazh.HasValue ? $"Этаж: {row.Etazh} из {row.EtazheyVDome}" : "Этаж: не указан";
            YearText.Text = row.GodPostroyki.HasValue ? $"Год постройки: {row.GodPostroyki}" : "Год постройки: не указан";
            DescriptionText.Text = string.IsNullOrEmpty(row.Opisanie) ? "Описание отсутствует." : row.Opisanie;
            if (!string.IsNullOrEmpty(row.ImagePath))
            {
                try
                {
                    if (row.ImagePath.StartsWith("pack://"))
                    {
                        BigImage.Source = new BitmapImage(new Uri(row.ImagePath, UriKind.Absolute));
                    }
                    else
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, row.ImagePath);
                        if (File.Exists(fullPath))
                        {
                            BigImage.Source = new BitmapImage(new Uri(fullPath));
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}