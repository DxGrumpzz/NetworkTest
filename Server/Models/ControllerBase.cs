namespace Server
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;


    /// <summary>
    /// A base class that all Controllers should inherit from
    /// </summary>
    public abstract class ControllerBase
    {
        /// <summary>
        /// Retrieves a list of actions inside this controller
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ControllerActionInfo> GetActions()
        {
            // Get the correct functions by...
            // Calling Get methods on methods that are: Public, are part of the instance, and that can be invoked
            var actions = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod)
            // Filter out functions that don't return a object of type ActionResult
            .Where(action => action.ReturnType == typeof(ActionResult) ||
                             action.ReturnType.BaseType == typeof(ActionResult))
            // Convert the MethodInfo to a ControllerActionInfo and pass it this controller
            .Select(action =>
            new ControllerActionInfo(action, this));

            return actions;
        }

        /// <summary>
        /// Finds a single action
        /// </summary>
        /// <param name="actionName"> The name of the action </param>
        /// <returns></returns>
        public ControllerActionInfo GetAction(string actionName)
        {
            // Call the GetActions function and find the first matching action,
            // If no action was found return the default value, which is null
            var action = GetActions().FirstOrDefault(action => action.ActionName == actionName);

            return action;
        }

    };
};
