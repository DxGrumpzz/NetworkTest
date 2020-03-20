namespace Server
{
    using Core;

    using System;

    public class Controller
    {

        public ActionResult Action()
        {
            Console.WriteLine($"Client called {nameof(Action)}");

            return null;
        }

        public ActionResult Action2(TestClass s)
        {
            Console.WriteLine($"Client called {nameof(Action2)}");

            return new ActionResult()
            {
                Result = "mega succ",
            };
        }

    }
};