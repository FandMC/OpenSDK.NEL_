using System;
using System.Net;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Http;
using OpenSDK.NEL;
using OpenSDK.NEL.Entities;
using Serilog;
using EntitiesCrcSalt = OpenSDK.NEL.Entities.CrcSalt;

namespace OpenSDK.NEL.Utils
{
    public static class CrcSalt
    {
        public static async Task<string> Compute()
        {
            Log.Information("正在计算 CRC Salt...");

            var localPath = Path.Combine(AppContext.BaseDirectory, "Salt.crc");
            var local = string.Empty;
            if (File.Exists(localPath))
            {
                local = (await File.ReadAllTextAsync(localPath)).Trim();
            }
            else
            {
                local = "22AC4B0143EFFC80F2905B267D4D84D3";
                await File.WriteAllTextAsync(localPath, local);
            }

            var http = new HttpWrapper("https://service.codexus.today",
                options => { options.WithBearerToken("c4b97481-ad3f-4ced-966e-f906fb9d1913.pk_32a86c6092954bbe90db6c72"); });

            var result = local;
            try
            {
                var response = await http.GetAsync("/crc-salt");
                var json = await response.Content.ReadAsStringAsync();
                var status = (int)response.StatusCode;
                if (AppState.Debug)
                {
                    Log.Information("Request GET https://service.codexus.today/crc-salt bearer={Bearer}", "0e9327a2-d0f8-41d5-8e23-233de1824b9a.pk_053ff2d53503434bb42fe158");
                }
                try
                {
                    var entity = JsonSerializer.Deserialize<OpenSdkResponse<EntitiesCrcSalt>>(json);
                    if (entity != null && entity.Success && entity.Data != null)
                    {
                        var remote = entity.Data.Salt;
                        if (!string.IsNullOrWhiteSpace(remote))
                        {
                            if (!string.Equals(remote, local, StringComparison.OrdinalIgnoreCase))
                            {
                                await File.WriteAllTextAsync(localPath, remote);
                            }
                            result = remote;
                        }
                    }
                    else
                    {
                        if (AppState.Debug)
                        {
                            var preview = json.Length > 500 ? json.Substring(0, 500) : json;
                            Log.Warning("CRC Salt服务返回失败: Status {Status}, Body {Body}", status, preview);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    if (AppState.Debug)
                    {
                        var preview = json.Length > 500 ? json.Substring(0, 500) : json;
                        Log.Error(ex, "CRC Salt响应非JSON: Status {Status}, Body {Body}", status, preview);
                    }
                }
                if (AppState.Debug)
                {
                    var preview = json.Length > 500 ? json.Substring(0, 500) : json;
                    Log.Information("Response Status {Status}, Body {Body}", status, preview);
                }
            }
            catch (Exception ex)
            {
                if (AppState.Debug) Log.Error(ex, "CRC Salt拉取失败");
            }
            if (AppState.Debug) Log.Information("CRC Salt: {Salt}", result);
            return result;
        }
    }
}