using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace StudentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeesController : ControllerBase
    {
        private readonly string _connectionString = "Data Source=DESKTOP-7MLSVJN\\SQLEXPRESS;Initial Catalog=Student;Persist Security Info=True;User ID=sa;Password=Mahendra@121;TrustServerCertificate=True";

        // GET: api/Fees
        [HttpGet]
        public IActionResult GetFeeRecords()
        {
            List<object> fees = new List<object>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Join with Students table to get Name & Roll No
                string query = @"
                    SELECT f.Id, f.StudentId, s.Name, s.RollNo, s.Class, f.Amount, f.PaymentDate, f.PaymentMode, f.Remarks 
                    FROM FeePayments f
                    JOIN Students s ON f.StudentId = s.Id
                    ORDER BY f.PaymentDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                try {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            fees.Add(new {
                                Id = reader["Id"],
                                StudentId = reader["StudentId"],
                                StudentName = reader["Name"],
                                RollNo = reader["RollNo"],
                                Class = reader["Class"],
                                Amount = reader["Amount"],
                                PaymentDate = reader["PaymentDate"],
                                PaymentMode = reader["PaymentMode"],
                                Remarks = reader["Remarks"]
                            });
                        }
                    }
                } catch (Exception ex) { return BadRequest("Error: " + ex.Message); }
            }
            return Ok(fees);
        }

        // GET: api/Fees/status - Returns summary of fees for all students (Total vs Paid)
        [HttpGet("status")]
        public IActionResult GetFeeStatus()
        {
            List<object> statusList = new List<object>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Calculate Total Paid per student using Group By
                string query = @"
                    SELECT 
                        s.Id, s.Name, s.RollNo, s.Class, s.AnnualFee,
                        ISNULL(SUM(fp.Amount), 0) as TotalPaid
                    FROM Students s
                    LEFT JOIN FeePayments fp ON s.Id = fp.StudentId
                    GROUP BY s.Id, s.Name, s.RollNo, s.Class, s.AnnualFee";

                SqlCommand cmd = new SqlCommand(query, con);
                try {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            decimal annualFee = reader["AnnualFee"] != DBNull.Value ? Convert.ToDecimal(reader["AnnualFee"]) : 0;
                            decimal totalPaid = Convert.ToDecimal(reader["TotalPaid"]);
                            
                            statusList.Add(new {
                                Id = reader["Id"],
                                Name = reader["Name"],
                                RollNo = reader["RollNo"],
                                Class = reader["Class"],
                                TotalFee = annualFee,
                                Paid = totalPaid,
                                Pending = annualFee - totalPaid
                            });
                        }
                    }
                } catch (Exception ex) { return BadRequest("Error: " + ex.Message); }
            }
            return Ok(statusList);
        }

        // POST: api/Fees
        [HttpPost]
        public IActionResult CollectFee([FromBody] FeePaymentModel model)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO FeePayments (StudentId, Amount, PaymentDate, PaymentMode, Remarks) 
                                 VALUES (@StudentId, @Amount, @Date, @Mode, @Remarks)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@StudentId", model.StudentId);
                cmd.Parameters.AddWithValue("@Amount", model.Amount);
                cmd.Parameters.AddWithValue("@Date", model.PaymentDate == default ? DateTime.Now : model.PaymentDate);
                cmd.Parameters.AddWithValue("@Mode", model.PaymentMode ?? "Cash");
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? (object)DBNull.Value);

                try {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Fee Collected Successfully" });
                } catch (Exception ex) { return BadRequest("Error: " + ex.Message); }
            }
        }

        // DELETE: api/Fees/5
        [HttpDelete("{id}")]
        public IActionResult DeleteFeeRecord(int id)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM FeePayments WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);

                try {
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0) return Ok(new { message = "Record Deleted Successfully" });
                    else return NotFound("Record not found");
                } catch (Exception ex) { return BadRequest("Error: " + ex.Message); }
            }
        }
    }

    public class FeePaymentModel
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMode { get; set; }
        public string Remarks { get; set; }
    }
}
