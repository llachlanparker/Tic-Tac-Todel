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

    [Header("References")]
    [SerializeField] private GameManager _gameManager;

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

                    // Get new user ID 
                    command.CommandText = "SELECT id FROM users WHERE username = '" + SanitizeInput(username) + "';";
                    object userIdObj = command.ExecuteScalar();
                    int userId = userIdObj != null && userIdObj != DBNull.Value 
                        ? Convert.ToInt32(userIdObj) 
                        : 0;

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
                        welcomeText.text = "Hello, " + username + "!";

                        // tell PlayerScores who logged in
                        PlayerScores.instance.SetCurrentUser((int)_currentUserId);

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

        // Reset PlayerScores display
        if (PlayerScores.instance != null)
            PlayerScores.instance.DisplayStats();

        // Reset game state via GameManager
        if (_gameManager != null)
            _gameManager.CloseAllPanels();

        loginPanel.SetActive(true);
    }

    public bool IsLoggedIn() => _currentUserId.HasValue;
    public int GetCurrentUserId() => _currentUserId ?? 0;

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

    // auth alert
    public void SetAuthMessage(string message, bool success)
    {
        if (authMessageText != null)
        {
            authMessageText.color = success ? Color.green : Color.red;
            authMessageText.text = message;
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
        _gameManager.CloseAllPanels();
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