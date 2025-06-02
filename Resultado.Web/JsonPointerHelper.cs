namespace Resultado.Web;

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Auxílio conversão de expressão LINQ em ponteiro JSON, de acordo com https://www.rfc-editor.org/rfc/rfc6901.html
/// </summary>
public static class JsonPointerHelper
{
    private static string GetSerializedPropertyName(MemberInfo member, JsonSerializerOptions? options)
    {
        JsonPropertyNameAttribute? jsonProperty = member.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonProperty != null)
        {
            return jsonProperty.Name;
        }

        // Aplica política de nomenclatura, se definido
        string propertyName = member.Name;
        return options?.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName;
    }

    /// <summary>
    /// Pega o ponteiro JSON de um membro, em representação URI (RFC 6901 §5).
    /// </summary>
    /// <param name="expression">Expressão LINQ para criar o ponteiro desejado.</param>
    /// <param name="options">Opções de serialização, usada para especificar a nomenclatura.</param>
    /// <typeparam name="T">Tipo alvo a ser inspecionado.</typeparam>
    /// <returns>Cadeia de caracteres representação JSON. <example>"#/Example3Array/0/Nested/1"</example></returns>
    public static string GetJsonUriPointer<T>(Expression<Func<T, object>> expression,
        JsonSerializerOptions? options = null)
        => GetJsonPointer(expression, JsonPointerRepresentation.UriFragment, options);

    public static string GetJsonPointer<T>(Expression<Func<T, object>> expression,
        JsonPointerRepresentation pointerRepresentation, JsonSerializerOptions? options = null)
    {
        if (pointerRepresentation == JsonPointerRepresentation.Normal)
            throw new NotImplementedException();

        Stack<string> pathParts = GetJsonPointerParts(expression, options);

        if (pathParts.Count == 0)
            return pointerRepresentation == JsonPointerRepresentation.UriFragment ? "#" : "";

        string result = pointerRepresentation == JsonPointerRepresentation.UriFragment
            ? "#/" + string.Join('/', pathParts.Select(Uri.EscapeDataString))
            : "/" + string.Join('/', pathParts);

        return result;
    }

    public static string GetJsonPointer<T>(Expression<Func<T, object>> expression,
        JsonSerializerOptions? options = null)
    {
        Stack<string> pathParts = GetJsonPointerParts(expression, options);
        return "/" + string.Join("/", pathParts);
    }

    private static Stack<string> GetJsonPointerParts<T>(Expression<Func<T, object>> expression,
        JsonSerializerOptions? options)
    {
        const string safetyFallback = "INVALID_EXPRESSION";
        Stack<string> pathParts = new();

        Expression? currentExpression = expression.Body;
        while (currentExpression is not ParameterExpression)
        {
            switch (currentExpression)
            {
                case UnaryExpression unaryExpression:
                    // Expressões em cima de números são unários.
                    currentExpression = unaryExpression.Operand;
                    continue;
                case MemberExpression memberExpression:
                    // Acesso de membro: pega o nome serializado e pop
                    pathParts.Push(GetSerializedPropertyName(memberExpression.Member, options));
                    currentExpression = memberExpression.Expression;
                    break;
                case BinaryExpression { Right: ConstantExpression arrayIndexConstantExpression } binaryExpression and
                    { NodeType: ExpressionType.ArrayIndex }:
                    string? item = arrayIndexConstantExpression.Value?.ToString();
                    if (item is null)
                        return new Stack<string>([safetyFallback]);
                    // Índice de array
                    pathParts.Push(item);
                    currentExpression = binaryExpression.Left;
                    break;
                case MethodCallExpression
                    {
                        Arguments:
                        [
                            ConstantExpression
                            {
                                Type.Name: nameof(Int32)
                            } listIndexConstantExpression
                        ],
                        Method.Name: "get_Item"
                    } callExpression
                    when callExpression.Method.DeclaringType.GetInterfaces().Any(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)):
                    // IReadOnlyList índice de outro tipo
                    string? index = listIndexConstantExpression.Value?.ToString();
                    if (index is null)
                        return new Stack<string>([safetyFallback]);
                    pathParts.Push(index);
                    currentExpression = callExpression.Object;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{currentExpression?.GetType().Name} (at {currentExpression}) not supported");
            }
        }

        return pathParts;
    }
}

/// <summary>
/// Valores que especificam a representação de um ponteiro JSON.
/// </summary>
public enum JsonPointerRepresentation
{
    /// <summary>
    /// A representação especificada em RFC 6901 §3.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// A representação cadeia de caracteres JSON especificada  em RFC 6901 §5
    /// </summary>
    JsonString,

    /// <summary>
    /// A representação fragmento identificador URI especificado em RFC 6901 §6
    /// </summary>
    UriFragment
}