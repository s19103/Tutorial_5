using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tutorial_5.DTOs.Request;
using Tutorial_5.Models;

namespace Tutorial_5.Services
{
    public interface IStudentServiceDb
    {
        void CreateStudent(string indexNumber, string firstName, string lastName, DateTime birthDate,
                           int idEnrollment, SqlConnection sqlConnection = null, SqlTransaction transaction = null);
        Enrollment CreateStudentWithStudies(StudentWithStudiesRequest request);
        Student GetStudent(string indexNumber);
        IEnumerable<Student> GetStudents();
        bool CheckIfEnrollmentExists(string studies, int semester);
        Enrollment PromoteStudents(string studies, int semester);

    }
}
