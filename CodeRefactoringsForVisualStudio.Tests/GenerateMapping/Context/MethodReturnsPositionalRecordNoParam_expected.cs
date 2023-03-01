using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A { get; set; }
        public string B { get; set; }


        public FooR Fuu()
        {
            return new FooR(this.A, this.B);
        }
    }

    record FooR(int A, string B);
}