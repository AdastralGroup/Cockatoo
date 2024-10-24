using System.Reflection;

namespace Adastral.Cockatoo.Common;

/// <summary>
/// Exception that is thrown when a value in <see cref="CockatooConfig"/> is invalid.
/// </summary>
public class InvalidConfigurationValueException : ApplicationException
{
    /// <summary>
    /// Name of the property
    /// </summary>
    public string PropertyName { get; set; }
    /// <summary>
    /// Value of the property
    /// </summary>
    public object? PropertyValue { get; set; }
    /// <summary>
    /// Type of the class or structure that the property is in.
    /// </summary>
    public Type PropertyParent { get; set; }

    public InvalidConfigurationValueException(string message, PropertyInfo property, object propertyParent)
        : base(message)
    {
        if (propertyParent == null)
        {
            throw new ArgumentNullException(nameof(propertyParent));
        }
        PropertyName = property.Name;
        PropertyValue = property.GetValue(propertyParent);
        PropertyParent = propertyParent.GetType();
    }
    public InvalidConfigurationValueException(string message, string propertyName, object propertyParent)
        : base(message)
    {
        if (propertyParent == null)
        {
            throw new ArgumentNullException(nameof(propertyParent));
        }
        PropertyName = propertyName;
        PropertyParent = propertyParent.GetType();
        var propInfo = PropertyParent.GetProperty(propertyName);
        if (propInfo == null)
        {
            throw new InvalidOperationException($"Unable to get value of property since it could not be found via reflection.");
        }
        PropertyValue = propInfo.GetValue(propertyParent);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, [
            base.ToString(),
            $"{nameof(PropertyParent)}: {PropertyParent.AssemblyQualifiedName}",
            $"{nameof(PropertyName)}: {PropertyName}",
            $"Value: {PropertyValue}"
        ]);
    }
}