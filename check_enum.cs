using System;
using Microsoft.EntityFrameworkCore;

public class Program {
    public static void Main() {
        Console.WriteLine(typeof(ParameterizedCollectionMode).FullName);
        foreach (var name in Enum.GetNames(typeof(ParameterizedCollectionMode))) {
            Console.WriteLine(name);
        }
    }
}
