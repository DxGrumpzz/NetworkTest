namespace Server
{
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A class that contains info about a controller's action
    /// </summary>
    public class ControllerActionInfo
    {

        /// <summary>
        /// The action's associated controller
        /// </summary>
        private ControllerBase _controller;

        /// <summary>
        /// .NET MethodInfo class, contains info about a function
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// A boolean flag that indicates if this action has arguments
        /// </summary>
        public bool ActionHasParameters => MethodInfo.GetParameters().Count() > 0;

        /// <summary>
        /// The name of this action
        /// </summary>
        public string ActionName => MethodInfo.Name;

        /// <summary>
        /// Retrieves a "list" of parameters
        /// </summary>
        public ParameterInfo[] Parameters => MethodInfo.GetParameters();


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="controller"></param>
        public ControllerActionInfo(MethodInfo methodInfo, ControllerBase controller)
        {
            MethodInfo = methodInfo;
            _controller = controller;
        }


        /// <summary>
        /// Call this action without arguments
        /// </summary>
        /// <returns></returns>
        public ActionResult Invoke()
        {
            // Invoke the action using the associated constroller and convert the result to an ActionResult
            return (ActionResult)MethodInfo.Invoke(_controller, null);
        }

        /// <summary>
        /// Call this action with a single argument
        /// </summary>
        /// <typeparam name="T"> The type of the argument </typeparam>
        /// <param name="arg"> The argument to pass to the action </param>
        /// <returns></returns>
        public ActionResult Invoke<T>(T arg)
        {
            // Invoke the action using the associated constroller, 
            // pass it the arguemnt,
            // and convert the result to an ActionResult
            return (ActionResult)MethodInfo.Invoke(_controller, new[] { (object)arg });
        }

        public ActionResult Invoke(params object[] args)
        {
            return (ActionResult)MethodInfo.Invoke(_controller, args);

        }

    }
};

