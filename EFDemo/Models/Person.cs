namespace EFDemo.Models;

public abstract class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public Address Address { get; set; }
}

public class Student : Person
{
    public string School { get; set; } = default!;
}

public class Teacher : Person
{
    public string Subject { get; set; } = default!;
}