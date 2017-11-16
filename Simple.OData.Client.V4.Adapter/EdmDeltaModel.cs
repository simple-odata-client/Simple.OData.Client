using System.Collections.Generic;
using System.Linq;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;

namespace Simple.OData.Client.V4.Adapter
{
    class EdmDeltaModel : IEdmModel
    {
        private readonly IEdmModel _source;
        private readonly EdmEntityType _entityType;

        public EdmDeltaModel(IEdmModel source, IEdmEntityType entityType, ICollection<string> propertyNames)
        {
            _source = source;
            _entityType = new EdmEntityType(entityType.Namespace, entityType.Name, null, entityType.IsAbstract, entityType.IsOpen, entityType.HasStream);

            foreach (var property in entityType.StructuralProperties())
            {
                if (propertyNames.Contains(property.Name))
                    _entityType.AddStructuralProperty(property.Name, property.Type, property.DefaultValueString);
            }

            foreach (var property in entityType.NavigationProperties())
            {
                if (propertyNames.Contains(property.Name))
                {
                    var navInfo = new EdmNavigationPropertyInfo()
                    {
                        ContainsTarget = property.ContainsTarget,
                        DependentProperties = property.DependentProperties(),
                        PrincipalProperties = property.PrincipalProperties(),
                        Name = property.Name,
                        OnDelete = property.OnDelete,
                        Target = property.Partner != null 
                            ? property.Partner.DeclaringEntityType()
                            : property.Type.TypeKind() == EdmTypeKind.Collection
                            ? (property.Type.Definition as IEdmCollectionType).ElementType.Definition as IEdmEntityType
                            : property.Type.TypeKind() == EdmTypeKind.Entity
                            ? property.Type.Definition as IEdmEntityType
                            : null,
                        TargetMultiplicity = property.TargetMultiplicity(),
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

        public IEnumerable<IEdmOperation> FindDeclaredBoundOperations(IEdmType bindingType) { return _source.FindDeclaredBoundOperations(bindingType); }
        public IEnumerable<IEdmOperation> FindDeclaredBoundOperations(string qualifiedName, IEdmType bindingType) { return _source.FindDeclaredBoundOperations(qualifiedName, bindingType); }
        public IEnumerable<IEdmOperation> FindDeclaredOperations(string qualifiedName) { return _source.FindDeclaredOperations(qualifiedName); }
        public IEdmTerm FindDeclaredTerm(string qualifiedName) { return _source.FindDeclaredTerm(qualifiedName); }
        public IEnumerable<IEdmVocabularyAnnotation> FindDeclaredVocabularyAnnotations(IEdmVocabularyAnnotatable element) { return _source.FindDeclaredVocabularyAnnotations(element); }
        public IEnumerable<IEdmStructuredType> FindDirectlyDerivedTypes(IEdmStructuredType baseType) { return _source.FindDirectlyDerivedTypes(baseType); }
        public IEnumerable<IEdmSchemaElement> SchemaElements { get { return _source.SchemaElements; } }
        public IEnumerable<IEdmVocabularyAnnotation> VocabularyAnnotations { get { return _source.VocabularyAnnotations; } }
        public IEnumerable<IEdmModel> ReferencedModels { get { return _source.ReferencedModels; } }
        public IEnumerable<string> DeclaredNamespaces { get; private set; }
        public IEdmDirectValueAnnotationsManager DirectValueAnnotationsManager { get { return _source.DirectValueAnnotationsManager; } }
        public IEdmEntityContainer EntityContainer { get; private set; }
    }
}