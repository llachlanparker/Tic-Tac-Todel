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
    private int? _currentUserId = null; // user login

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
        return;
    }

    // called by UserManager after successful login
    public void SetCurrentUser(int userId)
    {
        _currentUserId = userId;
        DisplayStats();
    }

    // Called by GameManager when game ends
    public void AddGameResult(string result, string tictactoeResult)
    {
        if (!_currentUserId.HasValue) return;

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                // Insert game record
                command.CommandText =
                    "INSERT INTO games (user_id, result, tictactoe_result, played_at) " +
                    "VALUES (" + _currentUserId + ", '" + SanitizeInput(result) + "', '" + 
                    SanitizeInput(tictactoeResult) + "', datetime('now'));";
                command.ExecuteNonQuery();

                // Update aggregated stats
                command.CommandText =
                    "INSERT INTO user_stats (user_id) VALUES (" + _currentUserId + ") " +
                    "ON CONFLICT(user_id) DO UPDATE SET " +
                    "total_games = total_games + 1, " +
                    "p1wins = p1wins + CASE WHEN '" + SanitizeInput(tictactoeResult) + "' = 'Player 1' THEN 1 ELSE 0 END, " +
                    "p2wins = p2wins + CASE WHEN '" + SanitizeInput(tictactoeResult) + "' = 'Player 2' THEN 1 ELSE 0 END, " +
                    "draws = draws + CASE WHEN '" + SanitizeInput(tictactoeResult) + "' = 'draw' THEN 1 ELSE 0 END;";
                command.ExecuteNonQuery();
            }
        }

        DisplayStats();
    }

    // Load and display stats for current user
    public void DisplayStats()
    {
        if (playerScores == null) return;
        
        if (!_currentUserId.HasValue)
        {
            playerScores.text = "Please login to view stats.";
            return;
        }

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = 
                    "SELECT total_games, p1wins, p2wins, draws " +
                    "FROM user_stats WHERE user_id = " + _currentUserId + ";";

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

    // Get recent game history for current user
    public List<GameRecord> GetRecentGames(int limit = 10)
    {
        var games = new List<GameRecord>();
        if (!_currentUserId.HasValue) return games;

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT result, tictactoe_result, played_at " +
                    "FROM games " +
                    "WHERE user_id = " + _currentUserId +
                    " ORDER BY played_at DESC LIMIT " + limit + ";";

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        games.Add(new GameRecord
                        {
                            result = reader["result"].ToString(),
                            tictactoeResult = reader["tictactoe_result"].ToString(),
                            playedAt = DateTime.Parse(reader["played_at"].ToString())
                        });
                    }
                }
            }
        }

        return games;
    }

    public class GameRecord
    {
        public string result;
        public string tictactoeResult;
        public DateTime playedAt;
    }

    private string SanitizeInput(string input) => input.Replace("'", "''");

    void CreateDB()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                // Games table - tracks each game
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS games (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        user_id INTEGER NOT NULL,
                        result TEXT NOT NULL,
                        tictactoe_result TEXT NOT NULL,
                        played_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(user_id) REFERENCES users(id)
                    );";
                command.ExecuteNonQuery();

                // User stats table - aggregated stats per user
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS user_stats (
                        user_id INTEGER PRIMARY KEY,
                        total_games INTEGER DEFAULT 0,
                        p1wins INTEGER DEFAULT 0,
                        p2wins INTEGER DEFAULT 0,
                        draws INTEGER DEFAULT 0
                    );";
                command.ExecuteNonQuery();
            }
        }

        Debug.Log("PlayerScores database initialized!");
    }
}