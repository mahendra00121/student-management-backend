using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        [HttpGet]

        public IActionResult GetStudents()
        {
            List<StudentModel> students = new List<StudentModel>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Students";
                SqlCommand cmd = new SqlCommand(query, con);
                
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new StudentModel
                            {
                                Id = SafeInt(reader["Id"]),
                                Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : "",
                                FatherName = reader["FatherName"] != DBNull.Value ? reader["FatherName"].ToString() : "",
                                RollNo = reader["RollNo"] != DBNull.Value ? reader["RollNo"].ToString() : "",
                                Age = SafeInt(reader["Age"]),
                                Class = reader["Class"] != DBNull.Value ? reader["Class"].ToString() : "",
                                Section = reader["Section"] != DBNull.Value ? reader["Section"].ToString() : "",
                                Contact = reader["Contact"] != DBNull.Value ? reader["Contact"].ToString() : "",
                                AdmissionDate = reader["AdmissionDate"] != DBNull.Value ? Convert.ToDateTime(reader["AdmissionDate"]).ToString("yyyy-MM-dd") : "",
                                AnnualFee = reader["AnnualFee"] != DBNull.Value ? Convert.ToDecimal(reader["AnnualFee"]) : 0
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERROR GET] " + ex.Message);
                    return BadRequest("Error: " + ex.Message);
                }
            }

            return Ok(students);
        }

        private int SafeInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            if (int.TryParse(value.ToString(), out int result)) return result;
            return 0;
        }

        [HttpPost]
        public IActionResult AddStudent([FromBody] StudentModel model)
        {
            Console.WriteLine($"[DEBUG] Received Student: Name={model.Name}");
            
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Note: Using GETDATE() for AdmissionDate if not provided, or passed value
                string query = @"INSERT INTO Students (Name, FatherName, RollNo, Age, Class, Section, Contact, AnnualFee, AdmissionDate) 
                                 VALUES (@Name, @FatherName, @RollNo, @Age, @Class, @Section, @Contact, @AnnualFee, @AdmissionDate)";
                
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                cmd.Parameters.AddWithValue("@FatherName", model.FatherName ?? "");
                cmd.Parameters.AddWithValue("@RollNo", model.RollNo ?? "");
                cmd.Parameters.AddWithValue("@Age", model.Age);
                cmd.Parameters.AddWithValue("@Class", model.Class ?? "");
                cmd.Parameters.AddWithValue("@Section", model.Section ?? "");
                cmd.Parameters.AddWithValue("@Contact", model.Contact ?? "");
                cmd.Parameters.AddWithValue("@AnnualFee", model.AnnualFee);
                cmd.Parameters.AddWithValue("@AdmissionDate", string.IsNullOrEmpty(model.AdmissionDate) ? DateTime.Now : DateTime.Parse(model.AdmissionDate));

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Student Added Successfully" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERROR INSERT] " + ex.Message);
                    return BadRequest("Database Error: " + ex.Message);
                }
            }
        }

        // PUT and DELETE omitted for brevity but should be updated similarly if used

        // ...


        [HttpPut]
        public IActionResult UpdateStudent([FromBody] StudentModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE Students SET 
                                Name = @Name, 
                                FatherName = @FatherName, 
                                RollNo = @RollNo, 
                                Age = @Age, 
                                Class = @Class, 
                                Section = @Section, 
                                Contact = @Contact,
                                AnnualFee = @AnnualFee
                                WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@FatherName", model.FatherName);
                cmd.Parameters.AddWithValue("@RollNo", model.RollNo);
                cmd.Parameters.AddWithValue("@Age", model.Age);
                cmd.Parameters.AddWithValue("@Class", model.Class);
                cmd.Parameters.AddWithValue("@Section", model.Section);
                cmd.Parameters.AddWithValue("@Contact", model.Contact);
                cmd.Parameters.AddWithValue("@AnnualFee", model.AnnualFee);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if(rows > 0) return Ok(new { message = "Updated Successfully" });
                    else return NotFound("Student not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteStudent(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();
                try
                {
                    // 1. Delete Results
                    string delRes = "DELETE FROM Results WHERE StudentId = @Id";
                    SqlCommand cmdRes = new SqlCommand(delRes, con, trans);
                    cmdRes.Parameters.AddWithValue("@Id", id);
                    cmdRes.ExecuteNonQuery();

                    // 2. Delete Attendance
                    string delAtt = "DELETE FROM Attendance WHERE StudentId = @Id";
                    SqlCommand cmdAtt = new SqlCommand(delAtt, con, trans);
                    cmdAtt.Parameters.AddWithValue("@Id", id);
                    cmdAtt.ExecuteNonQuery();

                    // 3. Delete Fees
                    string delFees = "DELETE FROM FeePayments WHERE StudentId = @Id";
                    SqlCommand cmdFees = new SqlCommand(delFees, con, trans);
                    cmdFees.Parameters.AddWithValue("@Id", id);
                    cmdFees.ExecuteNonQuery();

                    // 4. Delete Student
                    string query = "DELETE FROM Students WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(query, con, trans);
                    cmd.Parameters.AddWithValue("@Id", id);
                    
                    int rows = cmd.ExecuteNonQuery();
                    
                    if (rows > 0) 
                    {
                        trans.Commit();
                        return Ok(new { message = "Deleted Successfully" });
                    }
                    else 
                    {
                        trans.Rollback();
                        return NotFound("Student not found");
                    }
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        [HttpDelete("all")]
        public IActionResult DeleteAll()
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Students";
                SqlCommand cmd = new SqlCommand(query, con);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "All Records Deleted" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }
    }

    public class StudentModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? FatherName { get; set; }
        public string? RollNo { get; set; }
        public int Age { get; set; }
        public string? Class { get; set; }
        public string? Section { get; set; }
        public string? Contact { get; set; }
        public decimal AnnualFee { get; set; }
        public string? AdmissionDate { get; set; } 
    }
}
