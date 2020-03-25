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

            string data = Guid.NewGuid().ToString("N");

            Console.WriteLine($"Will send {data}");
             
            return new ActionResult()
            {
                Data = data,
                Result = ActionStatus.Success,
            };
        }

    }
};