namespace Server
{
    using Core;

    using System;

    public class Controller : ControllerBase
    {

        public ActionResult Action()
        {
            Console.WriteLine($"Client called {nameof(Controller)}/{nameof(Action)}");

            return new ActionResult(ActionStatus.Success);
        }

        public ActionResult Action2(TestClass s)
        {
            Console.WriteLine($"Client called {nameof(Controller)}/{nameof(Action2)}");

            Console.WriteLine($"Recevied {s.Text}");

            return new ActionResult()
            {
                Data = "mega succ",
                Result = ActionStatus.Success,
            };
        }

    }
};