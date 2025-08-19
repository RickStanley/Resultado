using System.Text.Json;
using System.Text.Json.Serialization;
using Resultado.Web;

namespace Resultado.Test;

public class JsonPointerHelperTests
{
    private record Example(string Value, Example2 Nested, [property: JsonPropertyName("Barrs")] Example3[] Nested2);

    private record Example2(string Value);

    private record Example3(string Value);
    
    private record Example4([property: JsonPropertyName("Value")] string Value);

    [Fact]
    public void GetJsonPointer_ReturnsPointer()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonPointerHelper.GetJsonPointer<Example>(t => t.Value, options);

        Assert.Equal("/value", result);
    }

    [Fact]
    public void GetJsonPointer_WhenNested_ReturnsUriPath()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonPointerHelper.GetJsonPointer<Example>(t => t.Nested.Value, options);

        Assert.Equal("/nested/value", result);
    }

    [Fact]
    public void GetJsonPointer_WhenPropertyNameOverriden_RespectsUserPreference()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonPointerHelper.GetJsonPointer<Example4>(t => t.Value, options);

        Assert.Equal("/Value", result);
    }
    
    [Fact]
    public void GetJsonPointer_WhenNestedArray_RetrievesIncludesIndex()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonPointerHelper.GetJsonPointer<Example>(t => t.Nested2[0].Value, options);

        Assert.Equal("/Barrs/0/value", result);
    }
}