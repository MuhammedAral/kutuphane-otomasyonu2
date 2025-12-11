namespace KutuphaneOtomasyon
{
    public static class CurrentSession
    {
        public static int? UserId { get; set; }
        public static string? AdSoyad { get; set; }
        public static string? Rol { get; set; }

        public static void Clear()
        {
            UserId = null;
            AdSoyad = null;
            Rol = null;
        }
    }
}
