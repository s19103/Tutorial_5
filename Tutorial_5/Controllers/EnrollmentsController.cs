using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial_5.DTOs.Request;
using Tutorial_5.Exceptions;
using Tutorial_5.Services;

namespace Tutorial_5.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IStudentServiceDb _dbService;

        public EnrollmentsController(IStudentServiceDb dbService)
        {
            _dbService = dbService;
        }



        [HttpPost]
        public IActionResult CreateStudent(StudentWithStudiesRequest request)
        {
            try
            {
                return Ok(_dbService.CreateStudentWithStudies(request));
            } 
            catch (DbServiceException e)
            {
                if (e.Type == DbServiceExceptionTypeEnum.NotFound)
                    return NotFound(e.Message);
                else if (e.Type == DbServiceExceptionTypeEnum.ValueNotUnique)
                    return BadRequest(e.Message);
                else
                    return StatusCode(500);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost("promotions")]
        public IActionResult PromoteStudents(PromotionRequest request)
        {
            if(!_dbService.CheckIfEnrollmentExists(request.Studies, request.Semester))
            {
                return NotFound("Enrollment not found!");
            }

            try
            {
                return Ok(_dbService.PromoteStudents(request.Studies, request.Semester));
            }
            catch(DbServiceException e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
            catch(Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
        }
    }
}