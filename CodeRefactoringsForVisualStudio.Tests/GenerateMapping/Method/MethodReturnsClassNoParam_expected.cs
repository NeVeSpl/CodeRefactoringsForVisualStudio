﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A { get; set; }
        public string B { get; set; }


        public Foo Fuu()
        {
            var result = new Foo()
            {
                A = this.A,
                B = this.B
            };
            return result;
        }
    }
}