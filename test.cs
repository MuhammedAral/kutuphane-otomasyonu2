using Npgsql;

var connStr = "Host=aws-0-eu-central-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.pnilfypxicpgbwmrfqlw;Password=muhammedali2006";
using var conn = new NpgsqlConnection(connStr);
conn.Open();

Console.WriteLine("=== KITAPLAR (MevcutAdet > 0) ===");
using var cmd = new NpgsqlCommand("SELECT KitapID, Baslik, MevcutAdet FROM Kitaplar WHERE MevcutAdet > 0 LIMIT 5", conn);
using var reader = cmd.ExecuteReader();
int count = 0;
while (reader.Read())
{
    Console.WriteLine($"{reader["KitapID"]}: {reader["Baslik"]} - Mevcut: {reader["MevcutAdet"]}");
    count++;
}
Console.WriteLine($"Toplam: {count} kitap");
