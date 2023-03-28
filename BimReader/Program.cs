using System.Drawing;
using System.Text.Json.Nodes;
using BimReader.Extensions;
using GeometryGym.Ifc;

Console.WriteLine("Hello, World!");
const string path = "ifc/IfcOpenHouse4.ifc";
// const string path = "ifc/Bouwlab_HVA_Sensors.ifc";
var database = new DatabaseIfc(path);

database.ToJSON("output.json");
// var json = database.JSON();

var project = database.Project;
var root = project.RootElement(); // IFCSite
var rootChildren = root.GetChildrenFromDefinition(); // IfcBuilding(s)

var sensors = database.OfType<IfcSensor>().ToList(); // Faster
foreach (var ifcSensor in sensors)
{
    Console.WriteLine($"Sensor with ID '{ifcSensor.Guid}' and Type '{ifcSensor.PredefinedType}': {ifcSensor.Name}");
}

var building = rootChildren.OfType<IfcBuilding>().FirstOrDefault(); // Faster

var buildingParent = building.GetParentFromDefinition(); // IFCSite
var buildingChildren = building.GetChildrenFromDefinition(); // IFCBuildingStorey(s)

var sensor = sensors.FirstOrDefault();
var sensorType = sensor.PredefinedType;

var containingStorey = sensor.GetParentFromElement(); // IFCBuildingStorey
var storeyParent = containingStorey.GetParentFromDefinition(); // IFCBuilding
var storeyChildren = containingStorey.GetChildrenFromElement(); // IFCSensor

var storeys = project.Extract<IfcBuildingStorey>();
var firstFloor = storeys.FirstOrDefault();

var elements = project.Extract<IfcBuiltElement>();

var db2 = new DatabaseIfc();
foreach (var el in database)
{
    var j = el.getJson(database.Project, new BaseClassIfc.SetJsonOptions());
    var c = ParseJsonClass(db2, j);
    // foreach (var e in el.Extract<IfcPerson>())
    // {
    // var f = e.getJson(el, new BaseClassIfc.SetJsonOptions());
    // }
}

db2.ReadJSONFile("output.json");

var points = new List<Coord3D>()
{
    new(0, 0, 0), new(10, 0, 0),
    new(10, 10, 0), new(0, 10, 0),
    new(0, 0, 10), new(10, 0, 10),
    new(10, 10, 10), new(0, 10, 10)
};


var shape = new IfcCartesianPointList3D(database, points);

var triangles = new List<CoordIndex>()
{
    new(1, 6, 5), new(1, 2, 6), new(6, 2, 7),
    new(7, 2, 3), new(7, 8, 6), new(6, 8, 5),
    new(5, 8, 1), new(1, 8, 4), new(4, 2, 1),
    new(2, 4, 3), new(4, 8, 7), new(7, 3, 4)
};

var triangulatedFaceSet = new IfcTriangulatedFaceSet(shape, triangles);
var colourRgbList = new IfcColourRgbList(database, new List<Color>
{
    Color.Red,
    Color.Green,
    Color.Yellow
});

var indexedColourMap = new IfcIndexedColourMap(
    triangulatedFaceSet,
    colourRgbList,
    new List<int> { 1, 1, 2, 2, 3, 3, 1, 1, 1, 1, 1, 1 });

var buildingElementProxy =
    new IfcBuildingElementProxy(building,
        // new IfcLocalPlacement(new IfcAxis2Placement3D(new IfcCartesianPoint(database, 0, 0, 0))),
        null,
        new IfcProductDefinitionShape(new IfcShapeRepresentation(triangulatedFaceSet)));

database.WriteFile("output.ifc");

var dictionary = new Dictionary<string, MyElement>();
foreach (var ele in project.Extract<IfcBuiltElement>())
{
    var desc = (ele switch
    {
        IfcColumn => "COL",
        IfcBeam => "BEAM",
        _ => ""
    });

    var mark = ele.Tag;
    if (!string.IsNullOrEmpty(desc))
    {
        if (dictionary.ContainsKey(mark))
            dictionary[mark].MQuantity++;
        else
        {
            var grade = "";
            double length = 0;
            foreach (var rdp in ele.IsDefinedBy)
            {
                foreach (var propertySet in rdp.RelatingPropertyDefinition.OfType<IfcPropertySet>())
                {
                    foreach (System.Collections.Generic.KeyValuePair<string, IfcProperty> pair in propertySet
                                 .HasProperties)
                    {
                        if (pair.Value is not IfcPropertySingleValue psv)
                        {
                            continue;
                        }

                        if (string.CompareOrdinal("Grade", psv.Name) == 0)
                        {
                            grade = psv.NominalValue.Value.ToString();
                        }
                        else if (string.CompareOrdinal("Length", psv.Name) == 0
                                 && psv.NominalValue is IfcLengthMeasure lengthMeasure)
                        {
                            length = lengthMeasure.Measure;
                        }
                    }
                }
            }

            dictionary.Add(mark, new MyElement(mark, desc, ele.ObjectType, grade, length));
        }
    }
}

Console.WriteLine("Mark\tDescription\tSection\tGrade\tLength\tQty");

foreach (var ee in dictionary.ToList().ConvertAll(x => x.Value).OrderBy(x => x.MMark))
{
    Console.WriteLine(ee.MMark + "\t" + ee.MDescription + "\t" + ee.MSection + "\t" +
                      ee.MGrade + "\t" + ee.MLength + "\t" + ee.MQuantity);
}

Console.WriteLine("...");
Console.ReadKey();

static BaseClassIfc? ParseJsonClass(DatabaseIfc databaseIfc, JsonObject jsonObject)
{
    jsonObject.TryGetPropertyValue("type", out var jsonType);
    var persons = jsonObject.OfType<IfcPerson>();
    return (jsonType?.GetValue<string>()) switch
    {
        "IfcPerson" => databaseIfc.ParseJsonObject<IfcPerson>(jsonObject),
        "IfcOrganization" => databaseIfc.ParseJsonObject<IfcOrganization>(jsonObject),
        _ => null
    };
}

internal class MyElement
{
    internal string MMark { get; }
    internal string MDescription { get; }
    internal string MSection { get; }
    internal string MGrade { get; }
    internal double MLength { get; }
    internal int MQuantity { get; set; } = 1;

    public MyElement(string mMark, string mDescription, string mSection, string mGrade, double mLength)
    {
        MMark = mMark;
        MDescription = mDescription;
        MSection = mSection;
        MGrade = mGrade;
        MLength = mLength;
    }
}

internal class Coord3D : Tuple<double, double, double>
{
    public Coord3D(double item1, double item2, double item3) : base(item1, item2, item3)
    {
    }
}

internal class CoordIndex : Tuple<int, int, int>
{
    public CoordIndex(int item1, int item2, int item3) : base(item1, item2, item3)
    {
    }
}