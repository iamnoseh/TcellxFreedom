using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TcellxFreedom.Application.Common;
using TcellxFreedom.Application.DTOs.OsonSms;
using TcellxFreedom.Application.Interfaces;
using TcellxFreedom.Infrastructure.Configuration;

namespace TcellxFreedom.Infrastructure.Services;

public sealed class OsonSmsService : IOsonSmsService
{
    private readonly RestClient _restClient;
    private readonly OsonSmsSettings _settings;
    private readonly ILogger<OsonSmsService> _logger;

    public OsonSmsService(IOptions<OsonSmsSettings> options, ILogger<OsonSmsService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _restClient = new RestClient();
    }

    public async Task<Response<OsonSmsSendResponseDto>> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var txnId = GenerateTransactionId();
            var hash = GenerateHash(txnId, _settings.Login, _settings.Sender, phoneNumber, _settings.PassHash);

            var request = CreateSendSmsRequest(phoneNumber, message, hash, txnId);
            var response = await _restClient.ExecuteAsync<OsonSmsSendResponseDto>(request);

            return HandleResponse(response, "SMS муваффақият равон шуд", "Хатогӣ дар равонкунии SMS");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<OsonSmsSendResponseDto>(ex.Message);
        }
    }

    public async Task<Response<OsonSmsStatusResponseDto>> CheckSmsStatusAsync(string msgId)
    {
        try
        {
            var txnId = GenerateTransactionId();
            var hash = GenerateHash(_settings.Login, txnId, _settings.PassHash);

            var request = CreateCheckStatusRequest(msgId, hash, txnId);
            var response = await _restClient.ExecuteAsync<OsonSmsStatusResponseDto>(request);

            return HandleResponse(response, "Статус гирифта шуд", "Хатогӣ дар гирифтани статус");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<OsonSmsStatusResponseDto>(ex.Message);
        }
    }

    public async Task<Response<OsonSmsBalanceResponseDto>> CheckBalanceAsync()
    {
        try
        {
            var txnId = GenerateTransactionId();
            var hash = GenerateHash(txnId, _settings.Login, _settings.PassHash);

            var request = CreateCheckBalanceRequest(hash, txnId);
            var response = await _restClient.ExecuteAsync<OsonSmsBalanceResponseDto>(request);

            return HandleResponse(response, "Баланс гирифта шуд", "Хатогӣ дар гирифтани баланс");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<OsonSmsBalanceResponseDto>(ex.Message);
        }
    }

    private RestRequest CreateSendSmsRequest(string phoneNumber, string message, string hash, string txnId)
    {
        var request = new RestRequest(_settings.SendSmsUrl);
        request.AddParameter("from", _settings.Sender);
        request.AddParameter("login", _settings.Login);
        request.AddParameter("t", _settings.T);
        request.AddParameter("phone_number", phoneNumber);
        request.AddParameter("msg", message);
        request.AddParameter("str_hash", hash);
        request.AddParameter("txn_id", txnId);
        return request;
    }

    private RestRequest CreateCheckStatusRequest(string msgId, string hash, string txnId)
    {
        var request = new RestRequest(_settings.CheckSmsStatusUrl);
        request.AddParameter("t", _settings.T);
        request.AddParameter("login", _settings.Login);
        request.AddParameter("msg_id", msgId);
        request.AddParameter("str_hash", hash);
        request.AddParameter("txn_id", txnId);
        return request;
    }

    private RestRequest CreateCheckBalanceRequest(string hash, string txnId)
    {
        var request = new RestRequest(_settings.CheckBalanceUrl);
        request.AddParameter("t", _settings.T);
        request.AddParameter("login", _settings.Login);
        request.AddParameter("str_hash", hash);
        request.AddParameter("txn_id", txnId);
        return request;
    }

    private Response<T> HandleResponse<T>(RestResponse<T> response, string successMessage, string errorMessage) where T : class
    {
        if (response is { IsSuccessful: true, Data: not null })
        {
            var error = GetErrorFromData(response.Data);
            if (error != null)
            {
                _logger.LogWarning("OsonSMS API хатогӣ баргардонд: {Error}", error.Message);
                return new Response<T>(HttpStatusCode.BadRequest, error.Message);
            }

            return new Response<T>(response.Data) { Message = successMessage };
        }

        _logger.LogError(
            "OsonSMS HTTP хатогӣ: StatusCode={StatusCode}, ErrorMessage={ErrorMessage}, Content={Content}",
            (int)response.StatusCode, response.ErrorMessage, response.Content);

        return new Response<T>(response.StatusCode, response.ErrorMessage ?? errorMessage);
    }

    private static OsonSmsErrorDto? GetErrorFromData<T>(T data)
    {
        var errorProperty = typeof(T).GetProperty("Error");
        return errorProperty?.GetValue(data) as OsonSmsErrorDto;
    }

    private string GenerateHash(params string[] values)
    {
        var concatenated = string.Join(_settings.Dlm, values);
        return ComputeSha256Hash(concatenated);
    }

    private static string ComputeSha256Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateTransactionId()
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var milliseconds = (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
        return milliseconds.ToString();
    }

    private static Response<T> CreateErrorResponse<T>(string errorMessage) where T : class
    {
        return new Response<T>(HttpStatusCode.InternalServerError, $"Хатогӣ: {errorMessage}");
    }
}
