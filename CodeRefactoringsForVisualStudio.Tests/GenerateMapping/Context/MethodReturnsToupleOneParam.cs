using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A { get; set; }
        public string C { get; set; }


        [|(int a, double b, string c) Fuu(Foo source)|]
        {            
             
        }
    }
}