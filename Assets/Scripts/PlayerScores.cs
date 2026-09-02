using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using TMPro;
using System.Collections.Generic;
using System;

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
        return;
    }

    // Called by GameManager when game ends
    public void AddGameResult(string tictactoeResult)
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE player_stats
                    SET
                        total_games = total_games + 1,
                        p1wins = p1wins +
                            CASE WHEN @winner = 'Player 1' THEN 1 ELSE 0 END,
                        p2wins = p2wins +
                            CASE WHEN @winner = 'Player 2' THEN 1 ELSE 0 END,
                        draws = draws +
                            CASE WHEN @winner = 'draw' THEN 1 ELSE 0 END;
                ";

                command.Parameters.AddWithValue("@winner", tictactoeResult);
                command.ExecuteNonQuery();
            }
        }

        DisplayStats();
    }

    // Load and display stats for current user
    public void DisplayStats()
    {
        if (playerScores == null) return;

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = 
                    "SELECT total_games, p1wins, p2wins, draws " +
                    "FROM player_stats";

                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int totalGames = Convert.ToInt32(reader["total_games"]);
                        int p1Wins = Convert.ToInt32(reader["p1wins"]);
                        int p2Wins = Convert.ToInt32(reader["p2wins"]);
                        int draws = Convert.ToInt32(reader["draws"]);

                        playerScores.text =
                            "Games: " + totalGames +
                            " | P1 Wins: " + p1Wins +
                            " | P2 Wins: " + p2Wins +
                            " | Draws: " + draws;
                    }
                }
            }
        }
    }

    void CreateDB()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS player_stats (
                        total_games INTEGER DEFAULT 0,
                        p1wins INTEGER DEFAULT 0,
                        p2wins INTEGER DEFAULT 0,
                        draws INTEGER DEFAULT 0
                    );

                    INSERT INTO player_stats
                        (total_games, p1wins, p2wins, draws)
                    SELECT 0, 0, 0, 0
                    WHERE NOT EXISTS (
                        SELECT 1 FROM player_stats
                    );
                ";

                command.ExecuteNonQuery();
            }
        }

        Debug.Log("PlayerScores database made");
    }
}