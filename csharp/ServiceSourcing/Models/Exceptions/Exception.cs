using System;
namespace ServiceSourcing.Models.Exceptions
{
    [Serializable]
    public class BadRefreshTokenException : Exception
    {
        public BadRefreshTokenException ()
        {}

        public BadRefreshTokenException (string message) 
            : base(message)
        {}

        public BadRefreshTokenException (string message, Exception innerException)
            : base (message, innerException)
        {}    
    }
    [Serializable]
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException ()
        {}

        public UnauthorizedException (string message) 
            : base(message)
        {}

        public UnauthorizedException (string message, Exception innerException)
            : base (message, innerException)
        {}    
    }
    [Serializable]
    public class RetryableException : Exception
    {
        public RetryableException ()
        {}

        public RetryableException (string message) 
            : base(message)
        {}

        public RetryableException (string message, Exception innerException)
            : base (message, innerException)
        {}    
    }
}