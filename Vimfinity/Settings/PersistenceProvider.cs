using System.Text.Json;

namespace Vimfinity;

internal interface IPersistenceProvider
{
	public T Load<T>(Stream stream);
	public void Save<T>(T value, Stream stream);
}

internal interface IFilePersisitenceProvider
{
	public T Load<T>(string path);

	public void Save<T>(T value, string path);
}

internal class JsonPersistenceProvider : IPersistenceProvider
{
	public T Load<T>(Stream stream)
	{
		return JsonSerializer.Deserialize<T>(stream)!;
	}

	public void Save<T>(T value, Stream stream)
	{
		JsonSerializerOptions options = new();
		options.WriteIndented = true;
		JsonSerializer.Serialize(stream, value, options);
	}
}

internal class JsonFilePersistenceProvider : JsonPersistenceProvider, IFilePersisitenceProvider
{
	public T Load<T>(string path)
	{
		using FileStream stream = new FileStream(path, FileMode.Open);
		return Load<T>(stream);
	}

	public void Save<T>(T value, string path)
	{
		using FileStream stream = new FileStream(path, FileMode.Create);
		Save(value, stream);
	}
}

internal class JsonStringPersistenceProvider : JsonPersistenceProvider
{
	public T Load<T>(string json)
	{
		using MemoryStream stream = new MemoryStream();
		StreamWriter writer = new StreamWriter(stream);
		writer.Write(json);
		writer.Flush();
		stream.Position = 0;
		return Load<T>(stream);
	}

	public string Save<T>(T value)
	{
		using MemoryStream stream = new MemoryStream();
		StreamReader reader = new StreamReader(stream);
		Save(value, stream);
		stream.Position = 0;
		return reader.ReadToEnd();
	}
}

internal interface IPathProvider
{
	public string SettingsPath { get; }
}

internal class PathProvider : IPathProvider
{
	public string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vimfinity");
}
