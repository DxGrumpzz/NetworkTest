namespace Server
{
    using Core;


    /// <summary>
    /// 
    /// </summary>
    public class ActionResult
    {
        public object Data { get; set; }
        
        public ActionStatus Result { get; set; }

        public ActionResult() { }

        public ActionResult(ActionStatus result) 
        {
            Result = result;
        }

    };

};
