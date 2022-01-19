using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Some
    {
        [|static T1 Map<T1, T2>(T2 source) where T1 : FooDTO where T2 : Foo|]
        {

        }
    }


    class Foo
    {
        public int A;
        public string B { get; set; };
    }

    class FooDTO
    {
        public int A;
        public string B { get; set; };
    }
}