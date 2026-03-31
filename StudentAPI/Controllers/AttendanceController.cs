using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System;
using System.Linq;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // --- STUDENT ATTENDANCE ---

        // GET: api/attendance?date=2024-02-06&className=10th&section=A
        [HttpGet]
        public IActionResult GetAttendance([FromQuery] string date, [FromQuery] string className, [FromQuery] string section)
        {
            List<AttendanceModel> attendanceList = new List<AttendanceModel>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        s.Id as StudentId, s.Name, s.RollNo, 
                        a.Status, a.Remarks
                    FROM Students s
                    LEFT JOIN Attendance a ON s.Id = a.StudentId AND a.AttendanceDate = @Date
                    WHERE s.Class = @ClassName AND s.Section = @Section
                    ORDER BY s.RollNo ASC";

                SqlCommand cmd = new SqlCommand(query, con);
                
                DateTime parsedDate;
                if (!DateTime.TryParse(date, out parsedDate))
                {
                    return BadRequest("Invalid Date Format received: " + date);
                }

                cmd.Parameters.Add("@Date", SqlDbType.Date).Value = parsedDate;
                cmd.Parameters.Add("@ClassName", SqlDbType.NVarChar).Value = className ?? "";
                cmd.Parameters.Add("@Section", SqlDbType.NVarChar).Value = section ?? "";

                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            attendanceList.Add(new AttendanceModel
                            {
                                StudentId = Convert.ToInt32(reader["StudentId"]),
                                StudentName = reader["Name"].ToString(),
                                RollNo = reader["RollNo"].ToString(),
                                Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : null,
                                Remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Database Error: " + ex.Message);
                }
            }
            return Ok(attendanceList);
        }

        // POST: api/attendance
        [HttpPost]
        public IActionResult SaveAttendance([FromBody] AttendanceBatchModel model)
        {
            return SaveGenericAttendance(model, "Attendance", "StudentId");
        }

        // --- TEACHER ATTENDANCE ---

        // GET: api/attendance/teachers?date=2024-02-06
        [HttpGet("teachers")]
        public IActionResult GetTeacherAttendance([FromQuery] string date)
        {
            List<AttendanceModel> list = new List<AttendanceModel>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Fetch all teachers
                string query = @"
                    SELECT 
                        t.Id as TeacherId, t.Name, 
                        ta.Status, ta.Remarks
                    FROM Teachers t
                    LEFT JOIN TeacherAttendance ta ON t.Id = ta.TeacherId AND ta.AttendanceDate = @Date
                    ORDER BY t.Name";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Date", DateTime.Parse(date));

                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AttendanceModel
                            {
                                StudentId = Convert.ToInt32(reader["TeacherId"]), // Reusing StudentId property for TeacherId to keep model simple
                                StudentName = reader["Name"].ToString(),
                                RollNo = "", // Not applicable
                                Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : null,
                                Remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : ""
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest("Error: " + ex.Message);
                }
            }
            return Ok(list);
        }

        // POST: api/attendance/teachers
        [HttpPost("teachers")]
        public IActionResult SaveTeacherAttendance([FromBody] AttendanceBatchModel model)
        {
             return SaveGenericAttendance(model, "TeacherAttendance", "TeacherId");
        }

        private IActionResult SaveGenericAttendance(AttendanceBatchModel model, string tableName, string idColumn)
        {
            if (model.Records == null || model.Records.Count == 0) return BadRequest("No records provided");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlTransaction transaction = con.BeginTransaction();
                try
                {
                    DateTime date = DateTime.Parse(model.Date);
                    foreach (var record in model.Records)
                    {
                        string deleteQuery = $"DELETE FROM {tableName} WHERE {idColumn} = @Id AND AttendanceDate = @Date";
                        SqlCommand delCmd = new SqlCommand(deleteQuery, con, transaction);
                        delCmd.Parameters.AddWithValue("@Id", record.StudentId); // StudentId holds TeacherId or StudentId
                        delCmd.Parameters.AddWithValue("@Date", date);
                        delCmd.ExecuteNonQuery();

                        string insertQuery = $"INSERT INTO {tableName} ({idColumn}, AttendanceDate, Status, Remarks) VALUES (@Id, @Date, @Status, @Remarks)";
                        SqlCommand insCmd = new SqlCommand(insertQuery, con, transaction);
                        insCmd.Parameters.AddWithValue("@Id", record.StudentId);
                        insCmd.Parameters.AddWithValue("@Date", date);
                        insCmd.Parameters.AddWithValue("@Status", record.Status);
                        insCmd.Parameters.AddWithValue("@Remarks", record.Remarks ?? (object)DBNull.Value);
                        insCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    return Ok(new { message = "Attendance Saved Successfully" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest("Error saving attendance: " + ex.Message);
                }
            }
        }

        // --- REPORTS ---

        // GET: api/attendance/report?type=student&month=2&year=2026&className=10th&section=A
        [HttpGet("report")]
        public IActionResult GetReport([FromQuery] string type, [FromQuery] int month, [FromQuery] int year, [FromQuery] string className, [FromQuery] string section)
        {
            List<AttendanceRecord> records = new List<AttendanceRecord>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "";
                if (type == "student")
                {
                    query = @"
                        SELECT a.StudentId as EntityId, s.Name, s.RollNo, a.AttendanceDate, a.Status
                        FROM Attendance a
                        JOIN Students s ON a.StudentId = s.Id
                        WHERE MONTH(a.AttendanceDate) = @Month 
                          AND YEAR(a.AttendanceDate) = @Year
                          AND TRIM(s.Class) = @ClassName AND TRIM(s.Section) = @Section";
                }
                else if (type == "teacher")
                {
                    query = @"
                        SELECT ta.TeacherId as EntityId, t.Name, '' as RollNo, ta.AttendanceDate, ta.Status
                        FROM TeacherAttendance ta
                        JOIN Teachers t ON ta.TeacherId = t.Id
                        WHERE MONTH(ta.AttendanceDate) = @Month 
                          AND YEAR(ta.AttendanceDate) = @Year";
                }
                else return BadRequest("Invalid type");

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Month", month);
                cmd.Parameters.AddWithValue("@Year", year);
                if(type == "student")
                {
                    cmd.Parameters.AddWithValue("@ClassName", className ?? "");
                    cmd.Parameters.AddWithValue("@Section", section ?? "");
                }

                try
                {
                    con.Open();
                    using(SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            records.Add(new AttendanceRecord {
                                Id = Convert.ToInt32(reader["EntityId"]),
                                Name = reader["Name"].ToString(),
                                RollNo = reader["RollNo"].ToString(),
                                Date = Convert.ToDateTime(reader["AttendanceDate"]).ToString("yyyy-MM-dd"),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
                catch(Exception ex)
                {
                    return BadRequest("Error fetching report: " + ex.Message);
                }
            }
            return Ok(records);
        }
    }

    public class AttendanceModel
    {
        public int StudentId { get; set; } // Used for both StudentId and TeacherId
        public string? StudentName { get; set; }
        public string? RollNo { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
    }

    public class AttendanceBatchModel
    {
        public string Date { get; set; }
        public List<AttendanceModel> Records { get; set; }
    }

    public class AttendanceRecord 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RollNo { get; set; }
        public string Date { get; set; }
        public string? Status { get; set; }
    }
}
