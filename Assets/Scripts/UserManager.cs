using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using TMPro;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System;

public class UserManager : MonoBehaviour
{
    public static UserManager instance;

    [Header("Database")]
    private string dbName;
    private int? _currentUserId = null;

    [Header("UI")]
    public TextMeshProUGUI authMessageText;
    public TextMeshProUGUI welcomeText;
    public TMP_InputField loginUsernameField;
    public TMP_InputField loginPasswordField;
    public TMP_InputField registerUsernameField;
    public TMP_InputField registerPasswordField;
    public GameObject loginPanel;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dbName = "URI=file:" + Application.persistentDataPath + "/User.db";
        InitializeDatabase();
        ConfigurePasswordFields();
    }


    private void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                // Users table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username TEXT UNIQUE NOT NULL,
                        password_hash TEXT NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";
                command.ExecuteNonQuery();

                // Games table (records each game)
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS games (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        user_id INTEGER NOT NULL,
                        result TEXT NOT NULL,
                        wordle_solved BOOLEAN NOT NULL,
                        tictactoe_result TEXT NOT NULL,
                        played_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(user_id) REFERENCES users(id)
                    );";
                command.ExecuteNonQuery();

                // User stats table (aggregated)
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS user_stats (
                        user_id INTEGER PRIMARY KEY,
                        total_games INTEGER DEFAULT 0,
                        wins INTEGER DEFAULT 0,
                        losses INTEGER DEFAULT 0,
                        draws INTEGER DEFAULT 0,
                        wordles_solved INTEGER DEFAULT 0,
                        FOREIGN KEY(user_id) REFERENCES users(id)
                    );";
                command.ExecuteNonQuery();
            }
        }

        Debug.Log("User database initialized!");
    }

    // password auth
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    private string SanitizeInput(string input) => input.Replace("'", "''");

    // register user
    public bool RegisterUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            SetAuthMessage("Username must be at least 3 characters!", false);
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
        {
            SetAuthMessage("Password must be at least 4 characters!", false);
            return false;
        }

        try
        {
            using (var connection = new SqliteConnection(dbName))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Check if username exists
                    command.CommandText = "SELECT COUNT(*) FROM users WHERE username = '" + SanitizeInput(username) + "';";
                    object countObj = command.ExecuteScalar();
                    int count = countObj != null && countObj != DBNull.Value 
                        ? Convert.ToInt32(countObj) 
                        : 0;

                    if (count > 0)
                    {
                        SetAuthMessage("Username already taken!", false);
                        return false;
                    }

                    // Insert new user
                    command.CommandText =
                        "INSERT INTO users (username, password_hash) VALUES ('" + 
                        SanitizeInput(username) + "', '" + HashPassword(password) + "');";
                    command.ExecuteNonQuery();

                    // Get new user ID and create stats entry
                    command.CommandText = "SELECT id FROM users WHERE username = '" + SanitizeInput(username) + "';";
                    object userIdObj = command.ExecuteScalar();
                    int userId = userIdObj != null && userIdObj != DBNull.Value 
                        ? Convert.ToInt32(userIdObj) 
                        : 0;

                    command.CommandText = "INSERT INTO user_stats (user_id) VALUES (" + userId + ");";
                    command.ExecuteNonQuery();

                    SetAuthMessage("Registration successful! Please login.", true);
                    Debug.Log("User registered: " + username);
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            SetAuthMessage("Registration failed: " + e.Message, false);
            Debug.LogError(e);
            return false;
        }
    }
    // login user
    public bool LoginUser(string username, string password)
    {
        try
        {
            using (var connection = new SqliteConnection(dbName))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT id FROM users WHERE username = '" + 
                        SanitizeInput(username) + 
                        "' AND password_hash = '" + HashPassword(password) + "';";

                    object result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        _currentUserId = Convert.ToInt32(result);
                        SetAuthMessage("Welcome back, " + username + "!", true);
                        LoadUserStats();
                        return true;
                    }
                    else
                    {
                        SetAuthMessage("Invalid username or password!", false);
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            SetAuthMessage("Login failed: " + e.Message, false);
            Debug.LogError(e);
            return false;
        }
    }

    // logout user
    public void LogoutUser()
    {
        _currentUserId = null;
        SetAuthMessage("Logged out successfully.", true);
        
        if (welcomeText != null)
            welcomeText.text = "";
    }

    public bool IsLoggedIn() => _currentUserId.HasValue;
    public int GetCurrentUserId() => _currentUserId ?? 0;

    // record game data (scores)
    public void RecordGame(string result, bool wordleSolved, string tictactoeResult)
    {
        if (!_currentUserId.HasValue) return;

        try
        {
            using (var connection = new SqliteConnection(dbName))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    // Insert game record
                    command.CommandText =
                        "INSERT INTO games (user_id, result, wordle_solved, tictactoe_result) " +
                        "VALUES (" + _currentUserId + ", '" + result + "', " + 
                        Convert.ToInt32(wordleSolved) + ", '" + SanitizeInput(tictactoeResult) + "');";
                    command.ExecuteNonQuery();

                    // Update stats
                    command.CommandText =
                        "UPDATE user_stats SET " +
                        "total_games = total_games + 1, " +
                        "wins = CASE WHEN '" + result + "' = 'win' THEN wins + 1 ELSE wins END, " +
                        "losses = CASE WHEN '" + result + "' = 'loss' THEN losses + 1 ELSE losses END, " +
                        "draws = CASE WHEN '" + result + "' = 'draw' THEN draws + 1 ELSE draws END, " +
                        "wordles_solved = CASE WHEN " + Convert.ToInt32(wordleSolved) + " = 1 THEN wordles_solved + 1 ELSE wordles_solved END " +
                        "WHERE user_id = " + _currentUserId + ";";
                    command.ExecuteNonQuery();

                    Debug.Log("Game recorded for user " + _currentUserId);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to record game: " + e.Message);
        }
    }

    // load recorded data
    public void LoadUserStats()
    {
        if (!_currentUserId.HasValue)
        {
            if (welcomeText != null)
                welcomeText.text = "Please login.";
            return;
        }
        try
        {
            using (var connection = new SqliteConnection(dbName))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT u.username, s.total_games, s.wins, s.losses, s.draws, s.wordles_solved " +
                        "FROM users u JOIN user_stats s ON u.id = s.user_id " +
                        "WHERE u.id = " + _currentUserId + ";";

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string username = reader["username"].ToString();
                            int totalGames = Convert.ToInt32(reader["total_games"]);
                            int wins = Convert.ToInt32(reader["wins"]);
                            int losses = Convert.ToInt32(reader["losses"]);
                            int draws = Convert.ToInt32(reader["draws"]);
                            int wordlesSolved = Convert.ToInt32(reader["wordles_solved"]);

                            if (welcomeText != null)
                                welcomeText.text = "Welcome, " + username + "!";

                            // Optionally update global PlayerScores display
                            if (PlayerScores.instance != null && PlayerScores.instance.playerScores != null)
                            {
                                PlayerScores.instance.playerScores.text =
                                    "Total: " + totalGames +
                                    " | Wins: " + wins +
                                    " | Losses: " + losses +
                                    " | Draws: " + draws +
                                    " | Words Solved: " + wordlesSolved;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load stats: " + e.Message);
        }
    }

    public List<GameRecord> GetRecentGames(int limit = 10)
    {
        var games = new List<GameRecord>();

        if (!_currentUserId.HasValue) return games;

        try
        {
            using (var connection = new SqliteConnection(dbName))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT result, wordle_solved, tictactoe_result, played_at " +
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
                                wordleSolved = (int)reader["wordle_solved"] == 1,
                                tictactoeResult = reader["tictactoe_result"].ToString(),
                                playedAt = DateTime.Parse(reader["played_at"].ToString())
                            });
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to get games: " + e.Message);
        }

        return games;
    }

    public class GameRecord
    {
        public string result;
        public bool wordleSolved;
        public string tictactoeResult;
        public DateTime playedAt;
    }

    public void SetAuthMessage(string message, bool success)
    {
        if (authMessageText != null)
        {
            authMessageText.color = success ? Color.green : Color.red;
            authMessageText.text = message;
        }
    }

    // hide password text
    private void ConfigurePasswordFields()
    {
        if (loginPasswordField != null)
        {
            loginPasswordField.contentType = TMP_InputField.ContentType.Password;
            loginPasswordField.characterLimit = 100;
        }

        if (registerPasswordField != null)
        {
            registerPasswordField.contentType = TMP_InputField.ContentType.Password;
            registerPasswordField.characterLimit = 100;
        }
    }

    // Buttons
    public void OnLoginButton()
    {
        if (LoginUser(loginUsernameField.text, loginPasswordField.text))
        {
            loginPanel.SetActive(false);  // Hide login panel on successful login
            ClearLoginFormFields(); // clear fields
        }
    }

    public void OnRegisterButton()
    {
        if (RegisterUser(registerUsernameField.text, registerPasswordField.text))
        {
            ClearLoginFormFields(); 
            // show "please login" message instead
        }
    }

    public void OnLogoutButton()
    {
        LogoutUser();
        loginPanel.SetActive(true);   // Show login panel again
    }

    // helper to clear input fields
    private void ClearLoginFormFields()
    {
        if (loginUsernameField != null) loginUsernameField.text = "";
        if (loginPasswordField != null) loginPasswordField.text = "";
        if (registerUsernameField != null) registerUsernameField.text = "";
        if (registerPasswordField != null) registerPasswordField.text = "";
        
        if (authMessageText != null)
            authMessageText.text = "";
    }
}