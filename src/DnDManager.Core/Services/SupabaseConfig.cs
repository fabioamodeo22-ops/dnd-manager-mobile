namespace DnDManager.Services
{
    /// <summary>
    /// Coordinate del progetto Supabase condiviso dal gruppo. La chiave "anon public" non è un
    /// segreto in senso stretto: è pensata per stare nel codice del client, protetta poi dalle
    /// regole di sicurezza (Row Level Security) impostate sul database — chi non ha un account
    /// autenticato non può leggere/scrivere nulla, indipendentemente da questa chiave.
    /// </summary>
    public static class SupabaseConfig
    {
        public const string Url = "https://ukkkohtkgiagbdyqwlpg.supabase.co";
        public const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVra2tvaHRrZ2lhZ2JkeXF3bHBnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODU3NDUwNTYsImV4cCI6MjEwMTMyMTA1Nn0.pWy0LEqeJgF9cLxRjxn-FXcwCaTajzvDNOwFQS5jD5o";
    }
}
