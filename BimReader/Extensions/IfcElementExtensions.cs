using GeometryGym.Ifc;

namespace BimReader.Extensions;

public static class IfcElementExtensions
{
    public static IfcSpatialElement GetParentFromElement(this IfcElement element)
    {
        return element.ContainedInStructure.RelatingStructure;
    }
}