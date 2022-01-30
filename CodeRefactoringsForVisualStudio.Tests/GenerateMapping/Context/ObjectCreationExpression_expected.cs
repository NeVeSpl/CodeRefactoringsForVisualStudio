using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Mapper
    {
        public void Fuu(Foo source)
        {
            var smth = new Foo2() { A = source.A };
        }
    }



    class Foo
    {
        public int A { get; set; }
    }


    class Foo2
    {
        public int A { get; set; }
    }
}