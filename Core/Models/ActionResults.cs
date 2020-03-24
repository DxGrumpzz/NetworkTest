namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;


    /// <summary>
    /// A result from an action
    /// </summary>
    public enum ActionStatus
    {
        /// <summary>
        /// The operation failed
        /// </summary>
        Failed = 0,

        /// <summary>
        /// The operation was successful
        /// </summary>
        Success = 1,
    };
};
