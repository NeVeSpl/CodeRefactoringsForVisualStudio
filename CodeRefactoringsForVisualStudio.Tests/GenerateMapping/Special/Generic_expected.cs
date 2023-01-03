using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Some
    {
        static T1 Map<T1, T2>(T2 source) where T1 : FooDTO, new() where T2 : Foo
        {
            var result = new T1()
            {
                A = source.A,
                B = source.B
            };
            return result;
        }
    }


    class Foo
    {
        public int A;
        public string B { get; set; }
    }

    class FooDTO
    {
        public int A;
        public string B { get; set; }
    }
}