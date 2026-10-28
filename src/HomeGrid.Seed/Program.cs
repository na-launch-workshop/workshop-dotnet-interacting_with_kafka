// Seeds data/appliances.db with reference data for the Appliance Reference panel.
// Usage: dotnet run --project src/HomeGrid.Seed

using HomeGrid;
using Microsoft.Data.Sqlite;

var db = Path.Combine(RepoRoot.Find(), "data", "appliances.db");

(string Name, double DailyKwh)[] appliances =
[
    ("Refrigerator",             1.2),
    ("Air Conditioner",          3.5),
    ("Electric Vehicle Charger", 9.6),
    ("Washing Machine",          1.8),
    ("Tumble Dryer",             4.5),
    ("Dishwasher",               1.4),
    ("Electric Oven",            2.3),
    ("Electric Water Heater",    4.0),
    ("Laptop",                   0.05),
    ("LED TV",                   0.1),
];

Directory.CreateDirectory(Path.GetDirectoryName(db)!);

using var conn = new SqliteConnection($"Data Source={db};Pooling=False");
conn.Open();

using (var create = conn.CreateCommand())
{
    create.CommandText = """
        CREATE TABLE IF NOT EXISTS appliances (
            name      TEXT PRIMARY KEY,
            daily_kwh REAL NOT NULL
        )
        """;
    create.ExecuteNonQuery();
}

using var tx = conn.BeginTransaction();
using var insert = conn.CreateCommand();
insert.CommandText = "INSERT OR IGNORE INTO appliances (name, daily_kwh) VALUES ($name, $dailyKwh)";
var name = insert.Parameters.Add("$name", SqliteType.Text);
var dailyKwh = insert.Parameters.Add("$dailyKwh", SqliteType.Real);

foreach (var appliance in appliances)
{
    name.Value = appliance.Name;
    dailyKwh.Value = appliance.DailyKwh;
    insert.ExecuteNonQuery();
}

tx.Commit();
Console.WriteLine($"Seeded {appliances.Length} appliances into {db}");
