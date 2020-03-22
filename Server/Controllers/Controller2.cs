namespace Server
{
    using Core;
    using System;


    /// <summary>
    /// 
    /// </summary>
    public class Controller2
    {

        public ActionResult Action()
        {
            Console.WriteLine($"Client called {nameof(Controller2)}/{nameof(Action2)}");

            return null;
        }

        public ActionResult Action2(TestClass s)
        {
            Console.WriteLine($"Client called {nameof(Controller2)}/{nameof(Action2)}");

            Console.WriteLine($"Recevied {s.Text}");

            return new ActionResult()
            {
                Data = "mega succ",
            };
        }

    };
};
