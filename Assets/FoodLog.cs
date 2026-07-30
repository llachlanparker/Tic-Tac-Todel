using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine.UI;

public class Foodlog : MonoBehaviour
{
    public TMPro.TMP_InputField foodInput;
    public TMPro.TMP_InputField mealInput;
    public TMPro.TMP_Text foodList;

    private string dbName = "URI=file:FoodLog.db";

    void Start()
    {
        CreateDB();
        DisplayFood();
    }

    public void CreateDB()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS fooditems (foodname VARCHAR(30), meal VARCHAR(20));";
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
    }

    public void AddFood()
    {
         using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO fooditems (foodname, meal) VALUES ('" + foodInput.text + "', '" + mealInput.text + "');";
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
        DisplayFood();
    }

    public void DisplayFood()
    {
        foodList.text ="";

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM fooditems ORDER BY meal;";

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    foodList.text += reader["foodname"] + "\t\t" + reader["meal"] + "\n";

                    reader.Close();
                }
            }
            connection.Close();
        }
    }
}