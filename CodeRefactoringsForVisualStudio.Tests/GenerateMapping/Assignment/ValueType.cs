using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A { get; set; }
        public Footruct B { get; set; }


        [|void Fuu(Foo source)|]
        {
             
        }
    }


    struct Footruct
    {

    }
}