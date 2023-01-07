using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace GenerateMapping.Model
{
    [DebuggerDisplay("{Name}")]
    internal class Accessor
    {
        public const string SpecialNameThis = "this";
        public const string SpecialNameReturnType = "result";

        public int Index { get; }
        public string Name { get; }
        public TypeData Type { get; }
        public Accessor Parent { get; }        
        public IEnumerable<Accessor> Children { get;  } = Enumerable.Empty<Accessor>();
        public bool IsMatched { get; set; }    


        public Accessor(ITypeSymbol type, string name, IEnumerable<ISymbol> members, bool publicOnly, Side sideOfAssignment, WriteLevel writeLevel)
        {
            Type = new TypeData(type);
            Name = name;
            Children = GetEligibleSymbols(members, publicOnly, sideOfAssignment, writeLevel).Select((x, i) => new Accessor(x, this, i)).ToList();
        }

        private Accessor(ISymbol symbol, Accessor parent, int index)
        {          
            Index = index;
            if (symbol is IPropertySymbol property)
            {
                Type = new TypeData(property.Type);
                Name = property.Name;
            }
            if (symbol is IFieldSymbol field)
            {
                Type = new TypeData(field.Type);
                Name = field.Name;
            }
            Parent = parent;
        }
              

        private IEnumerable<ISymbol> GetEligibleSymbols(IEnumerable<ISymbol> candidates, bool publicOnly, Side sideOfAssignment, WriteLevel writeLevel)
        {
            var members = candidates.Where(x => !x.IsCompilerGenerated() && !x.IsStatic);
            var fields = members.OfType<IFieldSymbol>();
            var props = members.OfType<IPropertySymbol>().Where(x => !x.IsIndexer);

            foreach (var field in fields)
            {               
                if (sideOfAssignment == Side.Left && field.IsReadOnly)
                {
                    if (writeLevel != WriteLevel.Constructor) continue;
                }
                yield return field;
            }

            foreach (var property in props)
            {       
                if (sideOfAssignment == Side.Left)
                {
                    if (property.IsReadOnly && writeLevel != WriteLevel.Constructor) continue;
                    if (publicOnly && property.SetMethod?.DeclaredAccessibility != Accessibility.Public) continue;
                    if (property.SetMethod?.IsInitOnly == true && writeLevel == WriteLevel.Ordinary) continue;
                }
                else
                {
                    if (property.IsWriteOnly) continue;
                    if (publicOnly && property.GetMethod?.DeclaredAccessibility != Accessibility.Public) continue;
                }

                yield return property;
            }
        }        
    }
}