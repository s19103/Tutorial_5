using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tutorial_5.Exceptions
{
    public class DbServiceException : Exception
    {
        public DbServiceExceptionTypeEnum Type { get; set; }
        public DbServiceException(DbServiceExceptionTypeEnum type, string msg) : base(message: msg)
        {
            this.Type = type;
        }
    }

    public enum DbServiceExceptionTypeEnum
    {
        ValueNotUnique = 0,
        NotFound = 1,
        ProcedureError = 2
    }
}
