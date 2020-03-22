namespace Server
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;


    /// <summary>
    /// 
    /// </summary>
    public class ControllerBase
    {
        public IEnumerable<ControllerActionInfo> GetActions
        {
            get
            {
                var actions = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod)
                .Where(action => action.ReturnType == typeof(ActionResult) ||
                                 action.ReturnType.BaseType == typeof(ActionResult))
                .Select(action =>
                new ControllerActionInfo(action, this));

                return actions;
            }
        }

        public ControllerBase()
        {

        }

        public ControllerActionInfo GetAction(string actionName)
        {
            var action = GetActions.FirstOrDefault(action => action.ActionName == actionName);

            return action;
        }

    };
};
