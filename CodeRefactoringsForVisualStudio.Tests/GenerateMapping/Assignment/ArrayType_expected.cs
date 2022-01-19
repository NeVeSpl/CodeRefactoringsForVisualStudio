using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class FooDTO
    {
        public int[] A;
        public FooItemDTO[] b;


        void FooDTO(Foo source)
        {
            this.A = source.A.ToArray();
            this.b = source.b.Select(x => new FooItemDTO(x)).ToArray();
        }
    }

    class Foo
    {
        public int[] A;
        public FooItem[] b;
    }


    class FooItemDTO
    {

    }

    class FooItem
    {

    }
}