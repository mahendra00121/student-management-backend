using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var stats = new DashboardStats();
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                // 1. Total Students
                string q1 = "SELECT COUNT(*) FROM Students";
                using (SqlCommand cmd = new SqlCommand(q1, con))
                {
                    stats.TotalStudents = (int)cmd.ExecuteScalar();
                }

                // 2. Total Teachers
                string q2 = "SELECT COUNT(*) FROM Teachers";
                using (SqlCommand cmd = new SqlCommand(q2, con))
                {
                    stats.TotalTeachers = (int)cmd.ExecuteScalar();
                }

                // 3. Today's Attendance Summary
                string q3 = "SELECT Status, COUNT(*) as Count FROM Attendance WHERE AttendanceDate = @Today GROUP BY Status";
                using (SqlCommand cmd = new SqlCommand(q3, con))
                {
                    cmd.Parameters.AddWithValue("@Today", today);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string status = reader["Status"].ToString();
                            int count = (int)reader["Count"];
                            if (status == "Present") stats.PresentToday = count;
                            else if (status == "Absent") stats.AbsentToday = count;
                            else if (status == "Leave") stats.OnLeaveToday = count;
                        }
                    }
                }

                // 4. Active Exams
                string q4 = "SELECT COUNT(*) FROM Exams WHERE IsActive = 1";
                using (SqlCommand cmd = new SqlCommand(q4, con))
                {
                    stats.ActiveExams = (int)cmd.ExecuteScalar();
                }

                // 5. Classes breakdown (For Chart)
                string q5 = "SELECT Class, COUNT(*) as Count FROM Students GROUP BY Class";
                using (SqlCommand cmd = new SqlCommand(q5, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.ClassWiseStudents.Add(new { 
                                ClassName = reader["Class"].ToString(), 
                                Count = (int)reader["Count"] 
                            });
                        }
                    }
                }

                // 6. Recent Admissions (Last 5 students)
                string q6 = "SELECT TOP 5 Name, RollNo, Class, Id FROM Students ORDER BY Id DESC";
                using (SqlCommand cmd = new SqlCommand(q6, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.RecentAdmissions.Add(new {
                                Name = reader["Name"].ToString(),
                                RollNo = reader["RollNo"].ToString(),
                                Class = reader["Class"].ToString(),
                                Id = reader["Id"]
                            });
                        }
                    }
                }

                // 7. Fees Summary
                string q7 = "SELECT ISNULL(SUM(Amount), 0) as Collected FROM FeePayments";
                using (SqlCommand cmd = new SqlCommand(q7, con))
                {
                    stats.TotalFeesCollected = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                string q8 = "SELECT ISNULL(SUM(AnnualFee), 0) as TotalExpected FROM Students";
                using (SqlCommand cmd = new SqlCommand(q8, con))
                {
                    stats.TotalFeesExpected = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // 8. Weekly Attendance Trend (Last 7 days)
                // 8. Weekly Attendance Trend (Last 7 days)
                string q9 = @"
                    SELECT TOP 7 AttendanceDate, 
                           SUM(CASE WHEN Status = 'Present' THEN 1 ELSE 0 END) as PresentCount
                    FROM Attendance 
                    GROUP BY AttendanceDate 
                    ORDER BY AttendanceDate DESC";
                using (SqlCommand cmd = new SqlCommand(q9, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.WeeklyAttendanceTrend.Add(new {
                                Date = ((DateTime)reader["AttendanceDate"]).ToString("ddd"),
                                Count = (int)reader["PresentCount"]
                            });
                        }
                    }
                    stats.WeeklyAttendanceTrend.Reverse(); // Chronological order
                }

                // 9. Total Classes Count
                string q10 = "SELECT COUNT(*) FROM Classes";
                using (SqlCommand cmd = new SqlCommand(q10, con))
                {
                    stats.TotalClasses = (int)cmd.ExecuteScalar();
                }
            }

            return Ok(stats);
        }
    }

    public class DashboardStats
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int ActiveExams { get; set; }
        public int TotalClasses { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int OnLeaveToday { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalFeesExpected { get; set; }
        public List<object> ClassWiseStudents { get; set; } = new List<object>();
        public List<object> RecentAdmissions { get; set; } = new List<object>();
        public List<object> WeeklyAttendanceTrend { get; set; } = new List<object>();
    }
}
