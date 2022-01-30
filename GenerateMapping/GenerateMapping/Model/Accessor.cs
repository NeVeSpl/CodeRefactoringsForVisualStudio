using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using GenerateMapping.Model;

namespace GenerateMapping.Model
{
    internal class Accessor
    {
        public const string SpecialNameThis = "this";
        public const string SpecialNameReturnType = "result";

        public string Name { get; }
        public TypeData Type { get; }
        public Accessor Parent { get; }        
        public IEnumerable<Accessor> Children { get;  } = Enumerable.Empty<Accessor>();
        public bool IsMatched { get; set; }
        

        public Accessor(IParameterSymbol parameter)
        {           
            Type = new TypeData(parameter.Type);
            Name = parameter.Name;
            Children = GetAccessorsForType(parameter.Type, true);
        }

        public Accessor(ITypeSymbol type, string name, bool publicOnly)
        {           
            Type = new TypeData(type);
            Name = name;
            Children = GetAccessorsForType(type, publicOnly);
        }        

        private Accessor(ISymbol symbol, Accessor parent)
        {
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

        private IEnumerable<Accessor> GetAccessorsForType(ITypeSymbol type, bool publicOnly)
        {
            var fields = type.GetAllMembers().OfType<IFieldSymbol>().OfType<ISymbol>();
            var props = type.GetAllMembers().OfType<IPropertySymbol>().OfType<ISymbol>();
            var all = fields.Union(props);
            var filtered = all.Where(x => !x.IsCompilerGenerated() && !x.IsStatic);
            if (publicOnly)
            {
                filtered = filtered.Where(x => x.DeclaredAccessibility == Accessibility.Public);
            }
            var transformed = filtered.Select(x => new Accessor(x, this));

            return transformed.ToList();
        }        
    }
}