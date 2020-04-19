using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tutorial_5.DTOs.Request;
using Tutorial_5.Exceptions;
using Tutorial_5.Models;

namespace Tutorial_5.Services
{
    public class SqlServerStudentDbService : IStudentServiceDb
    {
        private const string ConnStr = "Data Source=db-mssql;Initial Catalog=s19103;Integrated Security=True";


        public bool CheckIfEnrollmentExists(string studies, int semester)
        {
            using var con = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand
            {
                Connection = con,
                CommandText = @"SELECT e.idEnrollment 
                                FROM Enrollment e JOIN Studies s ON e.IdStudy = s.IdStudy 
                                WHERE s.Name = @Name AND e.Semester = @Semester;"
            };
            cmd.Parameters.AddWithValue("Name", studies);
            cmd.Parameters.AddWithValue("Semester", semester);

            con.Open();
            using var dr = cmd.ExecuteReader();

            return dr.Read();
        }

        public void CreateStudent(string indexNumber, string firstName, string lastName, DateTime birthDate, int idEnrollment, SqlConnection sqlConnection = null, SqlTransaction transaction = null)
        {
            using var cmd = new SqlCommand()
            {
                CommandText = @"INSERT INTO Student(IndexNumber, FirstName, LastName, BirthDate, IdEnrollment)
                                VALUES (@IndexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment);"
            };
            cmd.Parameters.AddWithValue("IndexNumber", indexNumber);
            cmd.Parameters.AddWithValue("FirstName", firstName);
            cmd.Parameters.AddWithValue("LastName", lastName);
            cmd.Parameters.AddWithValue("BirthDate", birthDate);
            cmd.Parameters.AddWithValue("IdEnrollment", idEnrollment);

            if (sqlConnection == null)
            {
                using var con = new SqlConnection(ConnStr);
                con.Open();
                cmd.Connection = con;
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.Connection = sqlConnection;
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
        }

        public Enrollment CreateStudentWithStudies(StudentWithStudiesRequest request)
        {
            using var con = new SqlConnection(ConnStr);
            con.Open();
            using var transaction = con.BeginTransaction();

            if (!CheckIfStudiesExists(request.Studies, con, transaction))
            {
                transaction.Rollback();
                throw new DbServiceException(DbServiceExceptionTypeEnum.NotFound, "Studies does not exists!");
            }

            var enrollment = GetNewestEnrollment(request.Studies, 1, con, transaction);
            if (enrollment == null)
            {
                CreateEnrollment(request.Studies, 1, DateTime.Now, con, transaction);
                enrollment = GetNewestEnrollment(request.Studies, 1, con, transaction);
            }

            if (GetStudent(request.IndexNumber) != null)
            {
                transaction.Rollback();
                throw new DbServiceException(DbServiceExceptionTypeEnum.ValueNotUnique, $"Index number ({request.IndexNumber}) is not unique!");
            }

            CreateStudent(request.IndexNumber, request.FirstName, request.LastName, request.BirthDate, enrollment.IdEnrollment, con, transaction);
            transaction.Commit();

            return enrollment;
        }

        public Student GetStudent(string indexNumber)
        {
            using var con = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand()
            {
                Connection = con,
                CommandText = @"SELECT * FROM Student WHERE IndexNumber = @indexNumber;"
            };
            cmd.Parameters.AddWithValue("indexNumber", indexNumber);

            con.Open();

            using var dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                return new Student
                {
                    IndexNumber = dr["IndexNumber"].ToString(),
                    FirstName = dr["FirstName"].ToString(),
                    LastName = dr["LastName"].ToString(),
                    BirthDate = DateTime.Parse(dr["BirthDate"].ToString()),
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString())
                };
            }
            else
            {
                return null;
            }

        }

        public IEnumerable<Student> GetStudents()
        {
            var student_list = new List<Student>();

            using (var con = new SqlConnection(ConnStr))
            {
                using var cmd = new SqlCommand
                {
                    Connection = con,
                    CommandText = @"SELECT * FROM Student;"
                };

                con.Open();
                using var dr = cmd.ExecuteReader();

                while(dr.Read())
                {
                    var student = new Student
                    {
                        IndexNumber = dr["IndexNumber"].ToString(),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        BirthDate = DateTime.Parse(dr["BirthDate"].ToString()),
                        IdEnrollment = int.Parse(dr["IdEnrollment"].ToString())
                    };
                    student_list.Add(student);
                }
            }
            return student_list;
        }

        public Enrollment PromoteStudents(string studies, int semester)
        {
            using var con = new SqlConnection(ConnStr);
            using var cmd = new SqlCommand()
            {
                Connection = con,
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandText = @"promoteStudents"
            };
            cmd.Parameters.AddWithValue("@Studies", studies);
            cmd.Parameters.AddWithValue("@Semester", semester);

            con.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new Enrollment
                {
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                    Semester = semester,
                    IdStudy = int.Parse(dr["IdStudy"].ToString()),
                    StartDate = DateTime.Parse(dr["StartDate"].ToString())
                };
            }
            else
            {
                throw new DbServiceException(DbServiceExceptionTypeEnum.ProcedureError, "something went wrong");
            }


        }



        // Helper methods
        private bool CheckIfStudiesExists(string name, SqlConnection sqlConnection, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand()
            {
                Connection = sqlConnection,
                Transaction = transaction,
                CommandText = @"SELECT 1 FROM Studies WHERE Name = @name;"
            };
            cmd.Parameters.AddWithValue("name", name);
            using var dr = cmd.ExecuteReader();

            return dr.Read();
        }

        private Enrollment GetNewestEnrollment(string studies, int semester, SqlConnection sqlConnection, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand()
            {
                Connection = sqlConnection,
                Transaction = transaction,
                CommandText = @"SELECT TOP 1 e.IdEnrollment, e.IdStudy, e.StartDate 
                                FROM Enrollment e JOIN Studies s ON e.IdStudy = s.IdStudy 
                                WHERE e.Semester = @Semester AND s.Name = @Name 
                                ORDER BY IdEnrollment DESC;"
            };
            cmd.Parameters.AddWithValue("Semester", semester);
            cmd.Parameters.AddWithValue("Name", studies);

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new Enrollment
                {
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                    Semester = semester,
                    IdStudy = int.Parse(dr["IdStudy"].ToString()),
                    StartDate = DateTime.Parse(dr["StartDate"].ToString())
                };
            }
            return null;
        }

        private void CreateEnrollment(string studies, int semester, DateTime startDate, SqlConnection sqlConnection, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand()
            {
                Connection = sqlConnection,
                Transaction = transaction,
                CommandText = @"INSERT INTO Enrollment(IdEnrollment, IdStudy, StartDate, Semester) 
                                VALUES ((SELECT ISNULL(MAX(e.IdEnrollment)+1,1) FROM Enrollment e), 
                                        (SELECT s.IdStudy FROM Studies s WHERE s.Name = @Name), 
                                        @StartDate. @Semester);"
            };
            cmd.Parameters.AddWithValue("Name", studies);
            cmd.Parameters.AddWithValue("Semester", semester);
            cmd.Parameters.AddWithValue("StartDate", startDate);

            cmd.ExecuteNonQuery();
        }

    }
}
