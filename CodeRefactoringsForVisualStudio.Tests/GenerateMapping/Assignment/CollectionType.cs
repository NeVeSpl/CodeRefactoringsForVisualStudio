using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class FooDTO
    {
        public IEnumerable<int> A { get; set; }
        public List<FooItemDTO> B { get; set; }
        public HashSet<int> C { get; set; }


        [|void FooDTO(Foo source)|]
        {
             
        }
    }

    class Foo
    {
        public List<int> A { get; set; }
        public IEnumerable<FooItem> B { get; set; }
        public HashSet<int> C { get; set; }
    }


    class FooItemDTO
    {

    }

    class FooItem
    {

    }
}