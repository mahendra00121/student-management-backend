using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachersController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // 1. GET ALL TEACHERS
        [HttpGet]
        public IActionResult GetTeachers()
        {
            List<object> teachers = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Ensure table exists or user creates it: Id, Name, Subject, Email, Phone, Qualification, Experience, JoinDate
                string query = "SELECT * FROM Teachers ORDER BY Id DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            teachers.Add(new
                            {
                                Id = reader["Id"],
                                Name = reader["Name"],
                                Subject = reader["Subject"],
                                Email = reader["Email"],
                                Phone = reader["Phone"],
                                Qualification = reader["Qualification"],
                                Experience = reader["Experience"],
                                JoinDate = reader["JoinDate"] != DBNull.Value ? Convert.ToDateTime(reader["JoinDate"]).ToString("yyyy-MM-dd") : ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }

            return Ok(teachers);
        }

        // 2. ADD TEACHER
        [HttpPost]
        public IActionResult AddTeacher([FromBody] TeacherModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Teachers (Name, Subject, Email, Phone, Qualification, Experience, JoinDate) 
                                 VALUES (@Name, @Subject, @Email, @Phone, @Qualification, @Experience, @JoinDate)";
                
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Subject", model.Subject ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", model.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Qualification", model.Qualification ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Experience", model.Experience ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@JoinDate", string.IsNullOrEmpty(model.JoinDate) ? DBNull.Value : DateTime.Parse(model.JoinDate));

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Teacher Added Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 3. UPDATE TEACHER
        [HttpPut]
        public IActionResult UpdateTeacher([FromBody] TeacherModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Teachers SET 
                                Name = @Name, 
                                Subject = @Subject, 
                                Email = @Email, 
                                Phone = @Phone, 
                                Qualification = @Qualification,
                                Experience = @Experience,
                                JoinDate = @JoinDate
                                WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Subject", model.Subject ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", model.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Qualification", model.Qualification ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Experience", model.Experience ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@JoinDate", string.IsNullOrEmpty(model.JoinDate) ? DBNull.Value : DateTime.Parse(model.JoinDate));

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if(rows > 0) return Ok(new { message = "Updated Successfully" });
                    else return NotFound("Teacher not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 4. DELETE TEACHER
        [HttpDelete("{id}")]
        public IActionResult DeleteTeacher(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Teachers WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Deleted Successfully" });
                    else return NotFound("Teacher not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 5. DELETE ALL
        [HttpDelete("all")]
        public IActionResult DeleteAllTeachers()
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Teachers";
                SqlCommand cmd = new SqlCommand(query, con);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "All Teachers Deleted Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }
    }

    public class TeacherModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Qualification { get; set; }
        public string Experience { get; set; }
        public string JoinDate { get; set; }
    }
}
