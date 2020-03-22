namespace Core
{
    using System.Collections.Generic;

    public class TestClass
    {
        public string Text { get; set; }
        public int Number { get; set; }
        public bool Bool { get; set; }

        public IEnumerable<int> Enumerable { get; set; }
    }
}
