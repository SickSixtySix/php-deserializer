# PHP-deserializer
.NET class library which allows to deserialize data serialized with serialize() PHP-function

At the moment there is no support for real number ('d' character), but it will be implemented in the future versions.

Usage is pretty simple and tight:
```csharp
using SickSixtySix.PHPDeserializer;

...

var data = new PHPDeserializer("a:2:{s:5:\"Hello\";s:6:\"Привет\";}").Deserialize();
