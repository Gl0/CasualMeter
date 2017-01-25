using System.ComponentModel;

namespace CasualMeter.Core.Entities
{
    public class DefaultValueEntity
    {
        public DefaultValueEntity()
        {
            // Iterate through each property and call ResetValue()
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(this))
                property.ResetValue(this);
        }
    }
}
