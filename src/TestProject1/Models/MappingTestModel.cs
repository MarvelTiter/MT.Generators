using AutoGenMapperGenerator;

namespace TestProject1.Models;

[GenMapper(typeof(MappingTestModelDto))]
[MapBetween(typeof(MappingTestModelDto), ["Id", "Name", "Level"], "Label", By = nameof(FormatLabel))]
public partial class MappingTestModel
{
    public int Id { get; set; }
    [MapBetween(typeof(MappingTestModelDto), nameof(MappingTestModelDto.DisplayName))]
    public string Name { get; set; }
    public string Level { get; set; }
    public DateTime Deadline { get; set; }
    [MapBetween(typeof(MappingTestModelDto), nameof(Last),By = nameof(DTL))]
    public DateTime Last { get; set; }

    private static long DTL(DateTime dt)
    {
        return dt.Ticks;
    }

    private static DateTime DTL(long tick)
    {
        return new DateTime(tick);
    }

    private static string FormatLabel(int id, string name, string level)
    {
        return $"{id + 1}-{name}-{level}";
    }
    
    //可选反向映射方法
    private static (int, string, string) FormatLabel(string label)
    {
        var values = label.Split('-');
        return (int.Parse(values[0]), values[1], values[2]);
    }
}

public class MappingTestModelDto
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public int Level { get; set; }
    public string Deadline { get; set; }
    public long Last { get; set; }
    public string Label { get; set; }
}