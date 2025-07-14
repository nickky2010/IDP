namespace EFDemo.Models;

public class Address
{
    public int Id { get; set; } // Primary Key
    public string Street { get; set; }
    public string City { get; set; }
    
    public int PersonId { get; set; } // Foreign Key
    public Person Person { get; set; } // Navigation back to Person
} 