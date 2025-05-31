using Vdk.Models;

namespace Vdk.Services;

public interface IKindVersionInfoService
{
    Task<KindVersionMap?> UpdateAsync();
    Task<KindVersionMap> GetVersionInfoAsync();

    Task<string> GetDefaultKubernetesVersionAsync(string kindVersion);
}