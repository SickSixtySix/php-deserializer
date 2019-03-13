# PHP-deserializer
.NET Standard class library which allows to deserialize data serialized with serialize() PHP-function

At the moment there is no support for null values, booleans, fixed-point numbers and objects, but it will be implemented in the future versions.

Usage is pretty simple and tight:
```csharp
using SickSixtySix.PHPDeserializer;

...

var data = new PHPDeserializer("a:2:{i:0;s:5:\"Hello\";i:1;s:6:\"Привет\";}").Deserialize();
