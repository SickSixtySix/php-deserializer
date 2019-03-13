using System.Collections.Generic;

namespace SickSixtySix.PHPDeserializer
{
    /// <summary>
    /// Deserialization interface
    /// </summary>
    public interface IDeserializer
    {
        /// <summary>
        /// Deserializes string representation into object-object mapping
        /// </summary>
        /// <returns>object-object mapping</returns>
        IDictionary<object, object> Deserialize();
    }
}