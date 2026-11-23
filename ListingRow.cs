namespace Prodazha_Arenda_Nedvizhki
{
    public class ListingRow
    {
        public int KOD_Obyavleniya { get; set; }
        public int KOD_Nedvizhimosti { get; set; }
        public int KOD_Polzovatelya { get; set; }
        public string Zagolovok { get; set; }
        public string TipObyavleniya { get; set; }
        public string TipObekta { get; set; }
        public string Gorod { get; set; }
        public string Adres { get; set; }
        public decimal Ploshchad { get; set; }
        public int Komnat { get; set; }
        public decimal Tsena { get; set; }
        public string StatusObyavleniya { get; set; }
        public int KolichestvoProsmotrov { get; set; }
        public string ImagePath { get; set; }
        public string Opisanie { get; set; }
        public int? Etazh { get; set; }
        public int? EtazheyVDome { get; set; }
        public int? GodPostroyki { get; set; }
        public bool CanEdit { get; set; }
        public bool IsMyListing { get; set; }
        public int? ReadyClientId { get; set; }
        public string ReadyClientName { get; set; }
    }
}