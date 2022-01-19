using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A
        {
            get;
            set;
        }
        public double B
        {
            get;
            set;
        }


        [|void Fuu(Foo2 other)|]
        {
             
        }
    }


    class Foo2 : Foo3
    {

    }

    class Foo3
    {
        public int A
        {
            get;
            set;
        }
        public double B
        {
            get;
            set;
        }
    }
}