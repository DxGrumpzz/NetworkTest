namespace Server
{
    using Core;


    /// <summary>
    /// A result that is retured from an action
    /// </summary>
    public class ActionResult
    {
        /// <summary>
        /// An object that can be returned
        /// </summary>
        public object Data { get; set; }
        
        /// <summary>
        /// The result from the action
        /// </summary>
        public ActionStatus Result { get; set; }

        public ActionResult() { }

        public ActionResult(ActionStatus result) 
        {
            Result = result;
        }

    };

};
