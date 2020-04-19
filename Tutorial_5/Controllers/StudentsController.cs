using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial_5.Services;

namespace Tutorial_5.Controllers
{
    [Route("api/students")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentServiceDb _dbService;

        public StudentsController(IStudentServiceDb dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public IActionResult GetStudents()
        {
            return Ok(_dbService.GetStudents());
        }

        [HttpGet("secured/{indexNumber}")]
        public IActionResult GetStudent(string indexNumber)
        {
            var student = _dbService.GetStudent(indexNumber);
            if (student == null)
            {
                return NotFound($"No students with provided index number ({indexNumber})");
            }
            else
            {
                return Ok(student);
            }
        }

 
    }
}