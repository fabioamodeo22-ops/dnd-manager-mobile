namespace DnDManager.Data
{
    /// <summary>Stato di sessione (non persistito): chi ha effettuato l'accesso, e in quale campagna si trova.</summary>
    public static class AppSession
    {
        public static string UserName { get; set; } = "Ospite";
        public static bool IsMaster { get; set; } = true;

        // Identità Supabase (valorizzati dopo il login online)
        public static System.Guid? UserId { get; set; }
        public static string Email { get; set; } = "";

        // Campagna attualmente selezionata
        public static long CampaignId { get; set; }
        public static string CampaignName { get; set; } = "";
    }
}
