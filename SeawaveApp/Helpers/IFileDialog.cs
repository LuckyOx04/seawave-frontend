using System.Threading.Tasks;

namespace SeawaveApp.Helpers;

public interface IFileDialog
{
    Task<string[]> SelectPathsAsync(bool isSingleFile);
}