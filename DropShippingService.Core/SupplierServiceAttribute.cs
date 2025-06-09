namespace DropShippingService.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SupplierServiceAttribute : Attribute
{
    public string Key { get; }

    public SupplierServiceAttribute(string key)
    {
        Key = key  ?? throw new ArgumentNullException(nameof(key));
    }
}