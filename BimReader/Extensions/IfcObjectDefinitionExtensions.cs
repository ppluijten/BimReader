using GeometryGym.Ifc;

namespace BimReader.Extensions;

public static class IfcObjectDefinitionExtensions
{
    public static IfcObjectDefinition GetParentFromDefinition(this IfcObjectDefinition definition)
    {
        return definition.Decomposes.RelatingObject;
    }

    public static IReadOnlyCollection<IfcObjectDefinition> GetChildrenFromDefinition(this IfcObjectDefinition definition)
    {
        return definition.IsDecomposedBy.SelectMany(d => d.RelatedObjects).ToList();
    }
}