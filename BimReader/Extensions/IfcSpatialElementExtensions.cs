using GeometryGym.Ifc;

namespace BimReader.Extensions;

public static class IfcSpatialElementExtensions
{
    public static IReadOnlyCollection<IfcObjectDefinition> GetChildrenFromElement(this IfcSpatialElement element)
    {
        return element.ContainsElements.SelectMany(c => c.RelatedElements).ToList();
    }
}