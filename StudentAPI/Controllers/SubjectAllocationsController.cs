using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectAllocationsController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // GET: api/SubjectAllocations
        [HttpGet]
        public IActionResult GetAllocations()
        {
            List<object> allocations = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Join query to get readable names instead of just IDs
                string query = @"
                    SELECT 
                        sa.Id, 
                        c.Id as ClassId, c.ClassName, c.Section, 
                        s.Id as SubjectId, s.SubjectName, s.SubjectCode,
                        t.Id as TeacherId, t.Name as TeacherName
                    FROM SubjectAllocations sa
                    JOIN Classes c ON sa.ClassId = c.Id
                    JOIN Subjects s ON sa.SubjectId = s.Id
                    JOIN Teachers t ON sa.TeacherId = t.Id
                    ORDER BY c.ClassName, c.Section, s.SubjectName";

                SqlCommand cmd = new SqlCommand(query, con);
                
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allocations.Add(new
                            {
                                Id = reader["Id"],
                                ClassId = reader["ClassId"],
                                ClassName = reader["ClassName"],
                                Section = reader["Section"],
                                SubjectId = reader["SubjectId"],
                                SubjectName = reader["SubjectName"],
                                SubjectCode = reader["SubjectCode"],
                                TeacherId = reader["TeacherId"],
                                TeacherName = reader["TeacherName"]
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }

            return Ok(allocations);
        }

        // POST: api/SubjectAllocations
        [HttpPost]
        public IActionResult CreateAllocation([FromBody] AllocationModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Check if already exists to prevent duplicates
                string checkQuery = "SELECT COUNT(*) FROM SubjectAllocations WHERE ClassId = @ClassId AND SubjectId = @SubjectId";
                SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                checkCmd.Parameters.AddWithValue("@ClassId", model.ClassId);
                checkCmd.Parameters.AddWithValue("@SubjectId", model.SubjectId);

                string query = @"INSERT INTO SubjectAllocations (ClassId, SubjectId, TeacherId) 
                                 VALUES (@ClassId, @SubjectId, @TeacherId)";
                
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ClassId", model.ClassId);
                cmd.Parameters.AddWithValue("@SubjectId", model.SubjectId);
                cmd.Parameters.AddWithValue("@TeacherId", model.TeacherId);

                try
                {
                    con.Open();
                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0)
                    {
                        return BadRequest("This subject is already assigned to this class.");
                    }

                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Subject Assigned Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // DELETE: api/SubjectAllocations/5
        [HttpDelete("{id}")]
        public IActionResult DeleteAllocation(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM SubjectAllocations WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Allocation Removed Successfully" });
                    else return NotFound("Allocation record not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }
    }

    public class AllocationModel
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
    }
}
