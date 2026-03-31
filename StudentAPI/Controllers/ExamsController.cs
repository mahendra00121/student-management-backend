using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamsController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        [HttpGet]
        public IActionResult GetExams()
        {
            List<ExamModel> exams = new List<ExamModel>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Exams ORDER BY StartDate DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        exams.Add(new ExamModel
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ExamName = reader["ExamName"].ToString(),
                            Session = reader["Session"].ToString(),
                            StartDate = reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd") : "",
                            EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd") : "",
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return Ok(exams);
        }

        [HttpPost]
        public IActionResult AddExam([FromBody] ExamModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO Exams (ExamName, Session, StartDate, EndDate, IsActive) VALUES (@Name, @Session, @Start, @End, @Active)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", model.ExamName);
                cmd.Parameters.AddWithValue("@Session", model.Session);
                cmd.Parameters.AddWithValue("@Start", string.IsNullOrEmpty(model.StartDate) ? (object)DBNull.Value : DateTime.Parse(model.StartDate));
                cmd.Parameters.AddWithValue("@End", string.IsNullOrEmpty(model.EndDate) ? (object)DBNull.Value : DateTime.Parse(model.EndDate));
                cmd.Parameters.AddWithValue("@Active", model.IsActive);

                con.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { message = "Exam added successfully" });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteExam(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Exams WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { message = "Exam deleted successfully" });
        }
    }

    public class ExamModel
    {
        public int Id { get; set; }
        public string ExamName { get; set; }
        public string Session { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
