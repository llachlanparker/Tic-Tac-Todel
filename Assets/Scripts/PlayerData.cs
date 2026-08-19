using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using TMPro;

public class PlayerData : MonoBehaviour
{
    public static PlayerData instance;

    public TMP_Text playerScores;

    private string dbName = "URI=file:PlayerScores.db";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateDB();
    }

    private void Start()
    {
        DisplayStats();
    }

    public void CreateDB()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "CREATE TABLE IF NOT EXISTS playerstats (" +
                    "wins INTEGER, " +
                    "losses INTEGER, " +
                    "draws INTEGER);";

                command.ExecuteNonQuery();
            }
        }

        Debug.Log("Database created!");
    }

    public void AddScores(int wins, int losses, int draws)
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "INSERT INTO playerstats (wins, losses, draws) " +
                    "VALUES (@wins, @losses, @draws);";

                command.Parameters.Add(new SqliteParameter("@wins", wins));
                command.Parameters.Add(new SqliteParameter("@losses", losses));
                command.Parameters.Add(new SqliteParameter("@draws", draws));

                command.ExecuteNonQuery();
            }
        }

        DisplayStats();
    }

    public void DisplayStats()
    {
        playerScores.text = "";

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM playerstats;";

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        playerScores.text +=
                            "Wins: " + reader["wins"] +
                            " | Losses: " + reader["losses"] +
                            " | Draws: " + reader["draws"] +
                            "\n";
                    }
                }
            }
        }
    }
}