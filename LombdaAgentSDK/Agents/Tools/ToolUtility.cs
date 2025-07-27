using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Chat;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LombdaAgentSDK.Agents.Tools
{
    public static class ToolUtility
    {
        /// <summary>
        /// Convert Agents into tools Automatically
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public static AgentTool AsTool(this Agent agent)
        {
            return new AgentTool(agent, new BaseTool().CreateTool(
                            toolName: agent.AgentName,
                            toolDescription: agent.Instructions,
                            toolParameters: BinaryData.FromBytes("""
                            {
                                "type": "object",
                                "properties": { "input" : {"type" : "string"}},
                                "required": [ "input" ]
                            }
                            """u8.ToArray()), true)
                );
        }

      
        /// <summary>
        /// Automatic conversion of method into a function tool
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static FunctionTool ConvertFunctionToTool(this Delegate function)
        {
            List<string> required_inputs = new List<string>();
            var input_tool_map = new Dictionary<string, ParameterSchema>();
            MethodInfo method = function.Method;

            //Get tool Attribute information (Required)
            var toolAttrs = method.GetCustomAttributes<ToolAttribute>();

            if (toolAttrs.Count() == 0) throw new Exception("Function doesn't have Tool Attribute");

            ToolAttribute toolAttr = toolAttrs.First();
            
            //deconstruct the function method parameters
            int i = 0;
            var description = "";
            foreach (ParameterInfo param in method.GetParameters())
            {
                //Name required
                if (param.Name == null) continue;

                //Set Json compatible type
                string typeName = param.ParameterType.IsEnum ? "string" : json_util.MapClrTypeToJsonType(param.ParameterType);

                if(toolAttr.In_parameters_description != null)
                {
                    if (toolAttr.In_parameters_description.Length < i)
                    {
                        description = toolAttr.In_parameters_description[i];
                    }
                }
                else
                {
                    description = typeName; // Default description to type name if not provided
                }

                //Configure Schema
                var schema = new ParameterSchema
                {
                    Type = typeName,
                    Description = description, //Add in description to parameter here from attribute
                    Enum = param.ParameterType.IsEnum ? param.ParameterType.GetEnumNames() : null //Get enum list if parameter is enum
                };

                //Add Schema to input map
                input_tool_map[param.Name] = schema;

                //No default value = required
                if (!param.HasDefaultValue)
                {
                    required_inputs.Add(param.Name);
                }

                i++;
            }

            //Convert the Input map into Json string of the Function schema
            string funcParamResult = JsonSchemaGenerator.BuildFunctionSchema(input_tool_map, required_inputs);

            var strictSchema = true;

            if (toolAttr.In_parameters_description != null)
            {
                strictSchema = required_inputs.Count == toolAttr.In_parameters_description.Length;
            }
            
            return new FunctionTool(
                        toolName: method.Name,
                        toolDescription: toolAttr.Description,
                        toolParameters: BinaryData.FromBytes(Encoding.UTF8.GetBytes(funcParamResult)),
                        function: function,
                        strictSchema: strictSchema//Auto set strict schema
                    );
        }

        /// <summary>
        /// Parse the input args from response to use in function
        /// </summary>
        /// <param name="function"></param>
        /// <param name="functionCallArguments"></param>
        /// <returns></returns>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static List<object> ParseFunctionCallArgs(this Delegate function, BinaryData functionCallArguments)
        {
            MethodInfo method = function.Method;
            List<object> arguments = new List<object>();

            //Gather json args from function call
            using var document = JsonDocument.Parse(functionCallArguments);
            var parameters = method.GetParameters();
            var argumentsByName = document.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            //Convert each value
            foreach (var param in parameters)
            {
                //Missing a required arg
                if (!argumentsByName.TryGetValue(param.Name!, out var value))
                {
                    if (param.HasDefaultValue)
                    {
                        arguments.Add(param.DefaultValue!);
                        continue;
                    }
                    throw new JsonException($"Required parameter '{param.Name}' not found in function call arguments.");
                }
                //converting string back to enum value
                if (param.ParameterType.IsEnum)
                {
                    var enumString = value.GetString();
                    if (Enum.TryParse(param.ParameterType, enumString, ignoreCase: true, out var enumValue))
                    {
                        arguments.Add(enumValue);
                        continue;
                    }
                    else
                    {
                        var validValues = string.Join(", ", Enum.GetNames(param.ParameterType));
                        throw new JsonException($"Invalid value '{enumString}' for enum '{param.ParameterType.Name}'. Valid values: {validValues}");
                    }
                }
                //Converting to primative type
                if (param.ParameterType.IsPrimitive || param.ParameterType == typeof(string) || param.ParameterType == typeof(decimal))
                {
                    arguments.Add(value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString()!,
                        JsonValueKind.Number when param.ParameterType == typeof(int) => value.GetInt32(),
                        JsonValueKind.Number when param.ParameterType == typeof(long) => value.GetInt64(),
                        JsonValueKind.Number when param.ParameterType == typeof(double) => value.GetDouble(),
                        JsonValueKind.Number when param.ParameterType == typeof(float) => value.GetSingle(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null when param.HasDefaultValue => param.DefaultValue!,
                        _ => throw new NotImplementedException(
                            $"Conversion from {value.ValueKind} to {param.ParameterType.Name} is not implemented.")
                    });
                }
                else
                {
                    // Try to deserialize complex types (objects, records)
                    var obj = value.Deserialize(param.ParameterType, jsonOptions);
                    arguments.Add(obj!);
                }
            }

            return arguments;
        }
    }
}
