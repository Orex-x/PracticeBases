using System.ComponentModel.DataAnnotations;

namespace Practice_bases.Models;

public class Mail
{
    [Key]
    public int Id { get; set; }
    
    public string Title { get; set; }
    
    public Type Type { get; set; }
}