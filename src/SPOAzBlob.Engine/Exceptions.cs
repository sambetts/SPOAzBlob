using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Base for all logical exceptions
    /// </summary>
    public abstract class SPOAzBlobException: Exception
    {
    }

    public abstract class UpdateConflictException : SPOAzBlobException
    {
        public UpdateConflictException(string otherUser) :base()
        { 
            OtherUser = otherUser;
        }

        public string OtherUser { get; set; } = string.Empty;
    }

    public class FileLockedByAnotherUserException : UpdateConflictException
    {
        public FileLockedByAnotherUserException(string otherUser) : base(otherUser)
        {
        }
    }
    public class FileUpdateConflictException : UpdateConflictException
    {
        public FileUpdateConflictException(string otherUser) : base(otherUser)
        {
        }
    }
}
