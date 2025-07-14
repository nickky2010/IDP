namespace EFDemo.Models;

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class Employee : User
{
    public int Salary { get; set; }
}

public class Manager : User
{
    public string? Department { get; set; }
} 