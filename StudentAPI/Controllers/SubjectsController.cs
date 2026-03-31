using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectsController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // 1. GET ALL SUBJECTS
        [HttpGet]
        public IActionResult GetSubjects()
        {
            List<object> subjects = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Subjects ORDER BY Id DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            subjects.Add(new
                            {
                                Id = reader["Id"],
                                SubjectName = reader["SubjectName"],
                                SubjectCode = reader["SubjectCode"],
                                Description = reader["Description"],
                                Credits = reader["Credits"] != DBNull.Value ? reader["Credits"] : 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }

            return Ok(subjects);
        }

        // 2. ADD SUBJECT
        [HttpPost]
        public IActionResult AddSubject([FromBody] SubjectModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Subjects (SubjectName, SubjectCode, Description, Credits) 
                                 VALUES (@SubjectName, @SubjectCode, @Description, @Credits)";
                
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@SubjectName", model.SubjectName);
                cmd.Parameters.AddWithValue("@SubjectCode", model.SubjectCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Credits", model.Credits);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Subject Added Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 3. UPDATE SUBJECT
        [HttpPut]
        public IActionResult UpdateSubject([FromBody] SubjectModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Subjects SET 
                                SubjectName = @SubjectName, 
                                SubjectCode = @SubjectCode, 
                                Description = @Description, 
                                Credits = @Credits
                                WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@SubjectName", model.SubjectName);
                cmd.Parameters.AddWithValue("@SubjectCode", model.SubjectCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Credits", model.Credits);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if(rows > 0) return Ok(new { message = "Updated Successfully" });
                    else return NotFound("Subject not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 4. DELETE SUBJECT
        [HttpDelete("{id}")]
        public IActionResult DeleteSubject(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Subjects WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Deleted Successfully" });
                    else return NotFound("Subject not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 5. DELETE ALL
        [HttpDelete("all")]
        public IActionResult DeleteAllSubjects()
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Subjects";
                SqlCommand cmd = new SqlCommand(query, con);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "All Subjects Deleted Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }
    }

    public class SubjectModel
    {
        public int Id { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public string Description { get; set; }
        public int Credits { get; set; }
    }
}
