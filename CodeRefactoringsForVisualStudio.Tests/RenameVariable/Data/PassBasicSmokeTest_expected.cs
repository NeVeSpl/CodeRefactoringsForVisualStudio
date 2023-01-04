using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.RenameVariable.Data
{
    public class Basic
    {
        public static void Foo()
        {
            var parent = new Parent();
            var child = parent.GetOne();
            var children = parent.GetMany();
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
