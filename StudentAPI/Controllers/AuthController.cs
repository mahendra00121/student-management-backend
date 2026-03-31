using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        [HttpPost("setup")]
        public IActionResult SetupTable()
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Create Users table if not exists
                string query = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                    BEGIN
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(100),
                            Email NVARCHAR(100) UNIQUE NOT NULL,
                            PasswordHash NVARCHAR(256) NOT NULL,
                            Role NVARCHAR(20) DEFAULT 'User'
                        )
                    END";

                SqlCommand cmd = new SqlCommand(query, con);
                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Users table checked/created successfully." });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error creating table: " + ex.Message);
                }
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Email and Password are required.");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Check if user exists
                string checkQuery = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@Email", model.Email);

                try
                {
                    con.Open();
                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0) return BadRequest("User already exists.");

                    // Hash password
                    string passwordHash = ComputeSha256Hash(model.Password);

                    // Insert
                    string insertQuery = "INSERT INTO Users (Name, Email, PasswordHash, Role) VALUES (@Name, @Email, @PasswordHash, 'Admin')";
                    SqlCommand cmd = new SqlCommand(insertQuery, con);
                    cmd.Parameters.AddWithValue("@Name", model.Name ?? "Admin");
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "User registered successfully." });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Email and Password are required.");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, Name, Email, Role, PasswordHash FROM Users WHERE Email = @Email";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", model.Email);

                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"].ToString();
                            if (ComputeSha256Hash(model.Password) == storedHash)
                            {
                                return Ok(new
                                {
                                    message = "Login successful",
                                    token = "mock-jwt-token-" + Guid.NewGuid().ToString(), // Should use real JWT in production
                                    user = new { 
                                        Id = reader["Id"],
                                        Name = reader["Name"],
                                        Email = reader["Email"],
                                        Role = reader["Role"]
                                    }
                                });
                            }
                        }
                    }
                    return Unauthorized(new { message = "Invalid email or password" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.NewPassword))
                return BadRequest("Email and New Password are required.");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Email = @Email";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PasswordHash", ComputeSha256Hash(model.NewPassword));
                cmd.Parameters.AddWithValue("@Email", model.Email);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Password reset successfully." });
                    else return NotFound(new { message = "User not found." });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }

    public class RegisterModel
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class LoginModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class ResetPasswordModel
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }
}
