using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.RenameVariable.Data
{
    public class Basic
    {
        public static void Foo()
        {
            [|var x = new Parent();
            var y = x.GetOne();
            var z = x.GetMany();|]
        }
    }

    public class Parent
    {
        public IChild GetOne() => null;
        public IEnumerable<IChild> GetMany()
        {
            yield break;
        }
    }

    public interface IChild
    {

    }
}
