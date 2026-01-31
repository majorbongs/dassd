using System;
using System.IO;
using LunarLabs.Parser.Binary;
using LunarLabs.Parser.CSV;
using LunarLabs.Parser.JSON;
using LunarLabs.Parser.XML;
using LunarLabs.Parser.YAML;

namespace LunarLabs.Parser;

public static class DataFormats
{
	public static DataFormat GetFormatForExtension(string extension)
	{
		return extension switch
		{
			".xml" => DataFormat.XML, 
			".json" => DataFormat.JSON, 
			".yaml" => DataFormat.YAML, 
			".csv" => DataFormat.CSV, 
			".bin" => DataFormat.BIN, 
			_ => DataFormat.Unknown, 
		};
	}

	public static DataFormat DetectFormat(string content)
	{
		int num = 0;
		while (num < content.Length)
		{
			char c = content[num];
			num++;
			if (!char.IsWhiteSpace(c))
			{
				switch (c)
				{
				case '-':
					return DataFormat.YAML;
				case '<':
					return DataFormat.XML;
				case '[':
				case '{':
					return DataFormat.JSON;
				default:
					return DataFormat.Unknown;
				}
			}
		}
		return DataFormat.Unknown;
	}

	public static DataNode LoadFromString(DataFormat format, string contents)
	{
		return format switch
		{
			DataFormat.XML => XMLReader.ReadFromString(contents), 
			DataFormat.JSON => JSONReader.ReadFromString(contents), 
			DataFormat.YAML => YAMLReader.ReadFromString(contents), 
			DataFormat.CSV => CSVReader.ReadFromString(contents), 
			_ => throw new Exception("Format not supported"), 
		};
	}

	public static string SaveToString(DataFormat format, DataNode root)
	{
		return format switch
		{
			DataFormat.XML => XMLWriter.WriteToString(root), 
			DataFormat.JSON => JSONWriter.WriteToString(root), 
			DataFormat.YAML => YAMLWriter.WriteToString(root), 
			DataFormat.CSV => CSVWriter.WriteToString(root), 
			_ => throw new Exception("Format not supported"), 
		};
	}

	public static DataNode LoadFromString(string content)
	{
		return LoadFromString(DetectFormat(content), content);
	}

	public static DataNode LoadFromFile(string fileName)
	{
		if (!File.Exists(fileName))
		{
			throw new FileNotFoundException();
		}
		string text = Path.GetExtension(fileName).ToLower();
		if (text.Equals(".bin"))
		{
			return BINReader.ReadFromBytes(File.ReadAllBytes(fileName));
		}
		string text2 = File.ReadAllText(fileName);
		DataFormat dataFormat = GetFormatForExtension(text);
		if (dataFormat == DataFormat.Unknown)
		{
			dataFormat = DetectFormat(text2);
			if (dataFormat == DataFormat.Unknown)
			{
				throw new Exception("Could not detect format for " + fileName);
			}
		}
		return LoadFromString(dataFormat, text2);
	}

	public static void SaveToFile(string fileName, DataNode root)
	{
		string contents = SaveToString(GetFormatForExtension(Path.GetExtension(fileName).ToLower()), root);
		File.WriteAllText(fileName, contents);
	}
}
