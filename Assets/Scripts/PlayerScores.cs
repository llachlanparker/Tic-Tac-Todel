using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using TMPro;

public class PlayerScores : MonoBehaviour
{
    public static PlayerScores instance;
    public TMP_Text playerScores;

    private string dbName;

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

        // store in persistent path
        dbName = "URI=file:" + Application.persistentDataPath + "/PlayerScores.db";

        CreateDB();
    }

    void Start()
    {
        Debug.Log("Database path: " + dbName);
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
                    "id INTEGER PRIMARY KEY CHECK (id = 1), " +
                    "player1wins INTEGER DEFAULT 0, " +
                    "player2wins INTEGER DEFAULT 0, " +
                    "draws INTEGER DEFAULT 0);";

                command.ExecuteNonQuery();

                // Insert a starting row if the table is empty
                command.CommandText =
                    "INSERT OR IGNORE INTO playerstats (id, player1wins, player2wins, draws) " +
                    "VALUES (1, 0, 0, 0);";

                command.ExecuteNonQuery();
            }
        }
    }

    public void AddScores(int p1Wins, int p2Wins, int draws)
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "UPDATE playerstats SET " +
                    "player1wins = player1wins + " + p1Wins + ", " +
                    "player2wins = player2wins + " + p2Wins + ", " +
                    "draws = draws + " + draws + " " +
                    "WHERE id = 1;";

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
                command.CommandText = "SELECT player1wins, player2wins, draws FROM playerstats WHERE id = 1;";

                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        playerScores.text =
                            "P1 Wins: " + reader["player1wins"] +
                            " | P2 Wins: " + reader["player2wins"] +
                            " | Draws: " + reader["draws"];
                    }
                }
            }
        }
    }
}