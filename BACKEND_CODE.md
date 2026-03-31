# Backend Implementation (SQL & C#)

Here is the complete solution using your Connection String.

## 1. SQL Query (Run this in SQL Server Management Studio)

First, create the table where the data will be stored.

```sql
USE Student; -- Using the database you mentioned: Student
GO

CREATE TABLE Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    DateOfBirth DATE,
    Gender NVARCHAR(20),
    RollNo NVARCHAR(50),
    Class NVARCHAR(50),
    FatherName NVARCHAR(100),
    MotherName NVARCHAR(100),
    AdmissionDate DATE
);
GO
```

---

## 2. C# Controller Code (ADO.NET)

This code uses standard `Microsoft.Data.SqlClient` to run raw queries. You can Copy-Paste this into your API Controller.

**File:** `StudentsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        // Your Connection String (Database Name Updated to 'Student')
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // 1. GET ALL STUDENTS
        [HttpGet]
        public IActionResult GetStudents()
        {
            List<object> students = new List<object>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM Students ORDER BY Id DESC";
                SqlCommand cmd = new SqlCommand(query, con);
                
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new
                            {
                                Id = reader["Id"],
                                Name = reader["Name"],
                                DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]).ToString("yyyy-MM-dd") : "",
                                Gender = reader["Gender"],
                                Roll = reader["RollNo"],
                                Class = reader["Class"],
                                Father = reader["FatherName"],
                                Mother = reader["MotherName"],
                                AdmissionDate = reader["AdmissionDate"] != DBNull.Value ? Convert.ToDateTime(reader["AdmissionDate"]).ToString("yyyy-MM-dd") : ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }

            return Ok(students);
        }

        // 2. ADD STUDENT (CREATE)
        [HttpPost]
        public IActionResult AddStudent([FromBody] StudentModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Students (Name, DateOfBirth, Gender, RollNo, Class, FatherName, MotherName, AdmissionDate) 
                                 VALUES (@Name, @Dob, @Gender, @Roll, @Class, @Father, @Mother, @Admission)";
                
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", model.StudentName);
                cmd.Parameters.AddWithValue("@Dob", string.IsNullOrEmpty(model.DateOfBirth) ? DBNull.Value : DateTime.Parse(model.DateOfBirth));
                cmd.Parameters.AddWithValue("@Gender", model.Gender ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Roll", model.RollNo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Class", model.StudentClass ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Father", model.FatherName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Mother", model.MotherName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Admission", string.IsNullOrEmpty(model.AdmissionDate) ? DBNull.Value : DateTime.Parse(model.AdmissionDate));

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Student Added Successfully" });
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }

        // 3. UPDATE STUDENT
        [HttpPut]
        public IActionResult UpdateStudent([FromBody] StudentModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Note: We use Id from the model to identify which record to update
                string query = @"UPDATE Students SET 
                                Name = @Name, 
                                DateOfBirth = @Dob, 
                                Gender = @Gender, 
                                RollNo = @Roll, 
                                Class = @Class, 
                                FatherName = @Father, 
                                MotherName = @Mother, 
                                AdmissionDate = @Admission
                                WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", model.Id);
                cmd.Parameters.AddWithValue("@Name", model.StudentName);
                cmd.Parameters.AddWithValue("@Dob", string.IsNullOrEmpty(model.DateOfBirth) ? DBNull.Value : DateTime.Parse(model.DateOfBirth));
                cmd.Parameters.AddWithValue("@Gender", model.Gender ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Roll", model.RollNo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Class", model.StudentClass ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Father", model.FatherName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Mother", model.MotherName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Admission", string.IsNullOrEmpty(model.AdmissionDate) ? DBNull.Value : DateTime.Parse(model.AdmissionDate));

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

        // 4. DELETE STUDENT
        [HttpDelete("{id}")]
        public IActionResult DeleteStudent(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM Students WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                try
                {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Deleted Successfully" });
                    else return NotFound("Student not found");
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
        }
    }

    // Helper Class to map incoming JSON
    public class StudentModel
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public string DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string RollNo { get; set; }
        public string StudentClass { get; set; }
        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public string AdmissionDate { get; set; }
    }

    // IMPORTANT: Add this to Program.cs to enable CORS (Connection from Next.js)
    // builder.Services.AddCors(options =>
    // {
    //    options.AddPolicy("AllowReactApp",
    //        builder => builder.WithOrigins("http://localhost:3000")
    //                          .AllowAnyMethod()
    //                          .AllowAnyHeader());
    // });
    // app.UseCors("AllowReactApp");
}
```
