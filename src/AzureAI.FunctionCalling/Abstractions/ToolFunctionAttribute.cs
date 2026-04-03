namespace AzureAI.FunctionCalling.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ToolFunctionAttribute : Attribute
{
    public ToolFunctionAttribute(string name, string description)
    {
        Name        = name;
        Description = description;
    }

    public string Name { get; }
    public string Description { get; }
}
