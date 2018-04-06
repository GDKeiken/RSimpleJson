# RSimpleJson

# **Current Version: 1.0.0**

# Getting Started
1. Make sure the Serializer has been set, a default serializer is provided if additional functionality isn't required
```
JSON.CurrentJsonSerializer = JSON.DefaultJsonSerializer
```

2. Set any encoding option that you require, these setting only apply for serialization. By default all flags are set.
```
JSON.CurrentJsonSerializer.SetOptions(EncodeOptions.All);
```
_EncodeOptions.PrettyPrint_ - will print the resulting string in a readable format

_EncodeOptions.AppendType_ - will append the object type (@type) to be used when deserializing, 
                            this is to help with list of generic object or inherited object.

_Serialize Object Example_
```
SomeClass obj = new SomeClass();
string jsonStr = JSON.Serialize(obj)
```
The resulting string will be formatted if the PrettyPrint flag was set 

_De-Serialize Object Example_
1. Json string to json object
```
string jsonStr = "{\"character\":{\"level\":1,\"attack\":50}}";
object jsonObj = JSON.Deserialize(jsonStr);
```
The resulting object will either be a JsonObject(Dictionary<string,object>) or JsonArray(List<object>)

2. Json string to Object
```
string jsonStr = "{\"character\":{\"level\":1,\"attack\":50}}";
SomeClass jsonObj = JSON.DeserializeStringAsObject<SomeClass>(jsonStr);
```