using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // GET api/results?examId=1&className=10th&section=A&subjectId=2
        [HttpGet]
        public IActionResult GetResultsForEntry([FromQuery] int examId, [FromQuery] string className, [FromQuery] string section, [FromQuery] int subjectId)
        {
            try 
            {
                List<ResultEntryModel> results = new List<ResultEntryModel>();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT 
                            s.Id as StudentId, s.Name, s.RollNo, 
                            r.MarksObtained, r.MaxMarks, r.Remarks
                        FROM Students s
                        LEFT JOIN Results r ON s.Id = r.StudentId 
                            AND r.ExamId = @ExamId 
                            AND r.SubjectId = @SubjectId
                        WHERE s.Class = @ClassName AND s.Section = @Section
                        ORDER BY s.RollNo ASC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@ExamId", examId);
                    cmd.Parameters.AddWithValue("@SubjectId", subjectId);
                    cmd.Parameters.AddWithValue("@ClassName", className ?? "");
                    cmd.Parameters.AddWithValue("@Section", section ?? "");

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ResultEntryModel
                            {
                                StudentId = Convert.ToInt32(reader["StudentId"]),
                                StudentName = reader["Name"].ToString(),
                                RollNo = reader["RollNo"].ToString(),
                                MarksObtained = reader["MarksObtained"] != DBNull.Value ? Convert.ToDouble(reader["MarksObtained"]) : 0,
                                MaxMarks = reader["MaxMarks"] != DBNull.Value ? Convert.ToDouble(reader["MaxMarks"]) : 100,
                                Remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : ""
                            });
                        }
                    }
                }
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Database Error: " + ex.Message);
            }
        }

        [HttpPost]
        public IActionResult SaveResults([FromBody] ResultBatchModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();
                try
                {
                    foreach (var res in model.Results)
                    {
                        // Delete existing if any
                        string del = "DELETE FROM Results WHERE StudentId = @Sid AND SubjectId = @SubId AND ExamId = @Eid";
                        SqlCommand delCmd = new SqlCommand(del, con, trans);
                        delCmd.Parameters.AddWithValue("@Sid", res.StudentId);
                        delCmd.Parameters.AddWithValue("@SubId", model.SubjectId);
                        delCmd.Parameters.AddWithValue("@Eid", model.ExamId);
                        delCmd.ExecuteNonQuery();

                        // Insert new
                        string ins = "INSERT INTO Results (StudentId, SubjectId, ExamId, MarksObtained, MaxMarks, Remarks) VALUES (@Sid, @SubId, @Eid, @Marks, @Max, @Rem)";
                        SqlCommand insCmd = new SqlCommand(ins, con, trans);
                        insCmd.Parameters.AddWithValue("@Sid", res.StudentId);
                        insCmd.Parameters.AddWithValue("@SubId", model.SubjectId);
                        insCmd.Parameters.AddWithValue("@Eid", model.ExamId);
                        insCmd.Parameters.AddWithValue("@Marks", res.MarksObtained);
                        insCmd.Parameters.AddWithValue("@Max", res.MaxMarks);
                        insCmd.Parameters.AddWithValue("@Rem", res.Remarks ?? "");
                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    return BadRequest("Error: " + ex.Message);
                }
            }
            return Ok(new { message = "Results saved successfully" });
        }

        [HttpGet("generate")]
        public IActionResult GetGeneratedResults([FromQuery] int examId, [FromQuery] string className, [FromQuery] string section, [FromQuery] int? subjectId = null)
        {
            try
            {
                List<GeneratedResultSummary> summaries = new List<GeneratedResultSummary>();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT 
                            s.Id as StudentId, s.Name, s.RollNo,
                            SUM(r.MarksObtained) as TotalObtained,
                            SUM(r.MaxMarks) as TotalMax,
                            (SELECT COUNT(*) FROM Attendance WHERE StudentId = s.Id AND Status = 'Present') as PresentDays,
                            (SELECT COUNT(*) FROM Attendance WHERE StudentId = s.Id) as TotalDays
                        FROM Students s
                        JOIN Results r ON s.Id = r.StudentId
                        WHERE s.Class = @ClassName AND s.Section = @Section AND r.ExamId = @ExamId";

                    if (subjectId.HasValue) query += " AND r.SubjectId = @SubjectId";
                    
                    query += " GROUP BY s.Id, s.Name, s.RollNo ORDER BY SUM(r.MarksObtained) DESC";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@ExamId", examId);
                    cmd.Parameters.AddWithValue("@ClassName", className);
                    cmd.Parameters.AddWithValue("@Section", section);
                    if (subjectId.HasValue) cmd.Parameters.AddWithValue("@SubjectId", subjectId.Value);

                    con.Open();
                    int rank = 1;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            double obtained = Convert.ToDouble(reader["TotalObtained"]);
                            double max = Convert.ToDouble(reader["TotalMax"]);
                            double per = max > 0 ? (obtained / max) * 100 : 0;
                            
                            int present = Convert.ToInt32(reader["PresentDays"]);
                            int totalDays = Convert.ToInt32(reader["TotalDays"]);
                            double attPer = totalDays > 0 ? (double)present / totalDays * 100 : 0;

                            summaries.Add(new GeneratedResultSummary
                            {
                                StudentId = Convert.ToInt32(reader["StudentId"]),
                                RollNo = reader["RollNo"].ToString(),
                                Name = reader["Name"].ToString(),
                                TotalMaxMarks = max,
                                TotalObtainedMarks = obtained,
                                Percentage = Math.Round(per, 2),
                                Grade = CalulateGrade(per),
                                Result = per >= 33 ? "Pass" : "Fail",
                                Rank = rank++,
                                AttendancePercentage = Math.Round(attPer, 2)
                            });
                        }
                    }
                }
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error generating result: " + ex.Message);
            }
        }

        private string CalulateGrade(double percentage)
        {
            if (percentage >= 90) return "A+";
            if (percentage >= 80) return "A";
            if (percentage >= 70) return "B";
            if (percentage >= 60) return "C";
            if (percentage >= 50) return "D";
            if (percentage >= 33) return "E";
            return "F";
        }

        [HttpGet("student-marksheet")]
        public IActionResult GetStudentMarksheet([FromQuery] int studentId, [FromQuery] int examId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    string query = @"
                        SELECT 
                            sub.SubjectName,
                            r.MarksObtained,
                            r.MaxMarks,
                            r.Remarks
                        FROM Subjects sub
                        LEFT JOIN Results r ON sub.Id = r.SubjectId AND r.StudentId = @Sid AND r.ExamId = @Eid
                        WHERE r.Id IS NOT NULL"; // Only show subjects where marks are entered

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Sid", studentId);
                    cmd.Parameters.AddWithValue("@Eid", examId);

                    con.Open();
                    List<object> marks = new List<object>();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            marks.Add(new
                            {
                                SubjectName = reader["SubjectName"].ToString(),
                                MarksObtained = Convert.ToDouble(reader["MarksObtained"]),
                                MaxMarks = Convert.ToDouble(reader["MaxMarks"]),
                                Remarks = reader["Remarks"].ToString()
                            });
                        }
                    }
                    return Ok(marks);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fetching marksheet: " + ex.Message);
            }
        }
    }

    // Models
    public class ResultEntryModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string RollNo { get; set; }
        public double MarksObtained { get; set; }
        public double MaxMarks { get; set; }
        public string Remarks { get; set; }
    }

    public class ResultBatchModel
    {
        public int ExamId { get; set; }
        public int SubjectId { get; set; }
        public List<ResultEntryModel> Results { get; set; }
    }

    public class GeneratedResultSummary
    {
        public int StudentId { get; set; }
        public string RollNo { get; set; }
        public string Name { get; set; }
        public double TotalMaxMarks { get; set; }
        public double TotalObtainedMarks { get; set; }
        public double Percentage { get; set; }
        public string Grade { get; set; }
        public string Result { get; set; }
        public int Rank { get; set; }
        public double AttendancePercentage { get; set; }
    }
}
