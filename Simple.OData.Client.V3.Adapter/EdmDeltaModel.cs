using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Annotations;
using Microsoft.Data.Edm.Library;

namespace Simple.OData.Client.V3.Adapter
{
    class EdmDeltaModel : IEdmModel
    {
        private readonly IEdmModel _source;
        private readonly EdmEntityType _entityType;

        public EdmDeltaModel(IEdmModel source, IEdmEntityType entityType, ICollection<string> propertyNames)
        {
            _source = source;
            _entityType = new EdmEntityType(entityType.Namespace, entityType.Name, null, entityType.IsAbstract, entityType.IsOpen);

            foreach (var property in entityType.StructuralProperties())
            {
                if (propertyNames.Contains(property.Name))
                    _entityType.AddStructuralProperty(property.Name, property.Type, property.DefaultValueString, property.ConcurrencyMode);
            }

            foreach (var property in entityType.NavigationProperties())
            {
                if (propertyNames.Contains(property.Name))
                {
                    var navInfo = new EdmNavigationPropertyInfo()
                    {
                        ContainsTarget = property.ContainsTarget,
                        DependentProperties = property.DependentProperties,
                        Name = property.Name,
                        OnDelete = property.OnDelete,
                        Target = property.Partner != null
                            ? property.Partner.DeclaringEntityType()
                            : property.Type.TypeKind() == EdmTypeKind.Collection
                            ? (property.Type.Definition as IEdmCollectionType).ElementType.Definition as IEdmEntityType
                            : property.Type.TypeKind() == EdmTypeKind.Entity
                            ? property.Type.Definition as IEdmEntityType
                            : null,
                        TargetMultiplicity = property.Partner != null 
                            ? property.Partner.Multiplicity() 
                            : property.Type.TypeKind() == EdmTypeKind.Collection
                            ? EdmMultiplicity.Many
                            : property.Type.TypeKind() == EdmTypeKind.Entity
                            ? EdmMultiplicity.ZeroOrOne
                            : EdmMultiplicity.Unknown,
                    };
                    _entityType.AddUnidirectionalNavigation(navInfo);
                }
            }
        }

        public IEdmSchemaType FindDeclaredType(string qualifiedName)
        {
            if (qualifiedName == _entityType.FullName())
                return _entityType;
            else
                return _source.FindDeclaredType(qualifiedName);
        }

        public IEdmEntityContainer FindDeclaredEntityContainer(string name) { return _source.FindDeclaredEntityContainer(name); }
        public IEnumerable<IEdmFunction> FindDeclaredFunctions(string qualifiedName) { return _source.FindDeclaredFunctions(qualifiedName); }
        public IEdmValueTerm FindDeclaredValueTerm(string qualifiedName) { return _source.FindDeclaredValueTerm(qualifiedName); }
        public IEnumerable<IEdmVocabularyAnnotation> FindDeclaredVocabularyAnnotations(IEdmVocabularyAnnotatable element) { return _source.FindDeclaredVocabularyAnnotations(element); }
        public IEnumerable<IEdmStructuredType> FindDirectlyDerivedTypes(IEdmStructuredType baseType) { return _source.FindDirectlyDerivedTypes(baseType); }
        public IEnumerable<IEdmSchemaElement> SchemaElements { get { return _source.SchemaElements; } }
        public IEnumerable<IEdmVocabularyAnnotation> VocabularyAnnotations { get { return _source.VocabularyAnnotations; } }
        public IEnumerable<IEdmModel> ReferencedModels { get { return _source.ReferencedModels; } }
        public IEdmDirectValueAnnotationsManager DirectValueAnnotationsManager { get { return _source.DirectValueAnnotationsManager; } }
    }
}