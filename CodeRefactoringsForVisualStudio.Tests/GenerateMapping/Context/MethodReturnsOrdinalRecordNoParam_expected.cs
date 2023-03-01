﻿using System;
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
            var result = new FooR()
            {
                A = this.A,
                B = this.B
            };
            return result;
        }
    }

    record FooR
    {
        public int A { get; init; }
        public string B { get; init; }
    };
}