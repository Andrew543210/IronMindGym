using System.ComponentModel.DataAnnotations;

namespace IronMindGym.Models;

public class UITheme
{
    private string _type = "Light";

    public int Id
    {
        get; set;
    }

    [MaxLength(30)]
    public string Type
    {
        get => _type;
        set => _type = string.IsNullOrWhiteSpace(value) ? "Light" : value.Trim();
    }

    public void SwitchTo(string type) => Type = type;

    public override string ToString() => $"UITheme: {Type}";
}
