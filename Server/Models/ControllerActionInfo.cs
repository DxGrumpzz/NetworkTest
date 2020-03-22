namespace Server
{
    using System.Linq;
    using System.Reflection;

    public class ControllerActionInfo
    {
        private ControllerBase _controller;
        
        public MethodInfo MethodInfo { get; }

        public bool ActionHasParameters => MethodInfo.GetParameters().Count() > 0;

        public string ActionName => MethodInfo.Name;

        public ParameterInfo[] GetParameters => MethodInfo.GetParameters();


        public ControllerActionInfo(MethodInfo methodInfo, ControllerBase controller)
        {
            MethodInfo = methodInfo;
            _controller = controller;
        }



        public ActionResult Invoke()
        {
            return (ActionResult)MethodInfo.Invoke(_controller, null);
        }

        public ActionResult Invoke<T>(T arg)
        {
            return (ActionResult)MethodInfo.Invoke(_controller, new[] { (object)arg });
        }

        public ActionResult Invoke(params object[] args)
        {
            return (ActionResult)MethodInfo.Invoke(_controller, args);

        }

    }
};

