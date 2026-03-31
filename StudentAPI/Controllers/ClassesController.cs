using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClassesController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // 1. GET ALL CLASSES
        [HttpGet]
        public IActionResult GetClasses()
        {
            List<object> classes = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Classes ORDER BY Id DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            classes.Add(new
                            {
                                Id = reader["Id"],
                                ClassName = reader["ClassName"],
                                Section = reader["Section"],
                                ClassTeacher = reader["ClassTeacher"],
                                RoomNumber = reader["RoomNumber"],
                                Capacity = reader["Capacity"]
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }

            return Ok(classes);
        }

        // 2. ADD CLASS
        [HttpPost]
        public IActionResult AddClass([FromBody] ClassModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Classes (ClassName, Section, ClassTeacher, RoomNumber, Capacity) 
                                 VALUES (@ClassName, @Section, @Teacher, @Room, @Capacity)";
                
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ClassName", model.ClassName);
                cmd.Parameters.AddWithValue("@Section", model.Section ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Teacher", model.ClassTeacher ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Room", model.RoomNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Capacity", model.Capacity);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Class Added Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 3. UPDATE CLASS
        [HttpPut]
        public IActionResult UpdateClass([FromBody] ClassModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Classes SET 
                                ClassName = @ClassName, 
                                Section = @Section, 
                                ClassTeacher = @Teacher, 
                                RoomNumber = @Room, 
                                Capacity = @Capacity
                                WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@ClassName", model.ClassName);
                cmd.Parameters.AddWithValue("@Section", model.Section ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Teacher", model.ClassTeacher ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Room", model.RoomNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Capacity", model.Capacity);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if(rows > 0) return Ok(new { message = "Updated Successfully" });
                    else return NotFound("Class not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 4. DELETE CLASS
        [HttpDelete("{id}")]
        public IActionResult DeleteClass(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Classes WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Deleted Successfully" });
                    else return NotFound("Class not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 5. DELETE ALL
        [HttpDelete("all")]
        public IActionResult DeleteAllClasses()
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Classes";
                SqlCommand cmd = new SqlCommand(query, con);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "All Classes Deleted Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }
    }

    public class ClassModel
    {
        public int Id { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public string ClassTeacher { get; set; }
        public string RoomNumber { get; set; }
        public int Capacity { get; set; }
    }
}
