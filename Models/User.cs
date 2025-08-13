namespace BarnCaseAPI.Models;
using Swashbuckle.AspNetCore.Annotations;

public class User
{
    [SwaggerSchema(ReadOnly = true)]
    public int Id { get; set; }
    
    public string Name { get; set; } = "";
}
