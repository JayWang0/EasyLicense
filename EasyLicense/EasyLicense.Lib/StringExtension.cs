using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EasyLicense.Lib
{
	public static class StringExtension
	{
		public static T Deserialize<T>(this string fileName,
			Action<JsonSerializerSettings> configJsonSerializerSettingsAction = null)
		{
			var result = default(T);

			if (File.Exists(fileName))
			{
				var jsonText = File.ReadAllText(fileName);

				var jsonSetting = new JsonSerializerSettings();
				jsonSetting.Converters.Add(new StringEnumConverter());

				configJsonSerializerSettingsAction?.Invoke(jsonSetting);

				using (var reader = new StringReader(jsonText))
				{
					using (JsonReader jsonReader = new JsonTextReader(reader))
					{
						var serializer = JsonSerializer.Create(jsonSetting);

						result = serializer.Deserialize<T>(jsonReader);
					}
				}
			}

			return result;
		}

		public static void Serialize(this string fileName, object obj,
			Action<JsonSerializerSettings> configJsonSerializerSettingsAction = null)
		{
			var jsonSetting = new JsonSerializerSettings();

			jsonSetting.Converters.Add(new StringEnumConverter());
			configJsonSerializerSettingsAction?.Invoke(jsonSetting);

			using (var messagelog = new StringWriter())
			{
				using (JsonWriter jsonWriter = new JsonTextWriter(messagelog))
				{
					jsonWriter.Formatting = Formatting.Indented;

					var jsonSerializer = JsonSerializer.Create(jsonSetting);
					jsonSerializer.Serialize(jsonWriter, obj);
					messagelog.Flush();

					File.WriteAllText(fileName, messagelog.ToString());
				}
			}
		}
	}
}