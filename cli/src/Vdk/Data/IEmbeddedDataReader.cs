namespace Vdk.Data;

public interface IEmbeddedDataReader
{
    string Read(string path);
    T ReadJsonObjects<T>(string path) where T: class, new();
}