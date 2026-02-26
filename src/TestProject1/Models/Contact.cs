using AutoGenMapperGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.Models;

public class Contact
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    //[MapIgnore]
    public bool Checked { get; set; }
    public List<MissionContact> Missions { get; set; } = [];
}

public class MissionContact
{
    public string? MissionId { get; set; }

    public string? PhoneNumber { get; set; }

    public List<Contact> Contacts { get; set; } = [];
}
public class ContactDto(string name)
{
    public string? Name { get; set; } = name;
    public string? PhoneNumber { get; set; }

    public string? Missions { get; set; }
    public bool Checked { get; set; }
}
public static partial class ContactMap
{
    [GenMapper]
    [MapBetween(nameof(ContactDto.Missions), nameof(Contact.Missions), By = nameof(MissionsTransBack))]
    public static partial Contact ToContact(this ContactDto dto);

    [GenMapper]
    [MapBetween(nameof(Contact.Missions), nameof(ContactDto.Missions), By = nameof(MissionsTrans))]
    [MapIgnore(nameof(Contact.Checked))]
    public static partial ContactDto ToDto(this Contact contact);

    private static List<MissionContact> MissionsTransBack(ContactDto dto, string? missions)
    {
        var items = missions?.Split(',');
        if (items?.Length > 0)
        {
            return [.. items.Select(c => new MissionContact() { MissionId = c, PhoneNumber = dto.PhoneNumber })];
        }
        else
        {
            return [];
        }
    }

    private static string MissionsTrans(List<MissionContact> missions)
    {
        return string.Join(',', missions.Select(c => c.MissionId));
    }
}