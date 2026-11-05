using Microsoft.Extensions.Logging;

namespace BioGamaEcuador.Services;

// Registro generado: conserva las plantillas y evita boxing y evaluaciones innecesarias.
public static partial class LogEvents
{
    [LoggerMessage(EventId = 1218, Level = LogLevel.Warning, Message = "No se pudo completar la solicitud a PayPal; no se confirma el pago.")]
    public static partial void PayPalRequestFailed(ILogger logger, Exception exception);
    [LoggerMessage(EventId = 1215, Level = LogLevel.Warning, Message = "El proveedor de IA no pudo completar la solicitud.")]
    public static partial void OllamaUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1216, Level = LogLevel.Warning, Message = "El proveedor de IA devolvio HTTP {StatusCode}.")]
    public static partial void OllamaRejected(ILogger logger, int statusCode);

    [LoggerMessage(EventId = 1217, Level = LogLevel.Warning, Message = "El proveedor de IA devolvio una respuesta incompleta.")]
    public static partial void OllamaInvalidResponse(ILogger logger);
    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "User with ID '{UserId}' has disabled 2fa.")]
    public static partial void TwoFactorDisabled(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "User with ID '{UserId}' has reset their authentication app key.")]
    public static partial void AuthenticatorReset(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Information, Message = "Correo enviado a {To}")]
    public static partial void EmailSent(ILogger logger, string? @to);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information, Message = "[AUDIT] Action={Action}, Entity={Entity}, EntityId={EntityId}, UserId={UserId}, IP={IP}, Old={Old}, New={New}")]
    public static partial void AuditEvent(ILogger logger, string? @action, string? @entity, string? @entityId, string? @userId, string? @ip, string? @old, string? @new);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Information, Message = "[AUDIT-LOGIN] UserId={UserId}, Success={Success}, IP={IP}")]
    public static partial void AuditLogin(ILogger logger, string? @userId, bool @success, string? @ip);

    [LoggerMessage(EventId = 1205, Level = LogLevel.Information, Message = "[AUDIT-MFA] UserId={UserId}, Enabled={Enabled}, IP={IP}")]
    public static partial void AuditMfaChange(ILogger logger, string? @userId, bool @enabled, string? @ip);

    [LoggerMessage(EventId = 1206, Level = LogLevel.Information, Message = "[AUDIT-ROLE] UserId={UserId}, OldRole={OldRole}, NewRole={NewRole}, ChangedBy={ChangedBy}")]
    public static partial void AuditRoleChange(ILogger logger, string? @userId, string? @oldRole, string? @newRole, string? @changedBy);

    [LoggerMessage(EventId = 1207, Level = LogLevel.Information, Message = "{Name} logged in with {LoginProvider} provider.")]
    public static partial void ExternalLoginSucceeded(ILogger logger, string? @name, string? @loginProvider);

    [LoggerMessage(EventId = 1208, Level = LogLevel.Information, Message = "User created an account using {Name} provider.")]
    public static partial void ExternalAccountCreated(ILogger logger, string? @name);

    [LoggerMessage(EventId = 1209, Level = LogLevel.Information, Message = "User with ID '{UserId}' logged in with 2fa.")]
    public static partial void TwoFactorLogin(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1210, Level = LogLevel.Information, Message = "User with ID '{UserId}' logged in with a recovery code.")]
    public static partial void RecoveryCodeLogin(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1211, Level = LogLevel.Information, Message = "User with ID '{UserId}' deleted themselves.")]
    public static partial void AccountDeleted(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1212, Level = LogLevel.Information, Message = "User with ID '{UserId}' asked for their personal data.")]
    public static partial void PersonalDataRequested(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1213, Level = LogLevel.Information, Message = "User with ID '{UserId}' has enabled 2FA with an authenticator app.")]
    public static partial void AuthenticatorEnabled(ILogger logger, string? @userId);

    [LoggerMessage(EventId = 1214, Level = LogLevel.Information, Message = "User with ID '{UserId}' has generated new 2FA recovery codes.")]
    public static partial void RecoveryCodesGenerated(ILogger logger, string? @userId);
}
